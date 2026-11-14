using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Malice;
using YuWanCard.Modifiers;

namespace YuWanCard.Patches;

/// <summary>
/// Shared malice selector plumbing for the standalone Blitz and Dread run screens.
/// Those screens have no character-select screen, so the malice panel has to be
/// built there and the selection has to be injected into the run by hand (the
/// vanilla screens always pass an empty modifier list).
/// </summary>
internal static class MaliceModePanel
{
    internal const string PanelNodeName = "MalicePanel";

    private const string InitializedMeta = "YUWANCARD_MALICE_INITIALIZED";
    private const string ConnectedMeta = "YUWANCARD_MALICE_CONNECTED";
    private const float PanelTopGap = 16f;

    public static NAscensionPanel? Get(Node screen) => screen.GetNodeOrNull<NAscensionPanel>(PanelNodeName);

    /// <summary>
    /// Duplicates an existing ascension panel (Blitz has one) and parks the copy right above it.
    /// </summary>
    public static NAscensionPanel? EnsureFromTemplate(Node screen, NAscensionPanel? template)
    {
        var existing = Get(screen);
        if (existing != null)
        {
            return existing;
        }

        if (template?.GetParent() == null)
        {
            return null;
        }

        var panel = (NAscensionPanel)template.Duplicate();
        panel.Name = PanelNodeName;
        MalicePanelStyler.MakeMaterialsUnique(panel);
        template.GetParent().AddChild(panel);

        // Work with raw offsets (not Position) so the placement is correct even when this
        // runs during _Ready, before the parent control has been laid out.
        float panelHeight = template.OffsetBottom - template.OffsetTop;
        if (panelHeight <= 0f)
        {
            panelHeight = template.Size.Y > 0f ? template.Size.Y : 117f;
        }

        float shift = panelHeight + PanelTopGap;
        panel.OffsetLeft = template.OffsetLeft;
        panel.OffsetRight = template.OffsetRight;
        panel.OffsetTop = template.OffsetTop - shift;
        panel.OffsetBottom = template.OffsetBottom - shift;
        panel.SetMeta("YUWANCARD_MALICE_PANEL", true);
        panel.Visible = false;
        MainFile.Logger.Info("[MaliceMode] Created malice panel from ascension template");
        return panel;
    }

    /// <summary>
    /// Instantiates a fresh ascension panel scene (Dread has no ascension panel at all).
    /// The scene keeps its own bottom-center layout.
    /// </summary>
    public static NAscensionPanel? EnsureFromScene(Node screen)
    {
        var existing = Get(screen);
        if (existing != null)
        {
            return existing;
        }

        var panel = SceneHelper.Instantiate<NAscensionPanel>("screens/ascension_panel");
        panel.Name = PanelNodeName;
        screen.AddChild(panel);
        panel.SetMeta("YUWANCARD_MALICE_PANEL", true);
        panel.Visible = false;
        MainFile.Logger.Info("[MaliceMode] Created malice panel from ascension scene");
        return panel;
    }

    public static void Sync(
        NAscensionPanel? panel,
        StartRunLobby? lobby,
        CharacterModel? character,
        Action<int>? persistSelection)
    {
        if (panel == null || lobby == null)
        {
            return;
        }

        if (!Config.YuWanCardConfig.EnableMaliceSelection)
        {
            panel.Visible = false;
            return;
        }

        EnsureInitialized(panel, lobby.NetService.Type);

        int lobbyLevel = MaliceModifierPatchHelpers.GetMaliceLevel(lobby.Modifiers);
        if (lobby.NetService.Type == NetGameType.Client)
        {
            panel.SetMaxAscension(lobbyLevel);
            panel.SetAscensionLevel(lobbyLevel);
            ApplyClientReadOnlyState(panel);
        }
        else
        {
            if (character == null || character.Id == ModelId.none)
            {
                panel.SetMaxAscension(0);
                return;
            }

            int max = MaliceManager.GetAvailableSelectionMax(character.Id);
            int preferred = lobbyLevel > 0 ? lobbyLevel : MaliceManager.GetPreferredMalice(character.Id);
            panel.SetMaxAscension(max);
            panel.SetAscensionLevel(Math.Clamp(preferred, 0, max));
        }

        if (persistSelection != null && !panel.HasMeta(ConnectedMeta))
        {
            panel.Connect(NAscensionPanel.SignalName.AscensionLevelChanged, Callable.From(() => persistSelection(panel.Ascension)));
            panel.SetMeta(ConnectedMeta, true);
        }

        MalicePanelStyler.Apply(panel);

        if (persistSelection != null && lobby.NetService.Type != NetGameType.Client)
        {
            persistSelection(panel.Ascension);
        }
    }

