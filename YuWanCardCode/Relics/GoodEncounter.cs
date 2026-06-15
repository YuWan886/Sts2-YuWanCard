using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class GoodEncounter : YuWanRelicModel
{
    private const int TotalCombats = 5;
    private const int StrengthPerCombat = 3;

    [SavedProperty]
    public int RemainingCombats { get; set; } = TotalCombats;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => RemainingCombats;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(StrengthPerCombat)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public GoodEncounter() : base(true)
    {
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null || RemainingCombats <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);

        RemainingCombats--;
        InvokeDisplayAmountChanged();
    }
}
