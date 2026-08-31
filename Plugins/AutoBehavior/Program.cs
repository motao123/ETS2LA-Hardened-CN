using System.Numerics;
using ETS2LA.Backend.Events;
using ETS2LA.Game.Output;
using ETS2LA.Game.SDK;
using ETS2LA.Game.Telemetry;
using ETS2LA.Logging;
using ETS2LA.Notifications;
using ETS2LA.Shared;
using ETS2LA.State;
using TruckLib.ScsMap;
using SdkSemaphore = ETS2LA.Game.SDK.Semaphore;

namespace ETS2LA.AutoBehavior;

/// <summary>
///  AutoBehavior adds two fully automatic behaviors on top of the stock
///  plugins (which are prebuilt DLLs without source in this repo):
///
///  1. Toll gate / checkpoint stop: when a GATE-type semaphore (toll booth
///     barrier, border checkpoint, weighing station) ahead is closed, the
///     truck automatically decelerates, stops before the barrier, and waits
///     until the gate opens before releasing control back to ACC.
///
///  2. Fuel watchdog: monitors fuel level and remaining range, and raises
///     notifications (with the distance to the nearest gas station found in
///     the map data) when refueling is needed.
/// </summary>
public class AutoBehaviorPlugin : Plugin
{
    public override PluginInformation Info => new PluginInformation
    {
        Id = "motao.autobehavior",
        Name = "AutoBehavior",
        Version = "1.0.0",
        Description = "收费站/道闸自动停车等待 + 低油量监视提醒 (Toll gate auto-stop & fuel watchdog)",
        AuthorName = "motao123",
    };

    public override float TickRate => 10f;

    private const string OutputTopic = "ETS2LA.Game.Output.ControlEvent";
    private const string TollChannelId = "motao.autobehavior.tollstop";

    // MARK: Toll gate tuning
    private const float ApproachDistance = 180f; // start caring about gates within this distance (m)
    private const float StopDistance = 12f;      // aim to stop this far before the barrier (m)
    private const float MaxDecel = 2.6f;         // hardest deceleration the plugin may request (m/s^2)
    private const float HoldBrake = 0.5f;        // brake intensity while stopped at the barrier
    private const float BrakeWeight = 4f;        // channel weight, must outweigh ACC's throttle

    // MARK: Fuel watchdog tuning
    private const float FuelFractionThreshold = 0.15f;
    private const float FuelRangeThreshold = 120_000f; // meters
    private const double FuelNotificationIntervalMinutes = 5;

    private GameTelemetryData? latest;
    private readonly Dictionary<int, DateTime> clearedGates = new();
    private float lastGateDistance = float.MaxValue;
    private bool brakeActive = false;

    // movement direction tracking (avoids relying on raw euler heading conventions)
    private Vector3Double lastPosition = new(0, 0, 0);
    private DateTime lastPositionTime = DateTime.MinValue;
    private Vector2? moveDirection = null;

    private DateTime lastFuelCheck = DateTime.MinValue;
    private DateTime lastFuelNotification = DateTime.MinValue;
    private List<Vector3> gasStations = new();
    private bool gasStationsBuilt = false;

    public override void OnEnable()
    {
        Events.Current.Subscribe<GameTelemetryData>(GameTelemetry.Current.EventString, OnTelemetry);
        Logger.Info("AutoBehavior enabled: toll gate auto-stop & fuel watchdog active.");
    }

    public override void OnDisable()
    {
        Events.Current.Unsubscribe<GameTelemetryData>(GameTelemetry.Current.EventString, OnTelemetry);
        ClearBrake();
    }

    public override void Shutdown()
    {
        Events.Current.Unsubscribe<GameTelemetryData>(GameTelemetry.Current.EventString, OnTelemetry);
        ClearBrake();
    }

    private void OnTelemetry(GameTelemetryData data) => latest = data;

