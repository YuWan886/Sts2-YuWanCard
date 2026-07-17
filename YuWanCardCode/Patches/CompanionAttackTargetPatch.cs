using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using YuWanCard.Monsters;

namespace YuWanCard.Patches;

/// <summary>
/// Monster attacks normally pull only real Player creatures from CombatState.
/// Call Companions is a player-side combat creature, so add it to the shared
/// monster-attack candidate list without manufacturing an engine-managed Player.
/// </summary>
[HarmonyPatch]
public static class CompanionAttackTargetPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(AttackCommand), "GetPossibleTargets")!;

    [HarmonyPostfix]
    private static void AddLivingCompanions(AttackCommand __instance, ref IReadOnlyList<Creature> __result)
    {
        var attacker = __instance.Attacker;
        var combatState = attacker?.CombatState;
        if (attacker is not { IsEnemy: true }
            || __instance.IsSingleTargeted
            || __instance.TargetSide != CombatSide.Player
            || combatState == null)
        {
            return;
        }

        var companions = combatState.Allies
            .Where(creature => creature is { IsAlive: true, Monster: CompanionPlaceholderModel })
            .ToList();
        if (companions.Count == 0)
        {
            return;
        }

        var originalTargets = __result;
        __result = originalTargets
            .Concat(companions.Where(companion => !originalTargets.Contains(companion)))
            .ToList();
    }
}
