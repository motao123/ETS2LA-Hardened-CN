using ETS2LA.Settings;

namespace ETS2LA.State;


[Serializable]
public class StateSettings
{
    public Units DisplayUnits = Units.Metric;
    public int SpeedControlStepSize = 2;
    public bool SnapTo10s = true;
    /// <summary>
    ///  当游戏遥测连接成功时自动激活辅助（等效于按下 SET），实现启动即全自动。
    /// </summary>
    public bool AutoEngageOnStartup = true;
    /// <summary>
    ///  当辅助因驾驶员接管（踩刹车/打方向等）而暂停后，驾驶员松开输入且延迟满足时自动恢复辅助。
    /// </summary>
    public bool AutoResumeAfterIntervention = true;
    /// <summary>
    ///  驾驶员完全松开输入后，等待多少秒才自动恢复辅助。
    /// </summary>
    public float AutoResumeDelaySeconds = 3f;
}

public class StateSettingsHandler
{
    private static readonly Lazy<StateSettingsHandler> _instance = new(() => new StateSettingsHandler());
    public static StateSettingsHandler Current => _instance.Value;

    private SettingsHandler _settingsHandler;
    private StateSettings _settings;

    public event Action<StateSettings> OnSettingsChanged;

    public StateSettingsHandler()
    {
        _settingsHandler = new SettingsHandler();
        _settings = _settingsHandler.Load<StateSettings>("StateSettings.json");
        _settingsHandler.RegisterListener<StateSettings>("StateSettings.json", OnSettingsChangedInternal);
    }

    public void Save()
    {
        _settingsHandler.Save("StateSettings.json", _settings);
    }

    public StateSettings GetSettings()
    {
        return _settings;
    }

    private void OnSettingsChangedInternal(StateSettings stateSettings)
    {
        _settings = stateSettings;
        OnSettingsChanged?.Invoke(_settings);
    }
}
