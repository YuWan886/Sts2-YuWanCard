using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigPudding : YuWanCardModel
{
    public PigPudding() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Token,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithTags(YuWanTags.FoodPig);
        WithCostUpgradeBy(-1);
    }



    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardUtils.RecordFoodPigPlayed(this);
        var debuffs = Owner.Creature.Powers
            .Where(p => p.Type == PowerType.Debuff)
            .ToList();

        if (debuffs.Count == 0) return;

        var randomDebuff = Owner.RunState.Rng.CombatCardGeneration.NextItem(debuffs);
        await PowerCmd.Remove(randomDebuff);

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
    }
}
