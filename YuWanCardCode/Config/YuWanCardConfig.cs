using BaseLib.Config;

namespace YuWanCard.Config;

public class YuWanCardConfig : SimpleModConfig
{
    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    public static bool ShowDeathOverlay { get; set; } = false;

    [ConfigSection("多人游戏设置")]
    [ConfigHoverTip]
    public static bool BypassModelDbHashCheck { get; set; } = false;

    public YuWanCardConfig() : base() { }
}
