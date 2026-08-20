using Velopack;
using Velopack.Locators;

using ETS2LA.Tutorials;
using ETS2LA.Overlay;
using ETS2LA.Backend;
using ETS2LA.Game.Telemetry;
using ETS2LA.State;
using ETS2LA.Logging;
using ETS2LA.Settings.Global;
using ETS2LA.Telemetry;
using ETS2LA.Networking;
using ETS2LA.Game.Output;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;

namespace ETS2LA;

internal static class Program
{
    private static TracerProvider? tracerProvider;
    private static MeterProvider? meterProvider;

    /// <summary>
    ///  Main entrypoint for ETS2LA.
    /// </summary>
    static void Main(string[] args)
    {
        if (args.Any(argument => string.Equals(argument, "--smoke-test", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.ExitCode = RunSmokeTest();
            return;
        }

        // This handles the main thread crashing (Avalonia)
        // Nothing else *should* run on the main thread.
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Utils.HandleFatalException(e.ExceptionObject as Exception, tracerProvider, meterProvider);
        };

        // This is for unobserved exceptions, i.e. plugins and other Task.Run() calls etc..
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            e.SetObserved(); // Prevents an immediate crash, we'll handle termination in HandleFatalException instead.

            // Avalonia's IBus integration on linux throws these from DBus calls
            // that nothing awaits (like when closing the window). They are harmless, can be ignored.
            if (e.Exception.Flatten().InnerExceptions.All(ex => ex is Tmds.DBus.Protocol.DBusExceptionBase))
            {
                Logger.Warn($"Ignored DBus exception: {e.Exception.InnerException?.Message}");
                return;
            }

            Utils.HandleFatalException(e.Exception, tracerProvider, meterProvider);
        };

        args = Utils.WaitForRestartParentProcess(args);

        if (Utils.IsRunningAsRoot())
            Utils.HandleContinueClose("ETS2LA is running as a system administrator. This puts your system at risk if you use 3rd party plugins. Select Yes to continue anyway and accept the risk.");

        if (Utils.DoesETS2LAProcessExist())
            throw new InvalidOperationException("ETS2LA is already running, please close it from the Task Manager.");

        // Velopack is the installer / update manager
        // Please don't move this, Velopack has to be initialized before anything else,
        // otherwise we might end up with weird bugs.
        VelopackApp.Build()
            .SetAutoApplyOnStartup(false)
            #if DEBUG
            .SetLocator(new TestVelopackLocator(
                appId: "ETS2LA",
                version: "1.0.0",
                packagesDir: "./Releases/Portable"
            ))
            #endif
            .Run();

        string currentVersion = VelopackLocator.Current?.CurrentlyInstalledVersion?.ToString()
                             ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) 
                             ?? "unknown"; 

        // For OTel (OpenTelemetry)
        var appResource = ResourceBuilder.CreateDefault()
            .AddService("ETS2LA", serviceVersion: currentVersion)
            .AddAttributes(OTelAttributes.GetAttributes());

        if (UserSettings.Current.IsTelemetryEnabled)
        {
            tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(appResource)
                .AddSource("ETS2LA.*")
                .AddOtlpExporter(options =>
                {
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Endpoint = new Uri("https://otel.ets2la.com/v1/traces");
                })
                .Build();

            meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(appResource)
                .AddMeter("ETS2LA.*")
                .AddOtlpExporter(options =>
                {
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Endpoint = new Uri("https://otel.ets2la.com/v1/metrics");
                })
                .Build();
        }

        bool shutdown = false;
        Task? analyticsThread = null;
        if (UserSettings.Current.IsTelemetryEnabled)
        {
            analyticsThread = Task.Factory.StartNew(() =>
            {
                while (!shutdown)
                {
                    AppAnalytics.Pulse();
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
            }, TaskCreationOptions.LongRunning);
        }

        var BackendThread = Task.Run(() =>
        {
            // These initialize global instances, if there's a more "official" way to
            // do this then please make a PR for that.
            var ar = OverlayHandler.Current;
            var backend = PluginBackend.Current;
            var telemetry = GameTelemetry.Current;
            var state = ApplicationState.Current;
            var tutorials = TutorialHandler.Current;
            var networking = NetworkingClient.Current;
        });

        # if LINUX
            string? useWayland = Environment.GetEnvironmentVariable("GLFW_USE_WAYLAND");
            if (useWayland == null || useWayland == "0" || useWayland == "")
            {
                // This is to prevent GLFW from trying to use wayland. If wayland is still required
                // then setting GLFW_USE_WAYLAND=1 should work fine.
                Environment.SetEnvironmentVariable("GLFW_USE_WAYLAND", "0");
                Environment.SetEnvironmentVariable("SDL_VIDEODRIVER", "x11");
            }
        # endif

        // Gotta wait for the UI thread to close (i.e. user closed the window)
        // and then tell the backend to shutdown too.
        UI.Program.Main(args);

        shutdown = true;
        PluginBackend.Current.Shutdown();
        GameOutput.Current.Shutdown();
        OverlayHandler.Current.Shutdown();
        GameTelemetry.Current.Shutdown();
        ApplicationState.Current.Shutdown();
        TutorialHandler.Current.Shutdown();

        LogFileWriter.Current.Save();
        meterProvider?.Dispose();
        tracerProvider?.Dispose();
    }

    private static int RunSmokeTest()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var requiredAssets = new[]
        {
            Path.Combine(baseDirectory, "Assets"),
            Path.Combine(baseDirectory, "ETS2LA.dll")
        };

        var missing = requiredAssets.Where(path => !File.Exists(path) && !Directory.Exists(path)).ToArray();
        if (missing.Length > 0)
        {
            Console.Error.WriteLine($"Smoke test failed; missing: {string.Join(", ", missing)}");
            return 1;
        }

        Console.WriteLine($"Smoke test passed for ETS2LA Hardened CN {typeof(Program).Assembly.GetName().Version}");
        return 0;
    }
}
