using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigFishPig : YuWanCardModel
{
    private static readonly LocString DiscardSelectionScreenPrompt =
        new("cards", "YUWANCARD-PIG_FISH_PIG.discardSelectionScreenPrompt");

    public PigFishPig() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
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
        var discardableCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (discardableCards.Count == 0)
        {
            return;
        }

        var discardPrefs = new CardSelectorPrefs(DiscardSelectionScreenPrompt, 1, 1);

        var cardsToDiscard = await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            discardPrefs,
            null,
            this
        );

        var discardList = cardsToDiscard.ToList();
        var discardedCard = discardList.FirstOrDefault();
        if (discardedCard == null)
        {
            return;
        }

        await CardCmd.Discard(choiceContext, discardList);

        var upgradableCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.IsUpgradable)
            .ToList();

        if (upgradableCards.Count == 0)
        {
            return;
        }

        int upgradeCount = Math.Min(IsUpgraded ? 2 : 1, upgradableCards.Count);
        var upgradePrefs = new CardSelectorPrefs(
            new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"),
            upgradeCount,
            upgradeCount
        );

        var cardsToUpgrade = await CardSelectCmd.FromHand(
            context: choiceContext,
            player: Owner,
            prefs: upgradePrefs,
            filter: c => c.IsUpgradable,
            source: this
        );

        foreach (var selectedCard in cardsToUpgrade)
        {
            CardCmd.Upgrade(selectedCard);
        }
    }
}