    /// <summary>
    /// Mirrors the selected malice level into the lobby modifier list so both the
    /// vanilla lobby begin-run message and multiplayer peers see it.
    /// </summary>
    public static void SyncLobby(StartRunLobby? lobby, int level)
    {
        if (lobby == null || lobby.NetService.Type == NetGameType.Client)
        {
            return;
        }

        List<ModifierModel> modifiers = lobby.Modifiers
            .Where(modifier => modifier is not MaliceModifier)
            .Select(modifier => ModifierModel.FromSerializable(modifier.ToSerializable()))
            .ToList();

        if (level > 0)
        {
            modifiers.Add(MaliceModifierPatchHelpers.CreateMaliceModifier(level));
        }

        if (HaveSameModifiers(lobby.Modifiers, modifiers))
        {
            return;
        }

        lobby.SetModifiers(modifiers);
    }

    public static void Cleanup(NAscensionPanel? panel)
    {
        panel?.Cleanup();
    }

    private static void EnsureInitialized(NAscensionPanel panel, NetGameType netType)
    {
        if (panel.HasMeta(InitializedMeta))
        {
            return;
        }

        MultiplayerUiMode mode = netType switch
        {
            NetGameType.Host => MultiplayerUiMode.Host,
            NetGameType.Client => MultiplayerUiMode.Client,
            _ => MultiplayerUiMode.Singleplayer
        };

        // Always initialize as Standard: this is the malice selector, so its arrows must
        // stay usable even inside Dread (whose vanilla ascension panel hides them).
        panel.Initialize(mode, GameMode.Standard);
        panel.SetMeta(InitializedMeta, true);
    }

    private static void ApplyClientReadOnlyState(NAscensionPanel panel)
    {
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.GetNodeOrNull<Control>("HBoxContainer/LeftArrowContainer")?.Set("mouse_filter", (int)Control.MouseFilterEnum.Ignore);
        panel.GetNodeOrNull<Control>("HBoxContainer/RightArrowContainer")?.Set("mouse_filter", (int)Control.MouseFilterEnum.Ignore);
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

[HarmonyPatch(typeof(NBlitzScreen))]
public static class MaliceBlitzScreenPatches
{
    private static readonly AccessTools.FieldRef<NBlitzScreen, NCharacterSelectButton?> SelectedButtonRef =
        AccessTools.FieldRefAccess<NBlitzScreen, NCharacterSelectButton?>("_selectedButton");

    private static readonly AccessTools.FieldRef<NBlitzScreen, NAscensionPanel?> AscensionPanelRef =
        AccessTools.FieldRefAccess<NBlitzScreen, NAscensionPanel?>("_ascensionPanel");

    private static NAscensionPanel? EnsurePanel(NBlitzScreen screen)
    {
        var template = AscensionPanelRef(screen) ?? screen.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        return MaliceModePanel.EnsureFromTemplate(screen, template);
    }

    private static void Sync(NBlitzScreen screen)
    {
        if (screen.Lobby is not { } lobby)
        {
            return;
        }

        MaliceModePanel.Sync(EnsurePanel(screen), lobby, GetCharacter(screen), level => Persist(screen, level));
    }

    private static CharacterModel? GetCharacter(NBlitzScreen screen)
    {
        var button = SelectedButtonRef(screen);
        return button is { IsLocked: false } ? button.Character : null;
    }

    private static void Persist(NBlitzScreen screen, int level)
    {
        var character = SelectedButtonRef(screen)?.Character;
        if (character != null)
        {
            MaliceManager.SetPreferredMalice(character.Id, level);
        }

        MaliceModePanel.SyncLobby(screen.Lobby, level);
    }

    [HarmonyPatch(nameof(NBlitzScreen._Ready))]
    [HarmonyPostfix]
    public static void ReadyPostfix(NBlitzScreen __instance)
    {
        EnsurePanel(__instance);
    }

    [HarmonyPatch(nameof(NBlitzScreen.InitializeSingleplayer))]
    [HarmonyPostfix]
    public static void InitializeSingleplayerPostfix(NBlitzScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NBlitzScreen.InitializeMultiplayerAsHost))]
    [HarmonyPostfix]
    public static void InitializeHostPostfix(NBlitzScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NBlitzScreen.InitializeMultiplayerAsClient))]
    [HarmonyPostfix]
    public static void InitializeClientPostfix(NBlitzScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NBlitzScreen.OnSubmenuOpened))]
    [HarmonyPostfix]
    public static void OpenedPostfix(NBlitzScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NBlitzScreen.SelectCharacter))]
    [HarmonyPostfix]
    public static void SelectCharacterPostfix(NBlitzScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NBlitzScreen.MaxAscensionChanged))]
    [HarmonyPostfix]
    public static void MaxAscensionChangedPostfix(NBlitzScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NBlitzScreen.ModifiersChanged))]
    [HarmonyPrefix]
    public static bool ModifiersChangedPrefix(NBlitzScreen __instance)
    {
        // Vanilla throws here because Blitz "supports no modifiers".
        Sync(__instance);
        return false;
    }

    [HarmonyPatch(nameof(NBlitzScreen.BeginRun))]
    [HarmonyPrefix]
    public static void BeginRunPrefix(NBlitzScreen __instance, ref IReadOnlyList<ModifierModel> modifiers)
    {
        var lobby = __instance.Lobby;
        if (lobby == null)
        {
            return;
        }

        int level = 0;
        if (Config.YuWanCardConfig.EnableMaliceSelection)
        {
            level = MaliceModePanel.Get(__instance)?.Ascension
                ?? MaliceModifierPatchHelpers.GetMaliceLevel(lobby.Modifiers);
        }

        var ensured = MaliceModifierPatchHelpers.EnsureMaliceModifier(modifiers, level);
        modifiers = ensured;
        MaliceModifierPatchHelpers.SetPendingRunModifiers(lobby, ensured);
        if (lobby.NetService.Type == NetGameType.Singleplayer)
        {
            MaliceModifierPatchHelpers.SetPendingSingleplayerModifiers(ensured);
        }

        MainFile.Logger.Info($"[MaliceMode] Blitz begin run malice={level} net={lobby.NetService.Type} modifiers=[{string.Join(", ", ensured.Select(m => m.Id.Entry))}]");
    }

    [HarmonyPatch(nameof(NBlitzScreen.OnSubmenuClosed))]
    [HarmonyPostfix]
    public static void ClosedPostfix(NBlitzScreen __instance)
    {
        MaliceModePanel.Cleanup(MaliceModePanel.Get(__instance));
    }
}