    public override void Tick()
    {
        try
        {
            if (latest == null || !latest.sdkActive || latest.paused)
            {
                ClearBrake();
                return;
            }

            UpdateMovementDirection();
            HandleTollGates();
            HandleFuelWatchdog();
        }
        catch (Exception ex)
        {
            Logger.Error($"AutoBehavior tick error: {ex}");
        }
    }

    // MARK: Toll gate / checkpoint stop

    private void HandleTollGates()
    {
        var app = ApplicationState.Current;
        if (app.PauseLongitudinalAssist)
        {
            ClearBrake();
            return;
        }

        SemaphoreData? data = SemaphoreProvider.Current.GetCurrentData();
        if (data == null)
        {
            ClearBrake();
            return;
        }

        var truckPos = latest!.truckPlacement.coordinate;
        float speed = latest.truckFloat.speed;

        SdkSemaphore? target = null;
        float bestDistance = float.MaxValue;
        foreach (var semaphore in data.semaphores)
        {
            if (semaphore == null || semaphore.type != SemaphoreType.GATE)
                continue;

            var gateState = (GateStates)semaphore.state;
            float distance = HorizontalDistance(truckPos, semaphore.GetWorldCoordinates());
            if (distance > ApproachDistance)
                continue;

            if (gateState is GateStates.OPEN or GateStates.OPENING)
            {
                // The gate is lifting: remember it as cleared so we don't brake
                // for it again if we're still close by.
                if (distance < 60f)
                    clearedGates[semaphore.id] = DateTime.UtcNow;
                continue;
            }

            if (clearedGates.TryGetValue(semaphore.id, out var cleared) &&
                (DateTime.UtcNow - cleared).TotalSeconds < 60)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                target = semaphore;
            }
        }

        if (target == null)
        {
            lastGateDistance = float.MaxValue;
            ClearBrake();
            return;
        }

        // Only brake for gates that are actually in front of us. Movement
        // direction is derived from consecutive placements, which is robust
        // against the various euler/heading conventions in the SCS SDK.
        var gatePos = target.GetWorldCoordinates();
        float toGateX = gatePos.X - (float)truckPos.X;
        float toGateZ = gatePos.Z - (float)truckPos.Z;
        float toGateLength = MathF.Sqrt(toGateX * toGateX + toGateZ * toGateZ);
        if (moveDirection.HasValue && toGateLength > 0.5f)
        {
            float alignment = moveDirection.Value.X * (toGateX / toGateLength)
                            + moveDirection.Value.Y * (toGateZ / toGateLength);
            if (alignment < 0.3f)
            {
                lastGateDistance = bestDistance;
                ClearBrake();
                return;
            }
        }

        float effective = MathF.Max(bestDistance - StopDistance, 0.1f);
        float requiredDecel = speed > 0.3f ? (speed * speed) / (2f * effective) : 0f;
        float brake = Math.Clamp(requiredDecel / MaxDecel, 0f, 1f);
        if (bestDistance <= StopDistance)
            brake = speed > 0.1f ? 1f : HoldBrake; // hold the truck at the barrier

        if (brake <= 0.02f)
        {
            ClearBrake();
            return;
        }

