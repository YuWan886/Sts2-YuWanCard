using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using YuWanCard.Core.RightClick;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(NPlayerHand), "AddCardHolder", typeof(NHandCardHolder), typeof(int))]
public static class RightClickCardHolderPatch
{
    private const string HolderMetaKey = "yuwan_right_click_card_holder_bound";
    private const string HitboxMetaKey = "yuwan_right_click_card_hitbox_bound";

    [HarmonyPostfix]
    private static void Postfix(NHandCardHolder holder)
    {
        ConnectGuiInputOnce(holder, HolderMetaKey, inputEvent => OnHolderGuiInput(holder, inputEvent));
        ConnectGuiInputOnce(holder.Hitbox, HitboxMetaKey, inputEvent => OnHitboxGuiInput(holder, inputEvent));
    }

    private static void OnHolderGuiInput(NCardHolder holder, InputEvent inputEvent)
    {
        bool triggeredByController =
            inputEvent is InputEventAction { Action: var action } actionEvent
            && action == MegaInput.cancel
            && actionEvent.IsPressed()
            && holder.HasFocus();

        if (triggeredByController)
        {
            TryHandle(holder, new YuWanRightClickTrigger(true));
        }
    }

    private static void OnHitboxGuiInput(NCardHolder holder, InputEvent inputEvent)
    {
        bool triggeredByMouse =
            inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightClick
            && rightClick.IsPressed();

        if (triggeredByMouse)
        {
            TryHandle(holder, new YuWanRightClickTrigger());
        }
    }

    private static void TryHandle(NCardHolder holder, YuWanRightClickTrigger trigger)
    {
        Viewport viewport = holder.GetViewport();
        if (viewport.IsInputHandled())
        {
            return;
        }

        NPlayerHand? hand = NPlayerHand.Instance;
        if (hand == null || hand.InCardPlay || NTargetManager.Instance?.IsInSelection == true)
        {
            return;
        }

        var card = holder.CardModel;
        if (card == null)
        {
            return;
        }

        var player = LocalContext.GetMe(card.CombatState);
        if (player == null)
        {
            return;
        }

        if (YuWanRightClickRegistry.TryDispatch(new YuWanRightClickContext(player, card, trigger)))
        {
            viewport.SetInputAsHandled();
        }
    }

    private static void ConnectGuiInputOnce(Control node, string metaKey, Action<InputEvent> handler)
    {
        if (node.HasMeta(metaKey))
        {
            return;
        }

        node.SetMeta(metaKey, true);
        node.Connect(Control.SignalName.GuiInput, Callable.From(handler));
    }
}

[HarmonyPatch(typeof(NRelic), nameof(NRelic._Ready))]
public static class RightClickRelicPatch
{
    private const string MetaKey = "yuwan_right_click_relic_bound";

    [HarmonyPostfix]
    private static void Postfix(NRelic __instance)
    {
        ConnectGuiInputOnce(__instance, MetaKey, inputEvent => OnGuiInput(__instance, inputEvent));
    }

    private static void OnGuiInput(NRelic relicNode, InputEvent inputEvent)
    {
        Viewport viewport = relicNode.GetViewport();
        if (viewport.IsInputHandled())
        {
            return;
        }

        if (!TryGetTrigger(relicNode, inputEvent, out YuWanRightClickTrigger trigger)
            || NTargetManager.Instance?.IsInSelection == true)
        {
            return;
        }

        var player = LocalContext.GetMe(relicNode.Model.Owner.RunState);
        if (player == null)
        {
            return;
        }

        if (YuWanRightClickRegistry.TryDispatch(new YuWanRightClickContext(player, relicNode.Model, trigger)))
        {
            viewport.SetInputAsHandled();
        }
    }

    private static void ConnectGuiInputOnce(Control node, string metaKey, Action<InputEvent> handler)
    {
        if (node.HasMeta(metaKey))
        {
            return;
        }

        node.SetMeta(metaKey, true);
        node.Connect(Control.SignalName.GuiInput, Callable.From(handler));
    }

