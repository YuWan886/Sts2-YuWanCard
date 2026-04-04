using BaseLib.Config;

namespace YuWanCard.Config;

public class YuWanCardConfig : SimpleModConfig
{
    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    public static bool ShowDeathOverlay { get; set; } = true;

    public YuWanCardConfig() : base() { }

}
