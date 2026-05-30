using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigRisingFist : YuWanCardModel
{
    public PigRisingFist() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithPower<FreeAttackPower>(1);
        WithPower<GigantificationPower>(1);
        WithPower<OneTwoPunchPower>(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FreeAttackPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["FreeAttackPower"].IntValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<GigantificationPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["GigantificationPower"].IntValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<OneTwoPunchPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["OneTwoPunchPower"].IntValue,
            Owner.Creature,
            this);
    }
}
