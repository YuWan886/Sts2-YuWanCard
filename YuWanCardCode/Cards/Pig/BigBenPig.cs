using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class BigBenPig : YuWanCardModel
{
    public BigBenPig() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithTip(typeof(SmallBenPig));
        WithTip(typeof(BigBenPigPower));
        WithPower<StrengthPower>(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = DynamicVars["StrengthPower"].IntValue;
        await PowerCmd.Apply<BigBenPigPower>(Owner.Creature, amount, Owner.Creature, this);

        var benPig = CombatState!.CreateCard(ModelDb.Card<SmallBenPig>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(benPig, PileType.Draw, addedByPlayer: true);
    }
}
