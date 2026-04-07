using BaseLib.Config;

namespace YuWanCard.Config;

public class YuWanCardConfig : SimpleModConfig
{
    [ConfigSection("多人游戏设置")]
    [ConfigHoverTip]
    public static bool BypassModelDbHashCheck { get; set; } = false;

    [ConfigSection("更新设置")]
    [ConfigHoverTip]
    public static bool EnableAutoUpdateCheck { get; set; } = true;

    public YuWanCardConfig() : base() { }
}