        PublishBrake(brake);
    }

    private void PublishBrake(float brake)
    {
        brakeActive = true;
        Events.Current.Publish(OutputTopic, new ControlEvent
        {
            ChannelDefinition = new ControlChannelDefinition { Id = TollChannelId, Timeout = 0.3f },
            Properties = new ControlProperties { Weight = BrakeWeight },
            Variables = new ControlVariables { aforward = -brake },
        });
    }

    private void ClearBrake()
    {
        if (!brakeActive)
            return;
        brakeActive = false;
        // An empty ControlVariables clears the channel in GameOutput.
        Events.Current.Publish(OutputTopic, new ControlEvent
        {
            ChannelDefinition = new ControlChannelDefinition { Id = TollChannelId },
            Variables = new ControlVariables(),
        });
    }

    private void UpdateMovementDirection()
    {
        var now = DateTime.UtcNow;
        var pos = latest!.truckPlacement.coordinate;
        float dt = (float)(now - lastPositionTime).TotalSeconds;

        if (lastPositionTime == DateTime.MinValue || dt <= 0)
        {
            lastPosition = pos;
            lastPositionTime = now;
            return;
        }

        float dx = (float)(pos.X - lastPosition.X);
        float dz = (float)(pos.Z - lastPosition.Z);
        float dist = MathF.Sqrt(dx * dx + dz * dz);
        if (dist > 1.0f && dt > 0.2f)
        {
            moveDirection = new Vector2(dx / dist, dz / dist);
            lastPosition = pos;
            lastPositionTime = now;
        }
    }

    // MARK: Fuel watchdog

    private void HandleFuelWatchdog()
    {
        if ((DateTime.UtcNow - lastFuelCheck).TotalSeconds < 5)
            return;
        lastFuelCheck = DateTime.UtcNow;

        var truck = latest!.truckFloat;
        float capacity = latest.configFloat.fuelCapacity;
        if (capacity <= 0)
            return;

        float fraction = truck.fuel / capacity;
        float range = truck.fuelRange;
        bool low = fraction < FuelFractionThreshold || (range > 0 && range < FuelRangeThreshold);
        if (!low)
            return;

        Events.Current.Publish<float>("AutoBehavior.Fuel.Low", fraction);

        if ((DateTime.UtcNow - lastFuelNotification).TotalMinutes < FuelNotificationIntervalMinutes)
            return;
        lastFuelNotification = DateTime.UtcNow;

        (Vector3 position, float distance)? station = FindNearestGasStation();
        string rangeText = range > 0 ? $"，续航约 {range / 1000f:0} km" : "";
        string content = station.HasValue
            ? $"当前油量剩余 {fraction * 100f:0}%{rangeText}。最近的加油站在直线距离约 {station.Value.distance / 1000f:0} km 处，请注意规划加油。"
            : $"当前油量剩余 {fraction * 100f:0}%{rangeText}。请在地图上尽快寻找加油站加油。";

        NotificationHandler.Current.SendNotification(new Notification
        {
            Id = "AutoBehavior.Fuel.Low",
            Title = "油量不足提醒",
            Content = content,
            Level = NotificationLevel.Warning,
        });
        Logger.Info($"AutoBehavior fuel warning: {fraction:P0} remaining, range {range / 1000f:0} km" +
                    (station.HasValue ? $", nearest station {station.Value.distance / 1000f:0} km" : ", no station indexed"));
    }

    private (Vector3 position, float distance)? FindNearestGasStation()
    {
        EnsureGasStationsBuilt();
        if (gasStations.Count == 0)
            return null;

        var truckPos = latest!.truckPlacement.coordinate;
        (Vector3, float) best = default;
        float bestDistance = float.MaxValue;
        foreach (var station in gasStations)
        {
            float distance = HorizontalDistance(truckPos, station);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = (station, distance);
            }
        }
        return (best.Item1, bestDistance);
    }

    private void EnsureGasStationsBuilt()
    {
        if (gasStationsBuilt)
            return;

        var game = ApplicationState.Current.RunningGame;
        if (game == null || !game.IsParsed)
            return;
        var map = game.GetMapData();
        if (map == null)
            return;

        var stations = new List<Vector3>();
        foreach (var item in map.MapItems.Values)
        {
            if (item is Prefab prefab)
            {
                string model = prefab.Model.ToString().ToLowerInvariant();
                if (model.Contains("fuel") || model.Contains("gas") || model.Contains("petrol"))
                {
                    if (prefab.Nodes.Count > 0)
                        stations.Add(prefab.Nodes[0].Position);
                }
            }
        }

        gasStations = stations;
        gasStationsBuilt = true;
        Logger.Info($"AutoBehavior: indexed {stations.Count} potential gas station prefabs from the map data.");
    }

    private static float HorizontalDistance(Vector3Double a, Vector3 b)
    {
        float dx = (float)(a.X - b.X);
        float dz = (float)(a.Z - b.Z);
        return MathF.Sqrt(dx * dx + dz * dz);
    }
}