    private static bool TryGetTrigger(Control node, InputEvent inputEvent, out YuWanRightClickTrigger trigger)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Right } mouseButton when mouseButton.IsReleased():
                trigger = new YuWanRightClickTrigger();
                return true;
            case InputEventAction { Action: var action } actionEvent
                when action == MegaInput.cancel && actionEvent.IsPressed() && node.HasFocus():
                trigger = new YuWanRightClickTrigger(true);
                return true;
            default:
                trigger = default;
                return false;
        }
    }
}

[HarmonyPatch(typeof(NPower), nameof(NPower._Ready))]
public static class RightClickPowerPatch
{
    private const string MetaKey = "yuwan_right_click_power_bound";

    [HarmonyPostfix]
    private static void Postfix(NPower __instance)
    {
        ConnectGuiInputOnce(__instance, MetaKey, inputEvent => OnGuiInput(__instance, inputEvent));
    }

    private static void OnGuiInput(NPower powerNode, InputEvent inputEvent)
    {
        Viewport viewport = powerNode.GetViewport();
        if (viewport.IsInputHandled())
        {
            return;
        }

        if (!TryGetTrigger(powerNode, inputEvent, out YuWanRightClickTrigger trigger)
            || NTargetManager.Instance?.IsInSelection == true)
        {
            return;
        }

        var player = LocalContext.GetMe(powerNode.Model.Owner.CombatState);
        if (player == null)
        {
            return;
        }

        if (YuWanRightClickRegistry.TryDispatch(new YuWanRightClickContext(player, powerNode.Model, trigger)))
        {
            viewport.SetInputAsHandled();
        }
    }

    private static void ConnectGuiInputOnce(Control node, string metaKey, Action<InputEvent> handler)
    {
        if (node.HasMeta(metaKey))
        {
            return;
        }

        node.SetMeta(metaKey, true);
        node.Connect(Control.SignalName.GuiInput, Callable.From(handler));
    }

    private static bool TryGetTrigger(Control node, InputEvent inputEvent, out YuWanRightClickTrigger trigger)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Right } mouseButton when mouseButton.IsReleased():
                trigger = new YuWanRightClickTrigger();
                return true;
            case InputEventAction { Action: var action } actionEvent
                when action == MegaInput.cancel && actionEvent.IsPressed() && node.HasFocus():
                trigger = new YuWanRightClickTrigger(true);
                return true;
            default:
                trigger = default;
                return false;
        }
    }
}

[HarmonyPatch(typeof(NPotion), nameof(NPotion._Ready))]
public static class RightClickPotionPatch
{
    private const string MetaKey = "yuwan_right_click_potion_bound";

    [HarmonyPostfix]
    private static void Postfix(NPotion __instance)
    {
        ConnectGuiInputOnce(__instance, MetaKey, inputEvent => OnGuiInput(__instance, inputEvent));
    }

    private static void OnGuiInput(NPotion potionNode, InputEvent inputEvent)
    {
        Viewport viewport = potionNode.GetViewport();
        if (viewport.IsInputHandled())
        {
            return;
        }

        if (!TryGetTrigger(potionNode, inputEvent, out YuWanRightClickTrigger trigger)
            || NTargetManager.Instance?.IsInSelection == true)
        {
            return;
        }

        var player = LocalContext.GetMe(potionNode.Model.Owner.RunState);
        if (player == null)
        {
            return;
        }

        if (YuWanRightClickRegistry.TryDispatch(new YuWanRightClickContext(player, potionNode.Model, trigger)))
        {
            viewport.SetInputAsHandled();
        }
    }

    private static void ConnectGuiInputOnce(Control node, string metaKey, Action<InputEvent> handler)
    {
        if (node.HasMeta(metaKey))
        {
            return;
        }

        node.SetMeta(metaKey, true);
        node.Connect(Control.SignalName.GuiInput, Callable.From(handler));
    }

    private static bool TryGetTrigger(Control node, InputEvent inputEvent, out YuWanRightClickTrigger trigger)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Right } mouseButton when mouseButton.IsReleased():
                trigger = new YuWanRightClickTrigger();
                return true;
            case InputEventAction { Action: var action } actionEvent
                when action == MegaInput.cancel && actionEvent.IsPressed() && node.HasFocus():
                trigger = new YuWanRightClickTrigger(true);
                return true;
            default:
                trigger = default;
                return false;
        }
    }
}
