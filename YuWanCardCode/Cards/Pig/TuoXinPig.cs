using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public sealed class TuoXinPig : YuWanCardModel
{
    public TuoXinPig() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithGold(10, 3);
        WithTip(typeof(TuoXinPigNextTurnGoldPower));
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<TuoXinPigNextTurnGoldPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars.Gold.IntValue,
            Owner.Creature,
            this);
    }
}
