using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using YuWanCard.Malice;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen._Ready))]
public static class MaliceCharacterSelectReadyPatch
{
    private const float MalicePanelTopGap = 16f;
    private const float MalicePanelFallbackOffsetY = 145f;

    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        if (__instance.GetNodeOrNull<NAscensionPanel>("MalicePanel") != null)
        {
            return;
        }

        NAscensionPanel? ascensionPanel = __instance.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        if (ascensionPanel == null || ascensionPanel.GetParent() == null)
        {
            return;
        }

        var malicePanel = (NAscensionPanel)ascensionPanel.Duplicate();
        malicePanel.Name = "MalicePanel";
        ascensionPanel.GetParent().AddChild(malicePanel);
        malicePanel.Position = GetMalicePanelPosition(ascensionPanel);
        malicePanel.SetMeta("YUWANCARD_MALICE_PANEL", true);
        EnsureUniqueVisualResources(malicePanel);
        malicePanel.Visible = false;
    }

    private static Vector2 GetMalicePanelPosition(NAscensionPanel ascensionPanel)
    {
        float offsetY = ascensionPanel.Size.Y > 0f
            ? ascensionPanel.Size.Y + MalicePanelTopGap
            : MalicePanelFallbackOffsetY;
        return ascensionPanel.Position + new Vector2(0f, -offsetY);
    }

    private static void EnsureUniqueVisualResources(NAscensionPanel malicePanel)
    {
        var icon = malicePanel.GetNodeOrNull<Control>("%AscensionIcon");
        if (icon?.Material is not ShaderMaterial shader)
        {
            return;
        }

        icon.Material = (ShaderMaterial)shader.Duplicate();
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class MaliceCharacterSelectSyncPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        SyncMalicePanel(__instance);
    }

    internal static void SyncMalicePanel(NCharacterSelectScreen screen)
    {
        EnsureMalicePanelInitialized(screen);

        var selectedButton = AccessTools.Field(typeof(NCharacterSelectScreen), "_selectedButton")?.GetValue(screen) as NCharacterSelectButton;
        var malicePanel = screen.GetNodeOrNull<NAscensionPanel>("MalicePanel");
        if (selectedButton?.Character == null || malicePanel == null)
        {
            return;
        }

        var character = selectedButton.Character;
        int max = MaliceManager.GetAvailableSelectionMax(character.Id);
        int preferred = MaliceManager.GetPreferredMalice(character.Id);

        malicePanel.SetMaxAscension(max);
        malicePanel.SetAscensionLevel(Math.Min(max, preferred));

        if (!malicePanel.HasMeta("YUWANCARD_MALICE_CONNECTED"))
        {
            malicePanel.Connect(NAscensionPanel.SignalName.AscensionLevelChanged, Callable.From(() => OnMaliceChanged(screen, malicePanel)));
            malicePanel.SetMeta("YUWANCARD_MALICE_CONNECTED", true);
        }

        MalicePanelStyler.Apply(malicePanel);
    }

    internal static void EnsureMalicePanelInitialized(NCharacterSelectScreen screen)
    {
        var malicePanel = screen.GetNodeOrNull<NAscensionPanel>("MalicePanel");
        if (malicePanel == null || malicePanel.HasMeta("YUWANCARD_MALICE_INITIALIZED"))
        {
            return;
        }

        MultiplayerUiMode mode = screen.Lobby.NetService.Type switch
        {
            NetGameType.Host => MultiplayerUiMode.Host,
            NetGameType.Client => MultiplayerUiMode.Client,
            _ => MultiplayerUiMode.Singleplayer
        };

        malicePanel.Initialize(mode);
        malicePanel.SetMeta("YUWANCARD_MALICE_INITIALIZED", true);
    }

    private static void OnMaliceChanged(NCharacterSelectScreen screen, NAscensionPanel panel)
    {
        var selectedButton = AccessTools.Field(typeof(NCharacterSelectScreen), "_selectedButton")?.GetValue(screen) as NCharacterSelectButton;
        if (selectedButton?.Character == null)
        {
            return;
        }

        MaliceManager.SetPreferredMalice(selectedButton.Character.Id, panel.Ascension);
        MalicePanelStyler.Apply(panel);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuOpened))]
public static class MaliceCharacterSelectOpenedPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        MaliceCharacterSelectSyncPatch.SyncMalicePanel(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsHost))]
public static class MaliceCharacterSelectHostInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        MaliceCharacterSelectSyncPatch.EnsureMalicePanelInitialized(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsClient))]
public static class MaliceCharacterSelectClientInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        MaliceCharacterSelectSyncPatch.EnsureMalicePanelInitialized(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeSingleplayer))]
public static class MaliceCharacterSelectSingleplayerInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        MaliceCharacterSelectSyncPatch.EnsureMalicePanelInitialized(__instance);
    }
}

[HarmonyPatch(typeof(NAscensionPanel), "RefreshAscensionText")]
public static class MaliceAscensionPanelTextPatch
{
    [HarmonyPostfix]
    public static void Postfix(NAscensionPanel __instance)
    {
        if (!MalicePanelStyler.IsMalicePanel(__instance))
        {
            return;
        }

        MalicePanelStyler.Apply(__instance);
    }
}

internal static class MalicePanelStyler
{
    private const string MaliceLocTable = "modifiers";
    private const string MaliceLocPrefix = "YUWANCARD-MALICE";

    public static bool IsMalicePanel(NAscensionPanel panel) =>
        panel.HasMeta("YUWANCARD_MALICE_PANEL") && panel.GetMeta("YUWANCARD_MALICE_PANEL").AsBool();

    public static void Apply(NAscensionPanel panel)
    {
        if (!IsMalicePanel(panel))
        {
            return;
        }

        var info = panel.GetNodeOrNull<RichTextLabel>("HBoxContainer/AscensionDescription/Description");
        var levelLabel = panel.GetNodeOrNull<Label>("HBoxContainer/AscensionIconContainer/AscensionIcon/AscensionLevel");
        var icon = panel.GetNodeOrNull<Control>("%AscensionIcon");

        if (icon?.Material is ShaderMaterial shader)
        {
            shader.SetShaderParameter("h", 0.82f);
            shader.SetShaderParameter("v", 1.1f);
        }

        if (levelLabel != null)
        {
            levelLabel.Text = panel.Ascension.ToString();
        }

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
