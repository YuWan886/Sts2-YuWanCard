using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Powers;

public class PigBrainOverloadPower : YuWanPowerModel
{
    [SavedProperty]
    public int YUWANCARD_TurnCounter { get; set; } = 0;

    [SavedProperty]
    public int YUWANCARD_UpgradedCount { get; set; } = 0;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public int DazedInterval => YUWANCARD_UpgradedCount > 0 ? 3 : 2;

    public int DazedCount => Amount;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DazedIntervalVar(this)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Dazed>()];

    private class DazedIntervalVar(PigBrainOverloadPower? power = null) : DynamicVar("DazedInterval", power?.DazedInterval ?? 2m)
    {
        private PigBrainOverloadPower? _power = power;

        public override void SetOwner(AbstractModel owner)
        {
            base.SetOwner(owner);
            _power = owner as PigBrainOverloadPower;
            if (_power != null)
            {
                BaseValue = _power.DazedInterval;
            }
        }
    }

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (cardSource is { IsUpgraded: true })
        {
            YUWANCARD_UpgradedCount++;
        }
        await base.BeforeApplied(target, amount, applier, cardSource);
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        YUWANCARD_TurnCounter = 0;
        DynamicVars["DazedInterval"].BaseValue = DazedInterval;
        await base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            YUWANCARD_TurnCounter++;
            
            if (YUWANCARD_TurnCounter >= DazedInterval)
            {
                Flash();
                YUWANCARD_TurnCounter = 0;
                for (int i = 0; i < DazedCount; i++)
                {
                    CardModel dazed = combatState.CreateCard<Dazed>(Owner.Player!);
                    CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(dazed, PileType.Hand, Owner.Player!));
                }
            }
        }
    }
}
