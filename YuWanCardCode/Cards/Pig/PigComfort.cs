using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigComfort : YuWanCardModel
{
    public PigComfort() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithCards(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        var allies = CombatState.Creatures
            .Where(c => c.IsAlive && c.Side == Owner.Creature.Side)
            .ToList();

        foreach (var ally in allies)
        {
            var debuff = DeterministicRandomUtils.PickStableRandom(
                ally.Powers.Where(p => p.Type == PowerType.Debuff),
                Owner.RunState.Rng.CombatCardGeneration);
            if (debuff != null)
            {
                await PowerCmd.Remove(debuff);
            }
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}
