using BaseLib.Config;

namespace YuWanCard.Config;

public class YuWanCardConfig : FallbackSimpleModConfig
{
    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    public static bool EnableDeathEffect { get; set; } = true;

    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    public static bool EnableCustomCursor { get; set; } = true;

    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    [ConfigSlider(0.1, 10.0, 0.1, "{0}x")]
    public static double CursorScale { get; set; } = 2.0;

    [ConfigSection("多人游戏设置")]
    [ConfigHoverTip]
    public static bool BypassModelDbHashCheck { get; set; } = false;

    [ConfigSection("更新设置")]
    [ConfigHoverTip]
    public static bool EnableAutoUpdateCheck { get; set; } = true;

    [ConfigSection("游戏设置")]
    [ConfigHoverTip]
    public static bool EnableSevenCursesRing { get; set; } = true;

    [ConfigSection("游戏设置")]
    [ConfigHoverTip]
    public static bool EnableWhatIfRelics { get; set; } = false;

    public YuWanCardConfig() : base() { }
}
