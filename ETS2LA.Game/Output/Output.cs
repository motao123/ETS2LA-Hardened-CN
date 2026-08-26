using ETS2LA.Logging;
using ETS2LA.Backend.Events;
using ETS2LA.Game.Telemetry;

using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Reflection;

namespace ETS2LA.Game.Output;

public sealed class GameOutput : IDisposable
{
    private static readonly Lazy<GameOutput> _instance = new(() => new GameOutput());
    public static GameOutput Current => _instance.Value;
    public string EventString = "ETS2LA.Game.Output.ControlEvent";

    private readonly object channelsLock = new();
    private readonly object ioLock = new();
    private readonly Dictionary<string, ControlChannel> channels = new(StringComparer.Ordinal);
    private readonly HashSet<Task> toggleTasks = new();
    private readonly CancellationTokenSource shutdown = new();
    private readonly Task worker;
    private readonly Stopwatch sinceTriedMemoryAccess = Stopwatch.StartNew();
    private readonly Dictionary<string, int> legacyShmOffsets = new(StringComparer.Ordinal);
    private readonly HashSet<string> previouslyMixedControls = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> slewState = new(StringComparer.Ordinal);
    private readonly TimeSpan tickInterval = TimeSpan.FromSeconds(1d / 60d);
    private readonly TimeSpan telemetryMaxAge = TimeSpan.FromSeconds(2);
    private DateTime lastLoopError = DateTime.MinValue;
    private bool telemetryFailsafeActive;
    private bool isReset;
    private bool disposed;

    public IReadOnlyDictionary<string, ControlChannel> Channels
    {
        get
        {
            lock (channelsLock)
                return new Dictionary<string, ControlChannel>(channels, StringComparer.Ordinal);
        }
    }

    private const string LegacyMapName = "Local\\SCSControls";
    private const string LegacyMapNameLinux = "/dev/shm/SCS/SCSControls";
    private const string ModernMapName = "Local\\ETS2LAPluginInput";
    private const string ModernMapNameLinux = "/dev/shm/ETS2LAPluginInput";
    private const int ModernMapSize = 26;
    private readonly int legacyMapSize;

    private MemoryMappedFile? legacyMmf;
    private MemoryMappedViewAccessor? legacyAccessor;
    private MemoryMappedFile? modernMmf;
    private MemoryMappedViewAccessor? modernAccessor;

    private bool MemoryAccessAvailable => legacyAccessor != null && modernAccessor != null;

    private GameOutput()
    {
        var offset = 0;
        foreach (var field in typeof(ControlVariables).GetFields())
        {
            legacyShmOffsets[field.Name] = offset;
            if (field.FieldType == typeof(bool?))
                offset += sizeof(bool);
            else if (field.FieldType == typeof(float?))
                offset += sizeof(float);
        }
        legacyMapSize = offset;

        Events.Current.Subscribe<ControlEvent>(EventString, OnControlEvent);
        worker = Task.Factory.StartNew(
            () => Tick(shutdown.Token),
            shutdown.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public void OnControlEvent(ControlEvent controlEvent)
    {
        ArgumentNullException.ThrowIfNull(controlEvent);
        ArgumentNullException.ThrowIfNull(controlEvent.ChannelDefinition);
        ValidateChannelId(controlEvent.ChannelDefinition.Id);

        lock (channelsLock)
        {
            if (controlEvent.Variables == null || controlEvent.Properties == null)
            {
                channels.Remove(controlEvent.ChannelDefinition.Id);
                return;
            }

            ValidateEvent(controlEvent);
            channels[controlEvent.ChannelDefinition.Id] = CloneChannel(controlEvent);
        }
    }

    private static void ValidateChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        if (channelId.Length > 128 || channelId.Any(char.IsControl) ||
            channelId.IndexOfAny(new[] { '/', '\\' }) >= 0)
            throw new ArgumentException("Control channel ID is invalid.", nameof(channelId));
    }

    private static void ValidateEvent(ControlEvent controlEvent)
    {
        var timeout = controlEvent.ChannelDefinition.Timeout;
        if (!float.IsFinite(timeout) || timeout <= 0f || timeout > 60f)
            throw new ArgumentOutOfRangeException(nameof(controlEvent), "Control channel timeout must be finite and between 0 and 60 seconds.");
        if (!Enum.IsDefined(controlEvent.Properties.BooleanType))
            throw new ArgumentOutOfRangeException(nameof(controlEvent), "Control boolean behavior is invalid.");
        if (!float.IsFinite(controlEvent.Properties.Weight) || controlEvent.Properties.Weight <= 0f)
            throw new ArgumentOutOfRangeException(nameof(controlEvent), "Control channel weight must be finite and positive.");

        foreach (var field in typeof(ControlVariables).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.FieldType == typeof(float?) && field.GetValue(controlEvent.Variables) is float value && !float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(controlEvent), $"Control value '{field.Name}' must be finite.");
        }
    }

