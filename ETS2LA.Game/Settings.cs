using ETS2LA.Settings;

namespace ETS2LA.Game;

[Serializable]
public class GameSettings
{
    public List<string> ManualGamePaths { get; set; } = new();

    [NonSerialized]
    private static readonly Lazy<GameSettings> _instance = new(() => new GameSettings(loadSettings: true));
    public static GameSettings Current => _instance.Value;

    [NonSerialized]
    private SettingsHandler? _settingsHandler;

    public GameSettings(bool loadSettings = false)
    {
        if (loadSettings)
        {
            _settingsHandler = new SettingsHandler();
            var loadedSettings = _settingsHandler.Load<GameSettings>("GameSettings.json");
            if (loadedSettings != null)
            {
                ManualGamePaths = loadedSettings.ManualGamePaths;
            }
        }
    }

    public GameSettings() { }

    public void Save()
    {
        _settingsHandler?.Save<GameSettings>("GameSettings.json", this);
    }
}
