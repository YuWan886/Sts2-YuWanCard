using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;

namespace YuWanCard.Patches;

file static class PlayerPetTurnParticipantsHelper
{
    public static IEnumerable<Creature> Expand(CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player)
        {
            return participants;
        }

        List<Creature> expanded = [];
        HashSet<Creature> seen = new(ReferenceEqualityComparer.Instance);

        foreach (Creature participant in participants)
        {
            if (!seen.Add(participant))
            {
                continue;
            }

            expanded.Add(participant);

            foreach (Creature pet in participant.Pets)
            {
                if (!pet.IsAlive || !seen.Add(pet))
                {
                    continue;
                }

                expanded.Add(pet);
            }
        }

        return expanded;
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeTurnEnd), typeof(ICombatState), typeof(CombatSide), typeof(IEnumerable<Creature>))]
public static class PlayerPetBeforeTurnEndParticipantsPatch
{
    [HarmonyPrefix]
    static void Prefix(CombatSide side, ref IEnumerable<Creature> participants)
    {
        participants = PlayerPetTurnParticipantsHelper.Expand(side, participants);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterTurnEnd), typeof(ICombatState), typeof(CombatSide), typeof(IEnumerable<Creature>))]
public static class PlayerPetAfterTurnEndParticipantsPatch
{
    [HarmonyPrefix]
    static void Prefix(CombatSide side, ref IEnumerable<Creature> participants)
    {
        participants = PlayerPetTurnParticipantsHelper.Expand(side, participants);
    }
}
