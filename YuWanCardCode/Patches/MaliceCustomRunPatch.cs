using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using YuWanCard.Malice;
using YuWanCard.Modifiers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen._Ready))]
public static class MaliceCustomRunReadyPatch
{
    private const float MalicePanelScale = 0.7f;
    private const float MalicePanelTopGap = 8f;

    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        if (__instance.GetNodeOrNull<NAscensionPanel>("MalicePanel") != null)
        {
            return;
        }

        NAscensionPanel? ascensionPanel = __instance.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        if (ascensionPanel?.GetParent() == null)
        {
            return;
        }

        var malicePanel = (NAscensionPanel)ascensionPanel.Duplicate();
        malicePanel.Name = "MalicePanel";
        malicePanel.Scale = new Vector2(MalicePanelScale, MalicePanelScale);
        ApplyMalicePanelLayout(malicePanel, ascensionPanel);
        ApplyMalicePanelPivot(malicePanel);
        malicePanel.ZIndex = ascensionPanel.ZIndex;
        malicePanel.SetMeta("YUWANCARD_MALICE_PANEL", true);
        EnsureUniqueVisualResources(malicePanel);
        malicePanel.Visible = false;

        ascensionPanel.GetParent().AddChild(malicePanel);
        ascensionPanel.GetParent().MoveChild(malicePanel, ascensionPanel.GetIndex() + 1);
        MainFile.Logger.Info(
            $"[MaliceCustomRun] Created panel offsets=({malicePanel.OffsetLeft}, {malicePanel.OffsetTop}, {malicePanel.OffsetRight}, {malicePanel.OffsetBottom}) scale={malicePanel.Scale}");
    }

    private static void ApplyMalicePanelLayout(NAscensionPanel malicePanel, NAscensionPanel ascensionPanel)
    {
        float panelHeight = ascensionPanel.OffsetBottom - ascensionPanel.OffsetTop;
        float offsetY = panelHeight * MalicePanelScale + MalicePanelTopGap + 180f;

        malicePanel.OffsetLeft = ascensionPanel.OffsetLeft;
        malicePanel.OffsetRight = ascensionPanel.OffsetRight;
        malicePanel.OffsetTop = ascensionPanel.OffsetTop + offsetY;
        malicePanel.OffsetBottom = ascensionPanel.OffsetBottom + offsetY;
    }

    private static void ApplyMalicePanelPivot(NAscensionPanel malicePanel)
    {
        float width = malicePanel.OffsetRight - malicePanel.OffsetLeft;
        float height = malicePanel.OffsetBottom - malicePanel.OffsetTop;
        malicePanel.PivotOffset = new Vector2(width / 2f, height / 2f);
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

public static class MaliceCustomRunPanelSync
{
    public static void SyncMalicePanel(NCustomRunScreen screen)
    {
        EnsureMalicePanelInitialized(screen);

        var malicePanel = GetMalicePanel(screen);
        if (malicePanel == null)
        {
            return;
        }

        int authoritativeMaliceLevel = MaliceModifierPatchHelpers.GetMaliceLevel(screen.Lobby.Modifiers);
        if (screen.Lobby.NetService.Type == NetGameType.Client)
        {
            malicePanel.SetMaxAscension(authoritativeMaliceLevel);
            malicePanel.SetAscensionLevel(authoritativeMaliceLevel);
            ApplyClientReadOnlyState(malicePanel);
        }
        else
        {
            CharacterModel? character = GetSelectedCharacter(screen);
            if (character == null)
            {
                malicePanel.SetMaxAscension(0);
                MainFile.Logger.Info("[MaliceCustomRun] Sync skipped: selected character is null");
                return;
            }

            int max = MaliceManager.GetAvailableSelectionMax(character.Id);
            int preferred = authoritativeMaliceLevel > 0
                ? authoritativeMaliceLevel
                : MaliceManager.GetPreferredMalice(character.Id);
            malicePanel.SetMaxAscension(max);
            malicePanel.SetAscensionLevel(Math.Min(max, preferred));
        }

        if (!malicePanel.HasMeta("YUWANCARD_MALICE_CONNECTED"))
        {
            malicePanel.Connect(NAscensionPanel.SignalName.AscensionLevelChanged, Callable.From(() => OnMaliceChanged(screen, malicePanel)));
            malicePanel.SetMeta("YUWANCARD_MALICE_CONNECTED", true);
        }

        MalicePanelStyler.Apply(malicePanel);
        MainFile.Logger.Info(
            $"[MaliceCustomRun] Sync net={screen.Lobby.NetService.Type} level={malicePanel.Ascension} lobbyLevel={authoritativeMaliceLevel} visible={malicePanel.Visible} offsets=({malicePanel.OffsetLeft}, {malicePanel.OffsetTop}, {malicePanel.OffsetRight}, {malicePanel.OffsetBottom})");
        if (screen.Lobby.NetService.Type != NetGameType.Client)
        {
            SyncLobbyMalice(screen, malicePanel.Ascension);
        }
    }

    public static void EnsureMalicePanelInitialized(NCustomRunScreen screen)
    {
        var malicePanel = GetMalicePanel(screen);
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

    private static void OnMaliceChanged(NCustomRunScreen screen, NAscensionPanel panel)
    {
        if (screen.Lobby.NetService.Type == NetGameType.Client)
        {
            int authoritativeMaliceLevel = MaliceModifierPatchHelpers.GetMaliceLevel(screen.Lobby.Modifiers);
            if (panel.Ascension != authoritativeMaliceLevel)
            {
                panel.SetAscensionLevel(authoritativeMaliceLevel);
            }

            MalicePanelStyler.Apply(panel);
            return;
        }

        CharacterModel? character = GetSelectedCharacter(screen);
        if (character == null)
        {
            return;
        }

        MaliceManager.SetPreferredMalice(character.Id, panel.Ascension);
        SyncLobbyMalice(screen, panel.Ascension);
        MalicePanelStyler.Apply(panel);
        MainFile.Logger.Info($"[MaliceCustomRun] OnMaliceChanged character={character.Id.Entry} level={panel.Ascension}");
    }

    private static void ApplyClientReadOnlyState(NAscensionPanel panel)
    {
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.GetNodeOrNull<Control>("HBoxContainer/LeftArrowContainer")?.Set("mouse_filter", (int)Control.MouseFilterEnum.Ignore);
        panel.GetNodeOrNull<Control>("HBoxContainer/RightArrowContainer")?.Set("mouse_filter", (int)Control.MouseFilterEnum.Ignore);
    }

    private static void SyncLobbyMalice(NCustomRunScreen screen, int maliceLevel)
    {
        if (screen.Lobby.NetService.Type == NetGameType.Client)
        {
            return;
        }

        List<ModifierModel> modifiers = screen.Lobby.Modifiers
            .Where(modifier => modifier is not MaliceModifier)
            .Select(modifier => ModifierModel.FromSerializable(modifier.ToSerializable()))
            .ToList();

        if (maliceLevel > 0)
        {
            modifiers.Add(MaliceModifierPatchHelpers.CreateMaliceModifier(maliceLevel));
        }

        if (HaveSameModifiers(screen.Lobby.Modifiers, modifiers))
        {
            return;
        }

        screen.Lobby.SetModifiers(modifiers);
        MainFile.Logger.Info($"[MaliceCustomRun] SyncLobbyMalice level={maliceLevel} modifiers=[{string.Join(", ", modifiers.Select(static m => m.Id.Entry))}]");
    }

    private static CharacterModel? GetSelectedCharacter(NCustomRunScreen screen)
    {
        var selectedButton = AccessTools.Field(typeof(NCustomRunScreen), "_selectedButton")?.GetValue(screen) as NCharacterSelectButton;
        return selectedButton?.Character ?? screen.Lobby.LocalPlayer.character;
    }

    public static NAscensionPanel? GetMalicePanel(NCustomRunScreen screen)
    {
        NAscensionPanel? ascensionPanel = screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        return ascensionPanel?.GetParent()?.GetNodeOrNull<NAscensionPanel>("MalicePanel");
    }

    private static bool HaveSameModifiers(IReadOnlyList<ModifierModel> left, IReadOnlyList<ModifierModel> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (left[i].Id != right[i].Id)
            {
                return false;
            }

            if (left[i] is MaliceModifier leftMalice
                && right[i] is MaliceModifier rightMalice
                && leftMalice.EffectiveMaliceLevel != rightMalice.EffectiveMaliceLevel)
            {
                return false;
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.SelectCharacter))]
public static class MaliceCustomRunSelectCharacterPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        MaliceCustomRunPanelSync.SyncMalicePanel(__instance);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.OnSubmenuOpened))]
public static class MaliceCustomRunOpenedPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        MaliceCustomRunPanelSync.SyncMalicePanel(__instance);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsHost))]
public static class MaliceCustomRunHostInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        MaliceCustomRunPanelSync.EnsureMalicePanelInitialized(__instance);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsClient))]
public static class MaliceCustomRunClientInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        MaliceCustomRunPanelSync.EnsureMalicePanelInitialized(__instance);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeSingleplayer))]
public static class MaliceCustomRunSingleplayerInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        MaliceCustomRunPanelSync.EnsureMalicePanelInitialized(__instance);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.ModifiersChanged))]
public static class MaliceCustomRunModifiersChangedPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        MaliceCustomRunPanelSync.SyncMalicePanel(__instance);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), "OnModifiersListChanged")]
public static class MaliceCustomRunModifiersListChangedPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCustomRunScreen __instance)
    {
        MaliceCustomRunPanelSync.SyncMalicePanel(__instance);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.BeginRun))]
public static class MaliceCustomRunBeginRunPatch
{
    [HarmonyPrefix]
    public static void Prefix(NCustomRunScreen __instance, ref IReadOnlyList<ModifierModel> modifiers)
    {
        int maliceLevel = MaliceCustomRunPanelSync.GetMalicePanel(__instance)?.Ascension
            ?? MaliceModifierPatchHelpers.GetMaliceLevel(modifiers);
        modifiers = MaliceModifierPatchHelpers.EnsureMaliceModifier(modifiers, maliceLevel);
    }
}
