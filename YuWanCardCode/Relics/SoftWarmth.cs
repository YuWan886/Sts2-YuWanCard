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
public sealed class SoftWarmth : YuWanRelicModel
{
    private const int TotalCombats = 5;
    private const int DexterityPerCombat = 3;

    [SavedProperty]
    public int RemainingCombats { get; set; } = TotalCombats;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => RemainingCombats;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DexterityPower>(DexterityPerCombat)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DexterityPower>()];

    public SoftWarmth() : base(true)
    {
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null || RemainingCombats <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<DexterityPower>(
            Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, null);

        RemainingCombats--;
        InvokeDisplayAmountChanged();
    }
}
