using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace YuWanCard.Patches;

internal static class MalicePanelStyler
{
    private const string MaliceLocTable = "modifiers";
    private const string MaliceLocPrefix = "YUWANCARD-MALICE";

    public static bool IsMalicePanel(NAscensionPanel panel) =>
        panel.HasMeta("YUWANCARD_MALICE_PANEL") && panel.GetMeta("YUWANCARD_MALICE_PANEL").AsBool();

    public static void Apply(NAscensionPanel panel)
    {
        if (!IsMalicePanel(panel))
            return;

        var info = panel.GetNodeOrNull<RichTextLabel>("HBoxContainer/AscensionDescription/Description");
        var levelLabel = panel.GetNodeOrNull<Label>("HBoxContainer/AscensionIconContainer/AscensionIcon/AscensionLevel");
        var icon = panel.GetNodeOrNull<Control>("%AscensionIcon");

        if (icon?.Material is ShaderMaterial shader)
        {
            shader.SetShaderParameter("h", 0.82f);
            shader.SetShaderParameter("v", 1.1f);
        }

        if (levelLabel != null)
            levelLabel.Text = panel.Ascension.ToString();

        if (info != null)
        {
            string title = GetTitle(panel.Ascension);
            string desc = GetDescription(panel.Ascension);
            info.Text = $"[b][purple]{title}[/purple][/b]\n{desc}";
        }
    }

    private static string GetTitle(int level) => GetLevelLocString(level, "title").GetFormattedText();

    private static string GetDescription(int level) => GetLevelLocString(level, "description").GetFormattedText();

    private static LocString GetLevelLocString(int level, string suffix)
    {
        int clampedLevel = Math.Clamp(level, 0, 10);
        return new LocString(MaliceLocTable, $"{MaliceLocPrefix}.LEVEL_{clampedLevel:00}.{suffix}");
    }
}
