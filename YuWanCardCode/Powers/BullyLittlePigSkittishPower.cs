using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public sealed class BullyLittlePigSkittishPower : YuWanPowerModel
{
    private sealed class Data
    {
        public bool HasGainedBlockThisTurn;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => true;

    public override string? CustomBigIconPath => "res://images/powers/skittish_power.png";
    public override string? CustomPackedIconPath => "res://images/powers/skittish_power.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    private bool HasGainedBlockThisTurn
    {
        get => GetInternalData<Data>().HasGainedBlockThisTurn;
        set
        {
            AssertMutable();
            GetInternalData<Data>().HasGainedBlockThisTurn = value;
        }
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || HasGainedBlockThisTurn || result.UnblockedDamage <= 0)
        {
            return;
        }

        if (!props.HasFlag(ValueProp.Move) || dealer == null || dealer.Side == Owner.Side)
        {
            return;
        }

        HasGainedBlockThisTurn = true;
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/phantasmal_gardeners/phantasmal_gardeners_retract");
        await CreatureCmd.TriggerAnim(Owner, "BlockStart", 0.3f);
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
        {
            return;
        }

        if (HasGainedBlockThisTurn)
        {
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/phantasmal_gardeners/phantasmal_gardeners_extend");
            await CreatureCmd.TriggerAnim(Owner, "BlockEnd", 0.15f);
        }

        HasGainedBlockThisTurn = false;
    }
}
