using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Powers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Apply), [typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool)])]
public static class EmperorsNewPigDebuffPatch
{
    [HarmonyPrefix]
    static bool Prefix(ref Task __result, PowerModel power, Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Type != PowerType.Debuff) return true;
        if (amount <= 0) return true;

        var emperorsNewPigPower = target.GetPower<EmperorsNewPigPower>();
        if (emperorsNewPigPower == null) return true;
        if (!emperorsNewPigPower.YUWANCARD_PreventDebuffs) return true;
        if (emperorsNewPigPower.Amount <= 0) return true;

        if (applier == null) return true;
        if (applier.Side == target.Side) return true;

        emperorsNewPigPower.Flash();
        __result = Task.FromResult(power);
        return false;
    }
}
