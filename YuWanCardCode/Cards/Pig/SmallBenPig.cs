using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Characters;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class SmallBenPig : YuWanCardModel
{
    public SmallBenPig() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithTip(card => HoverTipFactory.FromCard<BigBenPig>(card.IsUpgraded));
        WithTip(typeof(SmallBenPigPower));
        WithPower<DexterityPower>(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DexterityPower"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = DynamicVars["DexterityPower"].IntValue;
        await PowerCmd.Apply<SmallBenPigPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, amount, Owner.Creature, this);

        var bigBenPig = CombatState!.CreateCard(ModelDb.Card<BigBenPig>(), Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(bigBenPig);
        }
        CardPileAddResult addResult = await CardPileCmd.AddGeneratedCardToCombat(bigBenPig, PileType.Discard, Owner);
        CardCmd.PreviewCardPileAdd(addResult);

        VfxUtils.PlayStaticVfxAtCreatureTop(Owner.Creature);
    }
}
