using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Android can invoke relic flash hooks before the combat creature node is fully created.
/// Skip the target VFX in that case instead of letting NRelicFlashVfx._Ready crash.
/// </summary>
[HarmonyPatch(typeof(NRelicFlashVfx), nameof(NRelicFlashVfx.Create), typeof(RelicModel), typeof(Creature))]
static class RelicFlashCompatibilityPatch
{
    static bool Prefix(RelicModel relic, Creature target, ref NRelicFlashVfx? __result)
    {
        if (NCombatRoom.Instance?.GetCreatureNode(target) != null)
        {
            return true;
        }

        MainFile.Logger.Debug(
            $"Skipping relic flash VFX for {relic.Id} because the target creature node is not ready.");
        __result = null;
        return false;
    }
}
