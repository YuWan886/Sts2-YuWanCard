using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public sealed class IgnisCounterPower : YuWanPowerModel
{
    private sealed class Data
    {
        public Creature? Dealer { get; set; }
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
    

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || amount <= 0)
        {
            return amount;
        }

        if (dealer == null || dealer.Side == Owner.Side || dealer.IsDead)
        {
            return amount;
        }

        var data = GetInternalData<Data>();
        data.Dealer ??= dealer;
        return amount;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        var data = GetInternalData<Data>();
        if (data.Dealer == null || data.Dealer.IsDead || Owner.IsDead)
        {
            return;
        }

        Creature dealer = data.Dealer;
        data.Dealer = null;

        Flash();
        await DamageCmd.Attack(Amount)
            .Targeting(dealer)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(new ThrowingPlayerChoiceContext());
        if (dealer.IsAlive)
        {
            await CardPileCmd.AddToCombatAndPreview<Burn>(new[] { dealer }, PileType.Discard, 1, addedByPlayer: false);
        }
        await PowerCmd.Remove(this);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player)
        {
            return;
        }

        await PowerCmd.Remove(this);
    }
}
