using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class XueBiPig : YuWanCardModel
{
    public XueBiPig() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Token,
        target: TargetType.Self)
    {
        WithPower<DanseMacabrePower>(2);
        WithPower<BlurPower>(1);
        WithKeywords(CardKeyword.Exhaust);
        WithTags(YuWanTags.FoodPig);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardUtils.RecordFoodPigPlayed(this);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<DanseMacabrePower>(Owner.Creature, DynamicVars["DanseMacabrePower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<BlurPower>(Owner.Creature, DynamicVars["BlurPower"].BaseValue, Owner.Creature, this);
    }
}
