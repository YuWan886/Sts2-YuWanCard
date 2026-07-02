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

    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    public static bool EnablePigScaleWithHp { get; set; } = true;

    [ConfigSection("显示设置")]
    [ConfigHoverTip]
    [ConfigSlider(0.1, 3.0, 0.1, "{0}x")]
    public static double PigBaseScale { get; set; } = 1.0;

    [ConfigSection("更新设置")]
    [ConfigHoverTip]
    public static bool EnableAutoUpdateCheck { get; set; } = true;

    [ConfigSection("游戏设置")]
    [ConfigHoverTip]
    public static bool EnableSevenCursesRing { get; set; } = true;

    [ConfigSection("游戏设置")]
    [ConfigHoverTip]
    public static bool EnableMaliceSelection { get; set; } = true;

    [ConfigSection("游戏设置")]
    [ConfigHoverTip]
    public static bool EnablePigRewardAllCardPools { get; set; } = false;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableYuWanEnemyEncounters { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableIgnisBossEncounter { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableKillerEliteEncounter { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableYuWanEvents { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnablePigPigAncient { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableBlacksmithEvent { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableHelloHumanEvent { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableHorizonEvent { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableSkullGoldRushEvent { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableSunkenStatueQuestEvent { get; set; } = true;

    [ConfigSection("游戏内容设置")]
    [ConfigHoverTip]
    public static bool EnableZhiZhanZhiShangEvent { get; set; } = true;

    public YuWanCardConfig() : base() { }
}
