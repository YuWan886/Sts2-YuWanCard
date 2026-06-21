using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
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
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = CombatState!.Enemies
            .Where(e => e.IsAlive)
            .ToList();

        foreach (var enemy in enemies)
        {
            enemy.RemoveAllPowersInternalExcept();
            await CreatureCmd.Kill(enemy, true);
        }

        if (CombatManager.Instance != null)
            await CombatManager.Instance.CheckWinCondition();

        if (DeckVersion != null)
        {
            await CardPileCmd.RemoveFromDeck(DeckVersion);
        }
    }
}
