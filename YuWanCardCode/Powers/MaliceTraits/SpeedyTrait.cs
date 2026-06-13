using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class SpeedyTrait : MaliceTraitPowerBase
{
    private const int DexterityLossPerTurn = 1;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        int maxDexterityLoss = GetMaxDexterityLoss(combatState);
        bool flashed = false;

        foreach (var player in combatState.Players)
        {
            if (player.Creature.IsDead)
            {
                continue;
            }

            int currentDexterity = player.Creature.GetPower<DexterityPower>()?.Amount ?? 0;
            int remainingLoss = maxDexterityLoss + currentDexterity;
            int lossAmount = Math.Min(DexterityLossPerTurn, Math.Max(0, remainingLoss));
            if (lossAmount <= 0)
            {
                continue;
            }

            if (!flashed)
            {
                Flash();
                flashed = true;
            }

            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), player.Creature, -lossAmount, Owner, null);
        }
    }

    private static int GetMaxDexterityLoss(ICombatState combatState)
    {
        int actIndex = combatState.RunState?.CurrentActIndex ?? 0;
        return 3 + Math.Min(Math.Max(actIndex, 0), 2);
    }
}
