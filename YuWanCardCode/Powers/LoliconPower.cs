using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class LoliconPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    private const float HeightThreshold = 140f;

    private class Data
    {
        public List<(Creature Target, decimal Damage)> ReflectTargets = [];
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 1m;
        if (target == null) return 1m;
        if (target == Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;

        var internalData = GetInternalData<Data>();

        if (CreatureHeightUtils.IsTallCreature(target, HeightThreshold))
        {
            return 2m;
        }

        internalData.ReflectTargets.Add((target, amount));

        return 1m;
    }

    public override async Task AfterAttack(AttackCommand command)
    {
        var internalData = GetInternalData<Data>();
        if (internalData.ReflectTargets.Count == 0) return;
        if (command.ModelSource is not CardModel cardModel) return;
        if (cardModel.Owner.Creature != Owner) return;

        Flash();

        foreach (var (target, damage) in internalData.ReflectTargets)
        {
            await DamageCmd.Attack(damage)
                .FromCard(cardModel)
                .Targeting(Owner)
                .WithHitFx("vfx/vfx_attack_lightning", null, "lightning_orb_evoke.mp3")
                .Execute(new ThrowingPlayerChoiceContext());
        }

        internalData.ReflectTargets.Clear();
    }
}
