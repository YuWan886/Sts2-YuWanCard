using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Potions;

[Pool(typeof(PigPotionPool))]
public sealed class OinkChargePotion : YuWanPotionModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(4m),
        new PowerVar<DexterityPower>(3m)
    ];

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<StrengthPower>(choiceContext, target, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(choiceContext, target, DynamicVars["DexterityPower"].BaseValue, Owner.Creature, null);
    }
}
