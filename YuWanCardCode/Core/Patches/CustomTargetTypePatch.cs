using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace YuWanCard.Core.Patches;

internal static class CustomTargetingContext
{
    private static readonly AsyncLocal<CardModel?> CurrentCardSlot = new();

    internal static CardModel? CurrentCard => CurrentCardSlot.Value;

    internal static IDisposable Push(CardModel? card)
    {
        return new Scope(card);
    }

    private sealed class Scope : IDisposable
    {
        private readonly CardModel? _previous;

        public Scope(CardModel? card)
        {
            _previous = CurrentCardSlot.Value;
            CurrentCardSlot.Value = card;
        }

        public void Dispose()
        {
            CurrentCardSlot.Value = _previous;
        }
    }
}

internal static class CardPlayDelegates
{
    internal static readonly Func<NCardPlay, CardModel?> GetCard =
        AccessTools.MethodDelegate<Func<NCardPlay, CardModel?>>(
            AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "Card"));

    internal static readonly Func<NCardPlay, NCard?> GetCardNode =
        AccessTools.MethodDelegate<Func<NCardPlay, NCard?>>(
            AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "CardNode"));

    internal static readonly Action<NCardPlay> TryShowEvokingOrbs =
        AccessTools.MethodDelegate<Action<NCardPlay>>(
            AccessTools.DeclaredMethod(typeof(NCardPlay), "TryShowEvokingOrbs"));
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
internal static class ModelDbInitCustomTargetTypeRegistrationPatch
{
    public static void Postfix()
    {
        CustomTargetTypeRegistry.RegisterBuiltIns();
    }
}

[HarmonyPatch(typeof(NCardPlay), "ShowMultiCreatureTargetingVisuals")]
internal static class NCardPlayShowMultiCreatureTargetingVisualsCustomTargetTypePatch
{
    public static void Postfix(NCardPlay __instance)
    {
        if (__instance.Card is not { TargetType: var targetType })
            return;

        if (!CustomTargetType.IsCustomMultiTargetType(targetType))
            return;

        __instance.CardNode?.UpdateVisuals(
            __instance.Card.Pile!.Type,
            CardPreviewMode.MultiCreatureTargeting);

        var room = NCombatRoom.Instance;
        if (room == null)
            return;

        foreach (var creatureNode in room.CreatureNodes)
        {
            if (!CustomTargetTypeRegistry.TryShouldIncludeMultiTarget(targetType, creatureNode.Entity, out var include)
                || !include)
            {
                continue;
            }

            creatureNode.ShowMultiselectReticle();
        }
    }
}

[HarmonyPatch(typeof(ActionTargetExtensions), nameof(ActionTargetExtensions.IsSingleTarget))]
internal static class ActionTargetExtensionsIsSingleTargetCustomTargetTypePatch
{
    public static void Postfix(TargetType targetType, ref bool __result)
    {
        if (__result)
            return;

        if (CustomTargetType.IsCustomSingleTargetType(targetType))
            __result = true;
    }
}

[HarmonyPatch(typeof(NTargetManager), nameof(NTargetManager.AllowedToTargetCreature))]
internal static class NTargetManagerAllowedToTargetCreatureCustomTargetTypePatch
{
    public static bool Prefix(NTargetManager __instance, Creature creature, ref bool __result)
    {
        if (!CustomTargetTypeRegistry.TryIsAllowedSingleTarget(__instance._validTargetsType, CustomTargetingContext.CurrentCard, creature, out var allowed))
            return true;

        __result = allowed;
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlayTargeting))]
internal static class CardModelCanPlayTargetingCustomTargetTypePatch
{
    public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
    {
        if (target == null)
            return true;

        if (!CustomTargetTypeRegistry.TryIsAllowedSingleTarget(__instance.TargetType, __instance, target, out var allowed))
            return true;

        __result = allowed;
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget), typeof(Creature))]
internal static class CardModelIsValidTargetCustomTargetTypePatch
{
    public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
    {
        if (target == null)
            return true;

        if (!CustomTargetTypeRegistry.TryIsAllowedSingleTarget(__instance.TargetType, __instance, target, out var allowed))
            return true;

        __result = allowed;
        return false;
    }
}

