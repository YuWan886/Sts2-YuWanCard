using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen._Ready))]
public static class BalatroCharacterSelectReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        BalatroCharacterSelectSyncPatch.SyncButton(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class BalatroCharacterSelectSelectCharacterPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        BalatroCharacterSelectSyncPatch.SyncButton(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuOpened))]
public static class BalatroCharacterSelectOpenedPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        BalatroCharacterSelectSyncPatch.SyncButton(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsHost))]
public static class BalatroCharacterSelectHostInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        BalatroCharacterSelectSyncPatch.SyncButton(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsClient))]
public static class BalatroCharacterSelectClientInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        BalatroCharacterSelectSyncPatch.SyncButton(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeSingleplayer))]
public static class BalatroCharacterSelectSingleplayerInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        BalatroCharacterSelectSyncPatch.SyncButton(__instance);
    }
}

internal static class BalatroCharacterSelectSyncPatch
{
    private const string ContainerName = "BalatroModeContainer";
    private const string TickboxName = "BalatroModeTickbox";
    private const string LabelName = "BalatroModeLabel";
    private const string SyncMetaKey = "YUWANCARD_BALATRO_SYNCING";
    private const float RightMargin = 48f;
    private const float TickboxSize = 56f;
    private const float DescriptionGap = 14f;
    private const float DescriptionWidth = 240f;

    public static void SyncButton(NCharacterSelectScreen screen)
    {
        NRunModifierTickbox? tickbox = EnsureTickbox(screen);
        if (tickbox == null)
        {
            return;
        }

        Control? container = GetContainer(screen);
        MegaRichTextLabel? label = GetExternalLabel(screen);
        if (container == null || label == null)
        {
            return;
        }

        if (screen.Lobby == null)
        {
            container.Visible = false;
            return;
        }

        bool visible = screen.Lobby.GameMode == GameMode.Standard;
        container.Visible = visible;
        if (!visible)
        {
            return;
        }

        bool enabled = BalatroCharacterSelectPatchHelpers.HasBalatroModifier(screen.Lobby.Modifiers);
        bool readOnly = screen.Lobby.NetService.Type == NetGameType.Client;

        tickbox.SetMeta(SyncMetaKey, true);
        tickbox.IsTicked = enabled;
        tickbox.SetMeta(SyncMetaKey, false);
        if (readOnly)
        {
            tickbox.Disable();
        }
        else
        {
            tickbox.Enable();
        }

        ApplyTickboxText(tickbox, label);
        tickbox.TooltipText = new LocString("modifiers", "YUWANCARD-BALATRO.neow_description").GetFormattedText();

        RefreshTickboxLayout(screen, container, tickbox, label);
    }

