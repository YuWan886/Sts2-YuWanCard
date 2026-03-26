using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(VulnerablePower))]
class VulnerablePowerArrogantPigPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("ModifyDamageMultiplicative")]
    static decimal ModifyVulnerableMultiplier(decimal __result, Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer == null || dealer.Player == null)
        {
            return __result;
        }

        var arrogantPig = dealer.Player.GetRelic<ArrogantPig>();
        if (arrogantPig != null && target != null)
        {
            __result = arrogantPig.ModifyVulnerableMultiplier(target, __result, props, dealer, cardSource);
        }

        return __result;
    }
}

[HarmonyPatch(typeof(WeakPower))]
class WeakPowerArrogantPigPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("ModifyDamageMultiplicative")]
    static decimal ModifyWeakMultiplier(decimal __result, Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer == null || dealer.Player == null)
        {
            return __result;
        }

        var arrogantPig = dealer.Player.GetRelic<ArrogantPig>();
        if (arrogantPig != null && target != null)
        {
            __result = arrogantPig.ModifyWeakMultiplier(target, __result, props, dealer, cardSource);
        }

        return __result;
    }
}
