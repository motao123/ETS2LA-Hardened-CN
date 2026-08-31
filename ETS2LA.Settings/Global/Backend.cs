namespace ETS2LA.Settings
{
    [Serializable]
    public class BackendSettings
    {
        /// <summary>
        ///  启动时自动启用所有已加载的插件，无需在插件管理器中逐个手动开启。
        ///  这是“启动即全自动”目标的一部分。
        /// </summary>
        public bool AutoEnablePluginsOnStartup = true;
    }

    public class BackendSettingsHandler
    {
        private static readonly Lazy<BackendSettingsHandler> _instance = new(() => new BackendSettingsHandler());
        public static BackendSettingsHandler Current => _instance.Value;

        private SettingsHandler _settingsHandler;
        private BackendSettings _settings;

        public event Action<BackendSettings>? OnSettingsChanged;

        public BackendSettingsHandler()
        {
            _settingsHandler = new SettingsHandler();
            _settings = _settingsHandler.Load<BackendSettings>("BackendSettings.json") ?? new BackendSettings();
            _settingsHandler.RegisterListener<BackendSettings>("BackendSettings.json", s =>
            {
                _settings = s;
                OnSettingsChanged?.Invoke(s);
            });
        }

        public void Save()
        {
            _settingsHandler.Save("BackendSettings.json", _settings);
        }

        public BackendSettings GetSettings()
        {
            return _settings;
        }
    }
}