[HarmonyPatch(typeof(NMouseCardPlay), "TargetSelection", typeof(TargetMode))]
internal static class NMouseCardPlayTargetSelectionCustomTargetTypePatch
{
    private static readonly Func<NMouseCardPlay, TargetMode, TargetType, Task> SingleCreatureTargeting =
        AccessTools.MethodDelegate<Func<NMouseCardPlay, TargetMode, TargetType, Task>>(
            AccessTools.DeclaredMethod(typeof(NMouseCardPlay), "SingleCreatureTargeting",
                [typeof(TargetMode), typeof(TargetType)]));

    public static bool Prefix(NMouseCardPlay __instance, TargetMode targetMode, ref Task __result)
    {
        var card = CardPlayDelegates.GetCard(__instance);
        if (card == null || !CustomTargetType.IsCustomSingleTargetType(card.TargetType))
            return true;

        __result = RunTargeting(__instance, targetMode, card.TargetType);
        return false;
    }

    private static async Task RunTargeting(NMouseCardPlay instance, TargetMode targetMode, TargetType targetType)
    {
        var cardNode = CardPlayDelegates.GetCardNode(instance);
        var card = CardPlayDelegates.GetCard(instance);
        if (cardNode == null)
            return;

        CardPlayDelegates.TryShowEvokingOrbs(instance);
        cardNode.CardHighlight.AnimFlash();
        using var _ = CustomTargetingContext.Push(card);
        await SingleCreatureTargeting(instance, targetMode, targetType);
    }
}

[HarmonyPatch(typeof(NControllerCardPlay), nameof(NControllerCardPlay.Start))]
internal static class NControllerCardPlayStartCustomTargetTypePatch
{
    private static readonly Action<NCardPlay> CenterCard =
        AccessTools.MethodDelegate<Action<NCardPlay>>(
            AccessTools.DeclaredMethod(typeof(NCardPlay), "CenterCard"));

    private static readonly Action<NCardPlay, CardModel> CannotPlayThisCardFtueCheck =
        AccessTools.MethodDelegate<Action<NCardPlay, CardModel>>(
            AccessTools.DeclaredMethod(typeof(NCardPlay), "CannotPlayThisCardFtueCheck", [typeof(CardModel)]));

    private static readonly Func<NControllerCardPlay, TargetType, Task> SingleCreatureTargeting =
        AccessTools.MethodDelegate<Func<NControllerCardPlay, TargetType, Task>>(
            AccessTools.DeclaredMethod(typeof(NControllerCardPlay), "SingleCreatureTargeting",
                [typeof(TargetType)]));

    public static bool Prefix(NControllerCardPlay __instance)
    {
        var card = CardPlayDelegates.GetCard(__instance);
        if (card == null || !CustomTargetType.IsCustomSingleTargetType(card.TargetType))
            return true;

        var cardNode = CardPlayDelegates.GetCardNode(__instance);
        if (cardNode == null)
            return false;

        NDebugAudioManager.Instance?.Play("card_select.mp3");
        NHoverTipSet.Remove(__instance.Holder);

        if (!card.CanPlay(out var reason, out var preventer))
        {
            CannotPlayThisCardFtueCheck(__instance, card);
            __instance.CancelPlayCard();
            var line = reason.GetPlayerDialogueLine(preventer);
            if (line != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(
                    NThoughtBubbleVfx.Create(line.GetFormattedText(), card.Owner.Creature, 1.0));
            }

            return false;
        }

        CardPlayDelegates.TryShowEvokingOrbs(__instance);
        cardNode.CardHighlight.AnimFlash();
        CenterCard(__instance);
        TaskHelper.RunSafely(SingleCreatureTargeting(__instance, card.TargetType));
        return false;
    }
}

[HarmonyPatch(typeof(NControllerCardPlay), "SingleCreatureTargeting", typeof(TargetType))]
internal static class NControllerCardPlaySingleTargetingCustomTargetTypePatch
{
    private static readonly Action<NCardPlay, NCreature> OnCreatureHover =
        AccessTools.MethodDelegate<Action<NCardPlay, NCreature>>(
            AccessTools.DeclaredMethod(typeof(NCardPlay), "OnCreatureHover", [typeof(NCreature)]));

    private static readonly Action<NCardPlay, NCreature> OnCreatureUnhover =
        AccessTools.MethodDelegate<Action<NCardPlay, NCreature>>(
            AccessTools.DeclaredMethod(typeof(NCardPlay), "OnCreatureUnhover", [typeof(NCreature)]));

