using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using YuWanCard.Malice;
using YuWanCard.Modifiers;

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
        int preferred = screen.Lobby.NetService.Type == NetGameType.Client
            ? MaliceModifierPatchHelpers.GetMaliceLevel(screen.Lobby.Modifiers)
            : MaliceManager.GetPreferredMalice(character.Id);
        if (screen.Lobby.NetService.Type == NetGameType.Client)
        {
            max = Math.Max(max, preferred);
        }

        malicePanel.SetMaxAscension(max);
        malicePanel.SetAscensionLevel(Math.Min(max, preferred));

        if (!malicePanel.HasMeta("YUWANCARD_MALICE_CONNECTED"))
        {
            malicePanel.Connect(NAscensionPanel.SignalName.AscensionLevelChanged, Callable.From(() => OnMaliceChanged(screen, malicePanel)));
            malicePanel.SetMeta("YUWANCARD_MALICE_CONNECTED", true);
        }

        MalicePanelStyler.Apply(malicePanel);
        SyncLobbyMalice(screen, malicePanel.Ascension);
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
        SyncLobbyMalice(screen, panel.Ascension);
        MalicePanelStyler.Apply(panel);
    }

    private static void SyncLobbyMalice(NCharacterSelectScreen screen, int maliceLevel)
    {
        if (screen.Lobby.NetService.Type != NetGameType.Host)
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

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.BeginRun))]
public static class MaliceCharacterSelectBeginRunPatch
{
    [HarmonyPrefix]
    public static void Prefix(NCharacterSelectScreen __instance, ref IReadOnlyList<ModifierModel> modifiers)
    {
        if (__instance.Lobby.GameMode != MegaCrit.Sts2.Core.Runs.GameMode.Standard || modifiers.Count == 0)
        {
            return;
        }

        MaliceModifierPatchHelpers.SetPendingRunModifiers(__instance.Lobby, modifiers);

        if (modifiers.All(modifier => modifier is MaliceModifier))
        {
            modifiers = Array.Empty<ModifierModel>();
        }
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.ModifiersChanged))]
public static class MaliceCharacterSelectModifiersChangedPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NCharacterSelectScreen __instance)
    {
        MaliceCharacterSelectSyncPatch.SyncMalicePanel(__instance);
        return false;
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
