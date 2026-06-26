using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Potions;

[Pool(typeof(PigPotionPool))]
public sealed class SweetDreamPotion : YuWanPotionModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Unpowered),
        new PowerVar<PlatingPower>(2m)
    ];

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<PlatingPower>()
    ];

    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await CreatureCmd.GainBlock(target, DynamicVars.Block, null);
        await PowerCmd.Apply<PlatingPower>(choiceContext, target, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, null);
    }
}
