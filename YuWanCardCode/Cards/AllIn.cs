using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class AllIn : YuWanCardModel
{
    public AllIn() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithVar("Magic", 4);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Magic"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var discardPile = PileType.Discard.GetPile(Owner);
        if (discardPile == null)
            return;

        var allCards = discardPile.Cards.ToList();
        if (allCards.Count == 0)
            return;

        int count = DynamicVars["Magic"].IntValue;
        var cardsToPlay = allCards.OrderBy(_ => Owner.RunState.Rng.Niche.NextFloat()).Take(count).ToList();

        foreach (var card in cardsToPlay)
        {
            CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
        }

        foreach (var card in cardsToPlay)
        {
            Creature? target = GetTargetForCard(card);

            await card.OnPlayWrapper(choiceContext, target, isAutoPlay: true, new ResourceInfo
            {
                EnergySpent = 0,
                EnergyValue = card.EnergyCost.GetAmountToSpend(),
                StarsSpent = 0,
                StarValue = 0
            }, skipCardPileVisuals: false);
        }
    }

    private Creature? GetTargetForCard(CardModel card)
    {
        var combatState = CombatState;
        if (combatState == null)
            return null;

        return card.TargetType switch
        {
            TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy
                => Owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies),
            TargetType.AnyPlayer
                => Owner.RunState.Rng.CombatTargets.NextItem(combatState.Players.Where(p => p.Creature.IsAlive).Select(p => p.Creature)),
            TargetType.AnyAlly or TargetType.AllAllies
                => Owner.RunState.Rng.CombatTargets.NextItem(combatState.Allies.Where(c => c.IsAlive)),
            TargetType.Self => Owner.Creature,
            _ => null
        };
    }
}