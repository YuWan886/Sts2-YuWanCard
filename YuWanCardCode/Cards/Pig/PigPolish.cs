using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigPolish : YuWanCardModel
{
    public PigPolish() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithCards(1);
        WithVar("UpgradeCount", 1);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("UpgradeCount", IsUpgraded ? 2 : 1);
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var upgradableCards = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this && card.IsUpgradable)
            .ToList();

        int upgradeCount = IsUpgraded ? 2 : 1;
        int actualUpgradeCount = Math.Min(upgradeCount, upgradableCards.Count);
        if (actualUpgradeCount > 0)
        {
            if (upgradableCards.Count <= actualUpgradeCount)
            {
                foreach (var upgradableCard in upgradableCards)
                {
                    CardCmd.Upgrade(upgradableCard);
                }
            }
            else
            {
                var selectedCards = await CardSelectCmd.FromHand(
                    prefs: new CardSelectorPrefs(
                        new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"),
                        actualUpgradeCount,
                        actualUpgradeCount),
                    context: choiceContext,
                    player: Owner,
                    filter: card => card != this && card.IsUpgradable,
                    source: this);

                foreach (var selectedCard in selectedCards)
                {
                    CardCmd.Upgrade(selectedCard);
                }
            }
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}