    private static ControlChannel CloneChannel(ControlEvent controlEvent)
    {
        var variables = new ControlVariables();
        foreach (var field in typeof(ControlVariables).GetFields(BindingFlags.Instance | BindingFlags.Public))
            field.SetValue(variables, field.GetValue(controlEvent.Variables));

        return new ControlChannel
        {
            Definition = new ControlChannelDefinition
            {
                Id = controlEvent.ChannelDefinition.Id,
                Timeout = controlEvent.ChannelDefinition.Timeout
            },
            Properties = new ControlProperties
            {
                BooleanType = controlEvent.Properties.BooleanType,
                Weight = controlEvent.Properties.Weight
            },
            Variables = variables
        };
    }

    private void Tick(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(tickInterval);
        try
        {
            while (timer.WaitForNextTickAsync(cancellationToken).AsTask().GetAwaiter().GetResult())
                ProcessFrame(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Logger.Error($"Control output loop terminated unexpectedly: {ex}");
            TryResetAfterFailure();
        }
    }

    private void ProcessFrame(CancellationToken cancellationToken)
    {
        try
        {
            if (!TryOpenMemory())
                return;

            var snapshot = GetActiveChannels();
            if (snapshot.Count == 0)
            {
                lock (ioLock)
                    ResetOutputs();
                return;
            }

            if (!GameTelemetry.Current.IsFresh(telemetryMaxAge))
            {
                MarkBooleanInputsHandled(snapshot);
                lock (ioLock)
                    ResetOutputs();
                if (!telemetryFailsafeActive)
                {
                    telemetryFailsafeActive = true;
                    Logger.Warn("Telemetry is stale; all game control outputs were reset to neutral.");
                }
                return;
            }

            if (telemetryFailsafeActive)
            {
                telemetryFailsafeActive = false;
                Logger.Info("Telemetry recovered; game control output resumed.");
            }

            lock (ioLock)
            {
                if (!MemoryAccessAvailable)
                    return;

                isReset = false;
                ProcessBooleans(snapshot, cancellationToken);
                WriteMixedFloats(ControlMixer.Mix(snapshot));
                legacyAccessor!.Flush();
                modernAccessor!.Flush();
            }
        }
        catch (Exception ex)
        {
            CloseMemory();
            if (DateTime.UtcNow - lastLoopError > TimeSpan.FromSeconds(5))
            {
                lastLoopError = DateTime.UtcNow;
                Logger.Error($"Control output frame failed and was reset: {ex}");
            }
        }
    }

    private List<ControlChannel> GetActiveChannels()
    {
        lock (channelsLock)
        {
            var expired = channels
                .Where(pair => pair.Value.LastUpdate.Elapsed.TotalSeconds > pair.Value.Definition.Timeout)
                .Select(pair => pair.Key)
                .ToArray();
            foreach (var channelId in expired)
                channels.Remove(channelId);
            return channels.Values.ToList();
        }
    }

    private static void MarkBooleanInputsHandled(IEnumerable<ControlChannel> snapshot)
    {
        foreach (var channel in snapshot)
            channel.BoolsProcessed = true;
    }

    private void ProcessBooleans(IEnumerable<ControlChannel> snapshot, CancellationToken cancellationToken)
    {
        foreach (var channel in snapshot)
        {
            if (channel.BoolsProcessed)
                continue;

            foreach (var field in typeof(ControlVariables).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType != typeof(bool?) || field.GetValue(channel.Variables) is not bool value)
                    continue;

                if (value && channel.Properties.BooleanType == ControlBooleanType.TrueToToggle)
                    TrackToggle(field.Name, cancellationToken);
                else
                    WriteBool(legacyAccessor!, legacyShmOffsets[field.Name], value);
            }

            lock (channelsLock)
            {
                if (channels.TryGetValue(channel.Definition.Id, out var current) && ReferenceEquals(current, channel))
                    current.BoolsProcessed = true;
            }
        }
    }

