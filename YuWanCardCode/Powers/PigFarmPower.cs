using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Core.Abstracts;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigFarmPower : YuWanPowerModel
{
    private sealed class Data
    {
        public bool DrewFromPigCardThisTurn;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            GetInternalData<Data>().DrewFromPigCardThisTurn = false;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        var data = GetInternalData<Data>();
        if (data.DrewFromPigCardThisTurn)
        {
            return;
        }

        if (cardPlay.Target is not { IsDead: false } target)
        {
            return;
        }

        if (target.Monster is not PigMinion || target.PetOwner?.Creature != Owner)
        {
            return;
        }

        data.DrewFromPigCardThisTurn = true;
        Flash();
        await CardPileCmd.Draw(context, 1, Owner.Player!);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
        {
            return;
        }

        var pig = PetManager.FindPetByType<PigMinion>(Owner);
        if (pig is not { IsAlive: true })
        {
            return;
        }

        Flash();
        await CreatureCmd.Heal(pig, Amount);
    }
}
