using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigOvertime : YuWanCardModel
{
    protected override bool ShouldGlowRedInternal => true;
    public override int MaxUpgradeLevel => 0;

    public PigOvertime() : base(
        baseCost: 1,
        type: CardType.Status,
        rarity: CardRarity.Status,
        target: TargetType.None)
    {
        WithVar("SelfDamage", 6);
        WithGold(5);
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (Pile?.Type != PileType.Hand || card.Owner != Owner)
        {
            return true;
        }

        if (ReferenceEquals(card, this) || card is PigOvertime)
        {
            return true;
        }

        if (autoPlayType != AutoPlayType.None)
        {
            return true;
        }

        return false;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            DynamicVars["SelfDamage"].BaseValue,
            ValueProp.Unpowered,
            Owner.Creature);
        await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner);
    }
}
