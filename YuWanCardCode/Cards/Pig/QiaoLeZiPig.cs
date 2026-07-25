using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class QiaoLeZiPig : YuWanCardModel
{
    public QiaoLeZiPig() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Token,
        target: TargetType.Self)
    {
        WithPower<DexterityPower>(2);
        WithKeywords(CardKeyword.Exhaust);
        WithTags(YuWanTags.FoodPig);
        WithCostUpgradeBy(-1);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardUtils.RecordFoodPigPlayed(this);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, DynamicVars["DexterityPower"].BaseValue, Owner.Creature, this);
    }
}
