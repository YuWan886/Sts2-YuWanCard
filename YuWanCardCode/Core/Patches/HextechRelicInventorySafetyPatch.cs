using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using YuWanCard.Hextech;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Some custom relics are intentionally ephemeral after obtain, or can otherwise hit
/// inventory-node timing edges. Suppress those inventory-only exceptions so the reward
/// flow completes instead of crashing the run.
/// </summary>
[HarmonyPatch]
static class HextechRelicInventorySafetyPatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(NRelicInventory), "Add", [typeof(RelicModel), typeof(bool), typeof(int)])]
    static Exception? SuppressHextechInventoryAddException(Exception? __exception, RelicModel relic)
    {
        if (__exception == null || !ShouldSuppressInventoryException(relic))
        {
            return __exception;
        }

        MainFile.Logger.Warn(
            $"HextechRelicInventorySafetyPatch: suppressed inventory add failure for {relic.Id.Entry}: {__exception}");
        return null;
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(NRelicInventory), nameof(NRelicInventory.AnimateRelic))]
    static Exception? SuppressHextechInventoryAnimateException(Exception? __exception, RelicModel relic)
    {
        if (__exception == null || !ShouldSuppressInventoryException(relic))
        {
            return __exception;
        }

        MainFile.Logger.Warn(
            $"HextechRelicInventorySafetyPatch: suppressed inventory animate failure for {relic.Id.Entry}: {__exception}");
        return null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder.PlayNewlyAcquiredAnimation))]
    static bool SkipDetachedProtectedRelicAnimation(NRelicInventoryHolder __instance, ref Task __result, out bool __state)
    {
        __state = false;

        if (!TryGetProtectedRelic(__instance, out RelicModel? relic))
        {
            return true;
        }

        if (GodotObject.IsInstanceValid(__instance) && __instance.IsInsideTree())
        {
            __state = true;
            return true;
        }

        string relicId = relic?.Id.Entry ?? "unknown";
        MainFile.Logger.Warn(
            $"HextechRelicInventorySafetyPatch: skipped acquired animation before start for {relicId} because holder is detached.");
        __result = Task.CompletedTask;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder.PlayNewlyAcquiredAnimation))]
    static void WrapProtectedRelicAnimationTask(NRelicInventoryHolder __instance, bool __state, ref Task __result)
    {
        if (!__state || !TryGetProtectedRelic(__instance, out RelicModel? relic))
        {
            return;
        }

        __result = AwaitAnimationSafely(__result, __instance, relic!);
    }

    static bool ShouldSuppressInventoryException(RelicModel relic)
    {
        return HextechRuntimeCompat.TryGetSafeEnergyPrefix(relic, out _) || relic is YuWanJokerRelicModel;
    }

    static bool TryGetProtectedRelic(NRelicInventoryHolder holder, out RelicModel? relic)
    {
        relic = holder.Relic?.Model;
        return relic != null && ShouldSuppressInventoryException(relic);
    }

    static async Task AwaitAnimationSafely(Task original, NRelicInventoryHolder holder, RelicModel relic)
    {
        try
        {
            await original;
        }
        catch (NullReferenceException) when (!GodotObject.IsInstanceValid(holder) || !holder.IsInsideTree())
        {
            MainFile.Logger.Warn(
                $"HextechRelicInventorySafetyPatch: suppressed detached-holder animation NRE for {relic.Id.Entry}.");
        }
        catch (ObjectDisposedException) when (!GodotObject.IsInstanceValid(holder))
        {
            MainFile.Logger.Warn(
                $"HextechRelicInventorySafetyPatch: suppressed disposed-holder animation failure for {relic.Id.Entry}.");
        }
    }
}
