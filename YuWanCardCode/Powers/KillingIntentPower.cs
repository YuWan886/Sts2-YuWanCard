using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers;

public class KillingIntentPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("KillingIntentMultiplier", 2m)];

    public decimal Multiplier => DynamicVars["KillingIntentMultiplier"].BaseValue;

    private class Data
    {
        public AttackCommand? CommandToModify;
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        return base.BeforeApplied(target, amount, applier, cardSource);
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        return base.AfterApplied(applier, cardSource);
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.ModelSource is not CardModel cardModel)
            return Task.CompletedTask;
        if (cardModel.Owner.Creature != Owner)
            return Task.CompletedTask;
        if (cardModel.Type != CardType.Attack)
            return Task.CompletedTask;
        if (!command.DamageProps.IsPoweredAttack())
            return Task.CompletedTask;
        if (Amount <= 0)
            return Task.CompletedTask;

        var internalData = GetInternalData<Data>();
        if (internalData.CommandToModify != null)
            return Task.CompletedTask;

        internalData.CommandToModify = command;
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 1m;
        if (cardSource == null) return 1m;
        if (cardSource.Type != CardType.Attack) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        if (Amount <= 0) return 1m;

        var internalData = GetInternalData<Data>();
        if (internalData.CommandToModify == null || cardSource == internalData.CommandToModify.ModelSource)
        {
            Flash();
            return Multiplier;
        }

        return 1m;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        var internalData = GetInternalData<Data>();
        if (command == internalData.CommandToModify)
        {
            internalData.CommandToModify = null;
            SetAmount(Amount - 1);
            if (Amount <= 0)
            {
                await PowerCmd.Remove(this);
            }
        }
    }
}