    private static readonly Action<NCardPlay, Creature?> TryPlayCard =
        AccessTools.MethodDelegate<Action<NCardPlay, Creature?>>(
            AccessTools.DeclaredMethod(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)]));

    public static bool Prefix(NControllerCardPlay __instance, TargetType targetType, ref Task __result)
    {
        if (!CustomTargetType.IsCustomSingleTargetType(targetType))
            return true;

        __result = RunTargeting(__instance, targetType);
        return false;
    }

    private static async Task RunTargeting(NControllerCardPlay instance, TargetType targetType)
    {
        var card = CardPlayDelegates.GetCard(instance);
        var cardNode = CardPlayDelegates.GetCardNode(instance);
        if (card?.CombatState == null || cardNode == null)
        {
            instance.CancelPlayCard();
            return;
        }

        var room = NCombatRoom.Instance;
        if (room == null)
        {
            instance.CancelPlayCard();
            return;
        }

        var nodes = room.CreatureNodes
            .Where(n =>
                CustomTargetTypeRegistry.TryIsAllowedSingleTarget(targetType, card, n.Entity, out var allowed) && allowed)
            .ToList();

        if (nodes.Count == 0)
        {
            instance.CancelPlayCard();
            return;
        }

        var targetManager = NTargetManager.Instance;
        var hoverCallable = Callable.From((NCreature c) => OnCreatureHover(instance, c));
        var unhoverCallable = Callable.From((NCreature c) => OnCreatureUnhover(instance, c));

        try
        {
            using var _ = CustomTargetingContext.Push(card);
            targetManager.Connect(NTargetManager.SignalName.CreatureHovered, hoverCallable);
            targetManager.Connect(NTargetManager.SignalName.CreatureUnhovered, unhoverCallable);

            targetManager.StartTargeting(
                targetType,
                cardNode,
                TargetMode.Controller,
                () => !GodotObject.IsInstanceValid(instance)
                      || !NControllerManager.Instance!.IsUsingController,
                null);

            room.RestrictControllerNavigation(nodes.Select(n => n.Hitbox));
            nodes.First().Hitbox.TryGrabFocus();

            var selected = (NCreature?)await targetManager.SelectionFinished();
            if (!GodotObject.IsInstanceValid(instance))
                return;

            if (selected != null)
                TryPlayCard(instance, selected.Entity);
            else
                instance.CancelPlayCard();
        }
        finally
        {
            if (targetManager.IsConnected(NTargetManager.SignalName.CreatureHovered, hoverCallable))
            {
                targetManager.Disconnect(NTargetManager.SignalName.CreatureHovered, hoverCallable);
            }

            if (targetManager.IsConnected(NTargetManager.SignalName.CreatureUnhovered, unhoverCallable))
            {
                targetManager.Disconnect(NTargetManager.SignalName.CreatureUnhovered, unhoverCallable);
            }
        }
    }
}

[HarmonyPatch(typeof(NCardPlay), "TryPlayCard", typeof(Creature))]
internal static class NCardPlayTryPlayCardCustomTargetTypePatch
{
    private static readonly Action<NCardPlay, bool> InvokeCleanup =
        AccessTools.MethodDelegate<Action<NCardPlay, bool>>(
            AccessTools.DeclaredMethod(typeof(NCardPlay), "Cleanup", [typeof(bool)])!);

    public static bool Prefix(NCardPlay __instance, Creature? target)
    {
        var card = __instance.Card;
        if (card == null || !CustomTargetType.IsCustomSingleTargetType(card.TargetType))
            return true;

        if (target == null || __instance.Holder.CardModel == null)
        {
            __instance.CancelPlayCard();
            return false;
        }

        if (!__instance.Holder.CardModel.CanPlayTargeting(target))
        {
            __instance.CannotPlayThisCardFtueCheck(__instance.Holder.CardModel);
            __instance.CancelPlayCard();
            return false;
        }

        __instance._isTryingToPlayCard = true;
        var played = card.TryManualPlay(target);
        __instance._isTryingToPlayCard = false;

        if (played)
        {
            __instance.AutoDisableCannotPlayCardFtueCheck();
            if (__instance.Holder.IsInsideTree())
            {
                var size = __instance.GetViewport().GetVisibleRect().Size;
                __instance.Holder.SetTargetPosition(new(size.X / 2f, size.Y - __instance.Holder.Size.Y));
            }

            InvokeCleanup(__instance, true);
            CardPlayUiFocus.AfterCardPlayFinished();
        }
        else
        {
            __instance.CancelPlayCard();
        }

        return false;
    }
}
