using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Potions;

[Pool(typeof(PigPotionPool))]
public sealed class CarrotFeastPotion : YuWanPotionModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(8m)];

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override bool CanBeGeneratedInCombat => false;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await CreatureCmd.Heal(target, DynamicVars.Heal.BaseValue);
    }
}
