using ETS2LA.Logging;
using ETS2LA.Controls;
using ETS2LA.Audio;

using Spectre.Console;

namespace ETS2LA.Backend
{
    /// <summary>
    ///  This class represents the plugin backend in ETS2LA. Every action to do with plugins
    ///  will one way or another go through this class. <br/> 
    ///  You usually shouldn't access it, but if you do, then use `PluginBackend.Current`.
    /// </summary>
    public class PluginBackend
    {
        private static readonly Lazy<PluginBackend> _instance = new(() => new PluginBackend());

        /// <summary>
        ///  This Instance property gives access to the ETS2LA-wide backend instance.
        ///  No matter where this is called from, it will always return the same instance.
        /// </summary>
        public static PluginBackend Current => _instance.Value;

        /// <summary>
        ///  The PluginHandler is what actually manages the plugins.
        /// </summary>
        public PluginHandler? PluginHandler;
        /// <summary>
        ///  This event is fired when the backend has been loaded.
        /// </summary>
        public event EventHandler? OnBackendLoaded;
        /// <summary>
        ///  Is the backing loaded?
        /// </summary>
        public bool IsLoaded = false;

        public void Start()
        {
            Logger.Console.Status().Start("Starting ETS2LA...", ctx =>
            {
                PluginHandler = new PluginHandler();
                CopyBundledPlugins();
                PluginHandler.LoadLibraries();
                PluginHandler.LoadPlugins();
                Thread.Sleep(1000);
                AutoEnablePlugins();

                Logger.Success("ETS2LA is running.");
                OnBackendLoaded?.Invoke(this, EventArgs.Empty);
                IsLoaded = true;
            });
        }

        /// <summary>
        ///  Copies plugin DLLs bundled with the app (Assets/BundledPlugins) into the
        ///  AppData plugin directory so release packages can ship first-party plugins
        ///  (e.g. AutoBehavior) without a manual install step. Files are only copied
        ///  when missing or older than the bundled copy.
        /// </summary>
        private void CopyBundledPlugins()
        {
            try
            {
                string source = Path.Combine(AppContext.BaseDirectory, "Assets", "BundledPlugins");
                if (!Directory.Exists(source))
                    return;

                string target = Path.Combine(ETS2LA.Settings.SettingsHandler.ConfigurationDirectory, "Plugins");
                Directory.CreateDirectory(target);

                foreach (string file in Directory.GetFiles(source, "*.dll"))
                {
                    string destination = Path.Combine(target, Path.GetFileName(file));
                    try
                    {
                        if (File.Exists(destination) &&
                            File.GetLastWriteTimeUtc(destination) >= File.GetLastWriteTimeUtc(file))
                            continue;

                        File.Copy(file, destination, overwrite: true);
                        Logger.Info($"Installed bundled plugin: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Could not install bundled plugin {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Bundled plugin installation failed: {ex.Message}");
            }
        }

        /// <summary>
        ///  Enables every loaded plugin so the user does not have to toggle them
        ///  one by one in the plugin manager after each launch (full autonomy mode).
        ///  Can be turned off via BackendSettings.json.
        /// </summary>
        private void AutoEnablePlugins()
        {
            var settings = ETS2LA.Settings.BackendSettingsHandler.Current.GetSettings();
            if (!settings.AutoEnablePluginsOnStartup || PluginHandler == null)
                return;

            int enabled = 0;
            foreach (var plugin in PluginHandler.LoadedPlugins.ToList())
            {
                try
                {
                    if (!plugin._IsRunning && PluginHandler.EnablePlugin(plugin))
                        enabled++;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to auto-enable plugin {plugin.Info.Name}: {ex}");
                }
            }

            if (enabled > 0)
                Logger.Info($"Auto-enabled {enabled} plugin(s) on startup.");
        }

        public void Shutdown()
        {
            if (PluginHandler != null)
            {
                PluginHandler.UnloadPlugins();
            }
            ControlsBackend.Current.Shutdown();
            AudioHandler.Current.Shutdown();
            Logger.Info("Backend shutdown complete.");
        }
    }
}