    private void TrackToggle(string controlName, CancellationToken cancellationToken)
    {
        var task = ToggleBoolAsync(controlName, cancellationToken);
        lock (toggleTasks)
            toggleTasks.Add(task);
        _ = task.ContinueWith(
            completed =>
            {
                lock (toggleTasks)
                    toggleTasks.Remove(completed);
                if (completed.IsFaulted && completed.Exception != null)
                    Logger.Error($"Control toggle '{controlName}' failed: {completed.Exception.GetBaseException().Message}");
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private async Task ToggleBoolAsync(string controlName, CancellationToken cancellationToken)
    {
        lock (ioLock)
        {
            if (legacyAccessor == null)
                return;
            WriteBool(legacyAccessor, legacyShmOffsets[controlName], true);
        }

        try
        {
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lock (ioLock)
            {
                if (legacyAccessor != null)
                    WriteBool(legacyAccessor, legacyShmOffsets[controlName], false);
            }
        }
    }

    private void WriteMixedFloats(IReadOnlyDictionary<string, float> mixed)
    {
        var controlsToWrite = previouslyMixedControls.Concat(mixed.Keys).ToHashSet(StringComparer.Ordinal);
        foreach (var controlName in controlsToWrite)
        {
            var value = mixed.TryGetValue(controlName, out var mixedValue) && float.IsFinite(mixedValue)
                ? mixedValue
                : 0f;

            // 主机侧失效安全：对主要驾驶控制做 slew-rate 限幅，防止急转/急加油抖动。
            if (controlName is "steering" or "acceleration")
            {
                slewState.TryGetValue(controlName, out var previous);
                value = OutputSafety.LimitSlew(previous, value);
                slewState[controlName] = value;
            }

            WriteMixedControl(controlName, value);
        }

        previouslyMixedControls.Clear();
        previouslyMixedControls.UnionWith(mixed.Keys);
    }

    private void WriteMixedControl(string controlName, float value)
    {
        value = float.IsFinite(value) ? Math.Clamp(value, -1f, 1f) : 0f;
        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d;

        if (controlName == "steering")
        {
            WriteFloat(modernAccessor!, 0, value);
            WriteBool(modernAccessor!, 4, value != 0f);
            WriteDouble(modernAccessor!, 5, time);
            return;
        }

        if (controlName == "acceleration")
        {
#if LINUX
            WriteFloat(modernAccessor!, 13, value);
            WriteBool(modernAccessor!, 17, value != 0f);
            WriteDouble(modernAccessor!, 18, time);
#else
            WriteFloat(legacyAccessor!, legacyShmOffsets["aforward"], Math.Max(value, 0f));
            WriteFloat(legacyAccessor!, legacyShmOffsets["abackward"], Math.Max(-value, 0f));
#endif
            return;
        }

        if (legacyShmOffsets.TryGetValue(controlName, out var offset))
            WriteFloat(legacyAccessor!, offset, value);
    }

    private bool TryOpenMemory()
    {
        lock (ioLock)
        {
            if (MemoryAccessAvailable)
                return true;
            if (sinceTriedMemoryAccess.Elapsed < TimeSpan.FromSeconds(5))
                return false;

            CloseMemoryCore();
            try
            {
#if WINDOWS
                legacyMmf = MemoryMappedFile.OpenExisting(LegacyMapName);
                modernMmf = MemoryMappedFile.OpenExisting(ModernMapName);
#else
                legacyMmf = MemoryMappedFile.CreateFromFile(LegacyMapNameLinux);
                modernMmf = MemoryMappedFile.CreateFromFile(ModernMapNameLinux);
#endif
                legacyAccessor = legacyMmf.CreateViewAccessor(0, legacyMapSize, MemoryMappedFileAccess.Write);
                modernAccessor = modernMmf.CreateViewAccessor(0, ModernMapSize, MemoryMappedFileAccess.ReadWrite);
                Logger.Debug("Successfully opened memory for output.");
            }
            catch (Exception ex)
            {
                CloseMemoryCore();
                Logger.Debug($"Memory not available for output: {ex.Message}");
            }
            finally
            {
                sinceTriedMemoryAccess.Restart();
            }
            return MemoryAccessAvailable;
        }
    }

    private void ResetOutputs()
    {
        if (!MemoryAccessAvailable || isReset)
            return;

        foreach (var field in typeof(ControlVariables).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.FieldType == typeof(float?))
                WriteFloat(legacyAccessor!, legacyShmOffsets[field.Name], 0f);
            else if (field.FieldType == typeof(bool?))
                WriteBool(legacyAccessor!, legacyShmOffsets[field.Name], false);
        }

        WriteFloat(modernAccessor!, 0, 0f);
        WriteBool(modernAccessor!, 4, false);
        WriteDouble(modernAccessor!, 5, 0d);
        WriteFloat(modernAccessor!, 13, 0f);
        WriteBool(modernAccessor!, 17, false);
        WriteDouble(modernAccessor!, 18, 0d);
        legacyAccessor!.Flush();
        modernAccessor!.Flush();
        previouslyMixedControls.Clear();
        slewState.Clear();
        isReset = true;
    }

    private void TryResetAfterFailure()
    {
        try
        {
            lock (ioLock)
                ResetOutputs();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to reset control output after loop failure: {ex.Message}");
        }
    }

    private static void WriteBool(MemoryMappedViewAccessor accessor, int offset, bool value) => accessor.Write(offset, value);

    private static void WriteFloat(MemoryMappedViewAccessor accessor, int offset, float value)
    {
        accessor.Write(offset, float.IsFinite(value) ? value : 0f);
    }

    private static void WriteDouble(MemoryMappedViewAccessor accessor, int offset, double value)
    {
        accessor.Write(offset, double.IsFinite(value) ? value : 0d);
    }

    private void CloseMemory()
    {
        lock (ioLock)
            CloseMemoryCore();
    }

    private void CloseMemoryCore()
    {
        legacyAccessor?.Dispose();
        modernAccessor?.Dispose();
        legacyMmf?.Dispose();
        modernMmf?.Dispose();
        legacyAccessor = null;
        modernAccessor = null;
        legacyMmf = null;
        modernMmf = null;
        isReset = false;
    }

    public void Shutdown()
    {
        if (disposed)
            return;

        shutdown.Cancel();
        if (Task.CurrentId != worker.Id)
        {
            try { worker.Wait(TimeSpan.FromSeconds(3)); }
            catch (AggregateException ex) when (ex.InnerExceptions.All(error => error is OperationCanceledException)) { }
        }

        Task[] pendingToggles;
        lock (toggleTasks)
            pendingToggles = toggleTasks.ToArray();
        try { Task.WaitAll(pendingToggles, TimeSpan.FromSeconds(1)); }
        catch (AggregateException ex) when (ex.InnerExceptions.All(error => error is OperationCanceledException)) { }

        TryResetAfterFailure();
        Events.Current.Unsubscribe<ControlEvent>(EventString, OnControlEvent);
        CloseMemory();
        shutdown.Dispose();
        disposed = true;
    }

    public void Dispose() => Shutdown();
}
