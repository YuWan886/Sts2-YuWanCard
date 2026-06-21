using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class HongMengPig : YuWanRelicModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m),
        new PowerVar<RitualPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<RitualPower>()
    ];

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public HongMengPig() : base(true)
    {
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["DexterityPower"].BaseValue, Owner.Creature, null);
        await PowerCmd.Apply<RitualPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["RitualPower"].BaseValue, Owner.Creature, null);
    }
}
