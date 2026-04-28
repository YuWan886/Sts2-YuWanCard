using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Powers;

namespace YuWanCard.Core.Abstracts;

/// <summary>
/// Simplified temporary power. Applies an internal power when first applied
/// and removes it at turn end. 
/// </summary>
public abstract class YuWanTemporaryPowerModel : YuWanPowerModel
{
    public abstract PowerModel InternallyAppliedPower { get; }
    public abstract AbstractModel OriginModel { get; }
    protected virtual bool UntilEndOfOtherSideTurn => false;
    protected virtual int LastForXExtraTurns => 0;

    public override PowerType Type => InternallyAppliedPower.Type;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Repeat", 0), new DynamicVar("UntilEndOfOtherSideTurn", 0)];

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await PowerCmd.Apply(InternallyAppliedPower, target, amount, applier, cardSource, true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if ((!UntilEndOfOtherSideTurn && side != Owner.Side) ||
            (UntilEndOfOtherSideTurn && side == Owner.Side))
            return;

        if (DynamicVars["Repeat"].BaseValue > 0)
        {
            DynamicVars["Repeat"].UpgradeValueBy(-1);
            return;
        }

        Flash();
        await PowerCmd.Apply(InternallyAppliedPower, Owner, -Amount, Owner, null, true);
        await PowerCmd.Remove(this);
    }
}

/// <summary>
/// Typed convenience wrapper for temporary powers. 
/// </summary>
public abstract class YuWanTemporaryPowerModelWrapper<TOrigin, TPower> : YuWanTemporaryPowerModel
    where TOrigin : AbstractModel
    where TPower : PowerModel
{
    public override AbstractModel OriginModel => ModelDb.GetById<AbstractModel>(ModelDb.GetId<TOrigin>());
    public override PowerModel InternallyAppliedPower => ModelDb.Power<TPower>();

    public override string? CustomPackedIconPath =>
        Amount >= 0 ? "res://YuWanCard/images/powers/pig_temp_up.png" : "res://YuWanCard/images/powers/pig_temp_down.png";
    public override string? CustomBigIconPath =>
        Amount >= 0 ? "res://YuWanCard/images/powers/pig_temp_up_big.png" : "res://YuWanCard/images/powers/pig_temp_down_big.png";

    public override LocString Title => OriginModel switch
    {
        CardModel cardModel => cardModel.TitleLocString,
        _ => new LocString("powers", Id.Entry + ".title")
    };

    public override LocString Description => new("powers",
        Amount > 0 ? "YUWANCARD-TEMP_POWER.UP.description" : "YUWANCARD-TEMP_POWER.DOWN.description");
}