    private static NRunModifierTickbox? EnsureTickbox(NCharacterSelectScreen screen)
    {
        Control? container = EnsureContainer(screen);
        if (container == null)
        {
            return null;
        }

        if (container.GetNodeOrNull<NRunModifierTickbox>(TickboxName) is { } existing)
        {
            return existing;
        }

        NRunModifierTickbox? tickbox = NRunModifierTickbox.Create(BalatroCharacterSelectPatchHelpers.CreateBalatroModifier());
        if (tickbox == null)
        {
            return null;
        }

        tickbox.Name = TickboxName;
        tickbox.TooltipText = new LocString("modifiers", "YUWANCARD-BALATRO.neow_description").GetFormattedText();
        tickbox.Toggled += toggledTickbox => OnTickboxToggled(screen, toggledTickbox);
        container.AddChild(tickbox);

        MegaRichTextLabel? originalLabel = tickbox.GetNodeOrNull<MegaRichTextLabel>("%Description");
        if (originalLabel != null)
        {
            originalLabel.Visible = false;
            originalLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        MegaRichTextLabel? label = EnsureExternalLabel(screen, container, originalLabel);
        if (label != null)
        {
            ApplyTickboxText(tickbox, label);
        }

        RefreshTickboxLayout(screen, container, tickbox, label);
        return tickbox;
    }

    private static void OnTickboxToggled(NCharacterSelectScreen screen, NTickbox toggledTickbox)
    {
        NRunModifierTickbox? tickbox = TryGetTickbox(screen);
        if (tickbox == null || screen.Lobby == null)
        {
            return;
        }

        if (!ReferenceEquals(toggledTickbox, tickbox))
        {
            return;
        }

        if (tickbox.HasMeta(SyncMetaKey) && tickbox.GetMeta(SyncMetaKey).AsBool())
        {
            return;
        }

        if (screen.Lobby.NetService.Type == NetGameType.Client)
        {
            SyncButton(screen);
            return;
        }

        List<ModifierModel> modifiers = BalatroCharacterSelectPatchHelpers.EnsureBalatroModifier(
            screen.Lobby.Modifiers,
            tickbox.IsTicked);
        if (HaveSameModifiers(screen.Lobby.Modifiers, modifiers))
        {
            SyncButton(screen);
            return;
        }

        screen.Lobby.SetModifiers(modifiers);
        SyncButton(screen);
    }

    private static void RefreshTickboxLayout(
        NCharacterSelectScreen screen,
        Control container,
        NRunModifierTickbox tickbox,
        MegaRichTextLabel? label)
    {
        Vector2 screenSize = screen.Size;
        if (screenSize == Vector2.Zero)
        {
            screenSize = screen.GetViewportRect().Size;
        }

        float visualWidth = TickboxSize + DescriptionGap + DescriptionWidth;
        float x = Math.Max(0f, screenSize.X - RightMargin - visualWidth);
        float y = Math.Max(0f, (screenSize.Y - TickboxSize) * 0.5f);

        container.Position = new Vector2(x, y);
        container.Size = new Vector2(visualWidth, TickboxSize);
        container.CustomMinimumSize = container.Size;

        tickbox.Position = Vector2.Zero;
        tickbox.Size = new Vector2(TickboxSize, TickboxSize);
        tickbox.CustomMinimumSize = tickbox.Size;

        if (label != null)
        {
            label.Position = new Vector2(TickboxSize + DescriptionGap, 0f);
            label.Size = new Vector2(DescriptionWidth, TickboxSize);
            label.CustomMinimumSize = label.Size;
        }
    }

    private static void ApplyTickboxText(NRunModifierTickbox tickbox, MegaRichTextLabel externalLabel)
    {
        string title = new LocString("modifiers", "YUWANCARD-BALATRO.neow_title").GetFormattedText();
        externalLabel.Text = title;
        externalLabel.MouseFilter = Control.MouseFilterEnum.Ignore;

        if (tickbox.GetNodeOrNull<Control>("Highlight") is { } highlight)
        {
            highlight.Visible = false;
            highlight.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
    }

    private static Control? EnsureContainer(NCharacterSelectScreen screen)
    {
        if (GetContainer(screen) is { } existing)
        {
            return existing;
        }

        var container = new Control
        {
            Name = ContainerName,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        screen.AddChild(container);
        return container;
    }

    private static MegaRichTextLabel? EnsureExternalLabel(
        NCharacterSelectScreen screen,
        Control container,
        MegaRichTextLabel? sourceLabel)
    {
        if (GetExternalLabel(screen) is { } existing)
        {
            return existing;
        }

        MegaRichTextLabel label = sourceLabel?.Duplicate() as MegaRichTextLabel ?? new MegaRichTextLabel();
        label.Name = LabelName;
        label.Visible = true;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.FitContent = false;
        container.AddChild(label);
        return label;
    }

    private static Control? GetContainer(NCharacterSelectScreen screen) =>
        screen.GetNodeOrNull<Control>(ContainerName);

    private static MegaRichTextLabel? GetExternalLabel(NCharacterSelectScreen screen) =>
        GetContainer(screen)?.GetNodeOrNull<MegaRichTextLabel>(LabelName);

    private static NRunModifierTickbox? TryGetTickbox(NCharacterSelectScreen screen) =>
        GetContainer(screen)?.GetNodeOrNull<NRunModifierTickbox>(TickboxName);

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

internal static class BalatroCharacterSelectPatchHelpers
{
    public static bool HasBalatroModifier(IReadOnlyList<ModifierModel>? modifiers) =>
        modifiers?.OfType<BalatroModifier>().Any() == true;

    public static List<ModifierModel> EnsureBalatroModifier(IReadOnlyList<ModifierModel> modifiers, bool enabled)
    {
        List<ModifierModel> list = modifiers
            .Where(modifier => modifier is not BalatroModifier)
            .Select(modifier => ModifierModel.FromSerializable(modifier.ToSerializable()))
            .ToList();

        if (enabled)
        {
            list.Add(CreateBalatroModifier());
        }

        return list;
    }

    public static BalatroModifier CreateBalatroModifier()
    {
        return (BalatroModifier)ModelDb.GetById<ModifierModel>(ModelDb.GetId<BalatroModifier>()).ToMutable();
    }
}
