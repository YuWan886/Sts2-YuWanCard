using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch]
static class ArrogantPigPowerPatch
{
    private static decimal ModifyMultiplier(decimal result, Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource,
        Func<ArrogantPig, decimal> getModified)
    {
        if (dealer?.Player == null || target == null)
            return result;

        var arrogantPig = dealer.Player.GetRelic<ArrogantPig>();
        return arrogantPig != null ? getModified(arrogantPig) : result;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(VulnerablePower), "ModifyDamageMultiplicative")]
    static decimal OnVulnerableMultiplier(decimal __result, Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
        => ModifyMultiplier(__result, target, props, dealer, cardSource,
            a => a.ModifyVulnerableMultiplier(target!, __result, props, dealer!, cardSource!));

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WeakPower), "ModifyDamageMultiplicative")]
    static decimal OnWeakMultiplier(decimal __result, Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
        => ModifyMultiplier(__result, target, props, dealer, cardSource,
            a => a.ModifyWeakMultiplier(target!, __result, props, dealer!, cardSource!));
}
