using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch]
public static class WhatIfInfiniteUpgradesPatch
{
    private static int _loadingDepth;

    private static bool IsLoadOverrideActive => _loadingDepth > 0;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.MaxUpgradeLevel), MethodType.Getter)]
    public static void CardModel_MaxUpgradeLevel_Postfix(CardModel __instance, ref int __result)
    {
        if (IsLoadOverrideActive || __instance.Owner?.GetRelic<WhatIfInfiniteUpgrades>() != null)
        {
            __result = int.MaxValue;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), "LoadInventory")]
    public static void Player_LoadInventory_Prefix(SerializablePlayer save)
    {
        if (save.Relics.Any(relic => relic.Id == ModelDb.GetId<WhatIfInfiniteUpgrades>()))
        {
            _loadingDepth++;
        }
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(Player), "LoadInventory")]
    public static Exception? Player_LoadInventory_Finalizer(Exception? __exception)
    {
        if (_loadingDepth > 0)
        {
            _loadingDepth--;
        }

        return __exception;
    }
}
