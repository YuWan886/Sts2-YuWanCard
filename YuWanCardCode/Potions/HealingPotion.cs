using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.PotionPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Potions;

[Pool(typeof(SharedPotionPool))]
public class HealingPotion : YuWanPotionModel
{
    private const decimal Duration = 3m;

    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HealingPotionPower>()];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<HealingPotionPower>(new ThrowingPlayerChoiceContext(), target, Duration, Owner.Creature, null);
    }
}
