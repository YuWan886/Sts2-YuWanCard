using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Extensions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpgradeInternal))]
static class UpgradeInternalPatch
{
    [HarmonyPostfix]
    static void UpgradeVars(CardModel __instance)
    {
        foreach (var varEntry in __instance.DynamicVars)
        {
            var upgradeValue = DynamicVarExtensions.DynamicVarUpgrades[varEntry.Value];
            if (upgradeValue != null)
            {
                varEntry.Value.UpgradeValueBy((decimal)upgradeValue);
            }
        }
        if (__instance is YuWanCardModel yuWanCard)
        {
            yuWanCard.ConstructedUpgrade();
        }
    }
}