[HarmonyPatch(typeof(NDreadScreen))]
public static class MaliceDreadScreenPatches
{
    private static readonly AccessTools.FieldRef<NDreadScreen, NCharacterSelectButton?> SelectedButtonRef =
        AccessTools.FieldRefAccess<NDreadScreen, NCharacterSelectButton?>("_selectedButton");

    private static NAscensionPanel? EnsurePanel(NDreadScreen screen) => MaliceModePanel.EnsureFromScene(screen);

    private static void Sync(NDreadScreen screen)
    {
        if (screen.Lobby is not { } lobby)
        {
            return;
        }

        MaliceModePanel.Sync(EnsurePanel(screen), lobby, GetCharacter(screen), level => Persist(screen, level));
    }

    private static CharacterModel? GetCharacter(NDreadScreen screen)
    {
        var button = SelectedButtonRef(screen);
        return button is { IsLocked: false } ? button.Character : null;
    }

    private static void Persist(NDreadScreen screen, int level)
    {
        var character = SelectedButtonRef(screen)?.Character;
        if (character != null)
        {
            MaliceManager.SetPreferredMalice(character.Id, level);
        }

        MaliceModePanel.SyncLobby(screen.Lobby, level);
    }

    [HarmonyPatch(nameof(NDreadScreen._Ready))]
    [HarmonyPostfix]
    public static void ReadyPostfix(NDreadScreen __instance)
    {
        EnsurePanel(__instance);
    }

    [HarmonyPatch(nameof(NDreadScreen.InitializeSingleplayer))]
    [HarmonyPostfix]
    public static void InitializeSingleplayerPostfix(NDreadScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NDreadScreen.InitializeMultiplayerAsHost))]
    [HarmonyPostfix]
    public static void InitializeHostPostfix(NDreadScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NDreadScreen.InitializeMultiplayerAsClient))]
    [HarmonyPostfix]
    public static void InitializeClientPostfix(NDreadScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NDreadScreen.OnSubmenuOpened))]
    [HarmonyPostfix]
    public static void OpenedPostfix(NDreadScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NDreadScreen.SelectCharacter))]
    [HarmonyPostfix]
    public static void SelectCharacterPostfix(NDreadScreen __instance) => Sync(__instance);

    [HarmonyPatch(nameof(NDreadScreen.ModifiersChanged))]
    [HarmonyPrefix]
    public static bool ModifiersChangedPrefix(NDreadScreen __instance)
    {
        // Vanilla throws here because Dread "supports no modifiers".
        Sync(__instance);
        return false;
    }

    [HarmonyPatch(nameof(NDreadScreen.BeginRun))]
    [HarmonyPrefix]
    public static void BeginRunPrefix(NDreadScreen __instance, ref IReadOnlyList<ModifierModel> modifiers)
    {
        var lobby = __instance.Lobby;
        if (lobby == null)
        {
            return;
        }

        int level = 0;
        if (Config.YuWanCardConfig.EnableMaliceSelection)
        {
            level = MaliceModePanel.Get(__instance)?.Ascension
                ?? MaliceModifierPatchHelpers.GetMaliceLevel(lobby.Modifiers);
        }

        var ensured = MaliceModifierPatchHelpers.EnsureMaliceModifier(modifiers, level);
        modifiers = ensured;
        MaliceModifierPatchHelpers.SetPendingRunModifiers(lobby, ensured);
        if (lobby.NetService.Type == NetGameType.Singleplayer)
        {
            MaliceModifierPatchHelpers.SetPendingSingleplayerModifiers(ensured);
        }

        MainFile.Logger.Info($"[MaliceMode] Dread begin run malice={level} net={lobby.NetService.Type} modifiers=[{string.Join(", ", ensured.Select(m => m.Id.Entry))}]");
    }

    [HarmonyPatch(nameof(NDreadScreen.OnSubmenuClosed))]
    [HarmonyPostfix]
    public static void ClosedPostfix(NDreadScreen __instance)
    {
        MaliceModePanel.Cleanup(MaliceModePanel.Get(__instance));
    }
}
