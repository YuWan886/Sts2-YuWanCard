using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace YuWanCard.Config;

public class YuWanCardConfig : SimpleModConfig
{
    [ConfigSection("性能设置")]
    [ConfigHoverTip]
    public static bool ForceDisableVSync { get; set; } = false;

    public YuWanCardConfig() : base() { }

    public void ApplyVSyncSetting()
    {
        if (ForceDisableVSync)
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            if (SaveManager.Instance?.SettingsSave != null)
            {
                SaveManager.Instance.SettingsSave.VSync = VSyncType.Off;
            }
            MainFile.Logger.Info("VSync forcibly disabled by YuWanCard config");
        }
    }
}
