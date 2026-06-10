using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigOffWork : YuWanCardModel
{
    public override int MaxUpgradeLevel => 0;
    
    public PigOffWork() : base(
        baseCost: 4,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.None)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var primaryEnemies = CombatState!.Enemies
            .Where(e => e.IsAlive && e.IsPrimaryEnemy)
            .ToList();

        foreach (var enemy in primaryEnemies)
        {
            await CreatureCmd.Kill(enemy, true);
        }

        if (DeckVersion != null)
        {
            await CardPileCmd.RemoveFromDeck(DeckVersion);
        }
    }
}
