using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(RunState))]
public static class MultiplayerModelIdentityRunStatePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(RunState.CreateForNewRun))]
    public static void BeforeCreateForNewRun()
    {
        MultiplayerModelIdentityRegistry.Clear();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(RunState.FromSerializable))]
    public static void BeforeFromSerializable()
    {
        MultiplayerModelIdentityRegistry.Clear();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(RunState.CreateForTest))]
    public static void BeforeCreateForTest()
    {
        MultiplayerModelIdentityRegistry.Clear();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(RunState.AddCard), new[] { typeof(CardModel), typeof(Player) })]
    private static void AfterRunStateAddOwnedCard(CardModel card)
    {
        MultiplayerModelIdentityRegistry.RegisterCardTree(card);
    }

    [HarmonyPostfix]
    [HarmonyPatch("AddCard", new[] { typeof(CardModel) })]
    private static void AfterRunStateAddCard(CardModel card)
    {
        MultiplayerModelIdentityRegistry.RegisterCardTree(card);
    }
}

[HarmonyPatch(typeof(CombatState))]
public static class MultiplayerModelIdentityCombatStatePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CombatState.AddCard), new[] { typeof(CardModel), typeof(Player) })]
    private static void AfterCombatStateAddOwnedCard(CardModel card)
    {
        MultiplayerModelIdentityRegistry.RegisterCardTree(card);
    }

    [HarmonyPostfix]
    [HarmonyPatch("AddCard", new[] { typeof(CardModel) })]
    private static void AfterCombatStateAddCard(CardModel card)
    {
        MultiplayerModelIdentityRegistry.RegisterCardTree(card);
    }
}

[HarmonyPatch(typeof(Player))]
public static class MultiplayerModelIdentityPlayerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("set_RunState", new[] { typeof(IRunState) })]
    private static void AfterSetRunState(Player __instance, IRunState value)
    {
        if (value is not NullRunState)
        {
            MultiplayerModelIdentityRegistry.RegisterPlayerInventory(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Player.AddRelicInternal), new[] { typeof(RelicModel), typeof(int), typeof(bool) })]
    private static void AfterAddRelicInternal(Player __instance, RelicModel relic)
    {
        if (__instance.RunState is not NullRunState)
        {
            MultiplayerModelIdentityRegistry.EnsureRegistered(relic);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Player.RemoveRelicInternal), new[] { typeof(RelicModel), typeof(bool) })]
    private static void BeforeRemoveRelicInternal(RelicModel relic)
    {
        MultiplayerModelIdentityRegistry.Unregister(relic);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Player.SyncWithSerializedPlayer), new[] { typeof(SerializablePlayer) })]
    private static void BeforeSyncWithSerializedPlayer(
        Player __instance,
        out MultiplayerModelIdentityRegistry.PlayerInventoryIdentitySnapshot __state)
    {
        __state = MultiplayerModelIdentityRegistry.CapturePlayerInventory(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Player.SyncWithSerializedPlayer), new[] { typeof(SerializablePlayer) })]
    private static void AfterSyncWithSerializedPlayer(
        Player __instance,
        MultiplayerModelIdentityRegistry.PlayerInventoryIdentitySnapshot __state)
    {
        MultiplayerModelIdentityRegistry.RestorePlayerInventory(__instance, __state);
    }
}
