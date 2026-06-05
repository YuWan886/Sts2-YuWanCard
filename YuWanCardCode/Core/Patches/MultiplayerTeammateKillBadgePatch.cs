using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YuWanCard.Badges;
using YuWanCard.Core.Badges;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.DamageReceived))]
public static class MultiplayerTeammateKillBadgePatch
{
    [HarmonyPostfix]
    public static void DamageReceivedPostfix(CombatState combatState, Creature receiver, Creature? dealer, DamageResult result)
    {
        if (!TryGetKillerPlayerId(combatState, receiver, dealer, result, out ulong killerPlayerId))
        {
            return;
        }

        if (BadgeProgressTracker.GetProgress(killerPlayerId, WerewolfBadge.BadgeId) > 0)
        {
            return;
        }

        BadgeProgressTracker.AddProgress(killerPlayerId, WerewolfBadge.BadgeId, 1);
    }

    private static bool TryGetKillerPlayerId(
        CombatState combatState,
        Creature receiver,
        Creature? dealer,
        DamageResult result,
        out ulong killerPlayerId)
    {
        killerPlayerId = 0;

        if (combatState.RunState.Players.Count <= 1)
        {
            return false;
        }

        if (!result.WasTargetKilled || receiver.Player == null)
        {
            return false;
        }

        Player? killerPlayer = dealer?.Player ?? dealer?.PetOwner;
        if (killerPlayer?.Creature == null)
        {
            return false;
        }

        if (killerPlayer.NetId == receiver.Player.NetId)
        {
            return false;
        }

        if (killerPlayer.Creature.Side != receiver.Side)
        {
            return false;
        }

        killerPlayerId = killerPlayer.NetId;
        return true;
    }
}
