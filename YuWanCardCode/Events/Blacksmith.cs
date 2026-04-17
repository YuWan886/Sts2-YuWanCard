using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Events;

public sealed class Blacksmith : CustomEventModel
{
    public override ActModel[] Acts => [];

    public override string? CustomInitialPortraitPath => "res://YuWanCard/images/events/blacksmith.png";

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Any(p => p.Deck.Cards.Any(c => c.IsUpgradable) || p.Deck.Cards.Count(c => CanFuse(c)) >= 2);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        bool hasUpgradeableCards = Owner!.Deck.Cards.Any(c => c.IsUpgradable);
        bool hasFusableCards = Owner.Deck.Cards.Count(c => CanFuse(c)) >= 2;

        if (hasUpgradeableCards)
        {
            options.Add(new EventOption(this, UpgradeCards, InitialOptionKey("UPGRADE")));
        }
        else
        {
            options.Add(new EventOption(this, null, InitialOptionKey("UPGRADE")));
        }

        if (hasFusableCards)
        {
            options.Add(new EventOption(this, FuseCards, InitialOptionKey("FUSE")));
        }
        else
        {
            options.Add(new EventOption(this, null, InitialOptionKey("FUSE")));
        }

        return options;
    }

    private async Task UpgradeCards()
    {
        var cardsToUpgrade = await CardSelectCmd.FromDeckForUpgrade(
            Owner!,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1, 2)
        );

        var cardList = cardsToUpgrade.ToList();
        if (cardList.Count < 1)
        {
            SetEventFinished(L10NLookup("YUWANCARD-BLACKSMITH.pages.CANCELLED.description"));
            return;
        }

        foreach (var card in cardList)
        {
            CardCmd.Upgrade(card);
        }

        SetEventFinished(L10NLookup("YUWANCARD-BLACKSMITH.pages.UPGRADED.description"));
    }

    private async Task FuseCards()
    {
        try
        {
            var fusableCards = PileType.Deck.GetPile(Owner!).Cards
                .Where(c => CanFuse(c))
                .ToList();

            if (fusableCards.Count < 2)
            {
                SetEventFinished(L10NLookup("YUWANCARD-BLACKSMITH.pages.NO_CARDS.description"));
                return;
            }

            var cardsToFuse = await CardSelectCmd.FromDeckGeneric(
                Owner!,
                new CardSelectorPrefs(new LocString("events", "YUWANCARD-BLACKSMITH.pages.FUSE_PROMPT"), 2, 2),
                c => CanFuse(c)
            );

            var cardList = cardsToFuse.ToList();
            if (cardList.Count < 2)
            {
                SetEventFinished(L10NLookup("YUWANCARD-BLACKSMITH.pages.CANCELLED.description"));
                return;
            }

            await CardPileCmd.RemoveFromDeck(cardList);

            var resultRarity = CalculateFusionRarity(cardList[0].Rarity, cardList[1].Rarity);

            var cardPool = Owner!.Character.CardPool;
            var availableCards = cardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity == resultRarity && c.Rarity != CardRarity.Ancient)
                .ToList();

            if (availableCards.Count == 0)
            {
                availableCards = cardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                    .Where(c => c.Rarity == CardRarity.Rare)
                    .ToList();
            }

            if (availableCards.Count == 0)
            {
                SetEventFinished(L10NLookup("YUWANCARD-BLACKSMITH.pages.FUSE_FAILED.description"));
                return;
            }

            var selectedCardModel = availableCards[Rng.NextInt(availableCards.Count)];
            var newCard = Owner.RunState.CreateCard(selectedCardModel, Owner);
            var addResult = await CardPileCmd.Add(newCard, PileType.Deck);
            
            if (addResult.success)
            {
                CardCmd.PreviewCardPileAdd(addResult, 2f);
            }

            SetEventFinished(L10NLookup("YUWANCARD-BLACKSMITH.pages.FUSED.description"));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[Blacksmith] FuseCards error: {ex.Message}");
            SetEventFinished(L10NLookup("YUWANCARD-BLACKSMITH.pages.FUSE_FAILED.description"));
        }
    }

    private CardRarity CalculateFusionRarity(CardRarity rarity1, CardRarity rarity2)
    {
        if (rarity1 == rarity2)
        {
            return GetHigherRarity(rarity1);
        }

        var higherRarity = GetMaxRarity(rarity1, rarity2);
        int roll = Rng.NextInt(100);

        if (roll < 60)
        {
            return GetHigherRarity(higherRarity);
        }
        else
        {
            return higherRarity;
        }
    }

    private static CardRarity GetHigherRarity(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Basic => CardRarity.Common,
            CardRarity.Common => CardRarity.Uncommon,
            CardRarity.Uncommon => CardRarity.Rare,
            CardRarity.Rare => CardRarity.Rare,
            _ => CardRarity.Rare
        };
    }

    private static CardRarity GetMaxRarity(CardRarity r1, CardRarity r2)
    {
        int v1 = GetRarityValue(r1);
        int v2 = GetRarityValue(r2);
        return v1 >= v2 ? r1 : r2;
    }

    private static int GetRarityValue(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Basic => 0,
            CardRarity.Common => 1,
            CardRarity.Uncommon => 2,
            CardRarity.Rare => 3,
            CardRarity.Ancient => 4,
            _ => 0
        };
    }

    private static bool CanFuse(CardModel card)
    {
        return card.Rarity != CardRarity.Ancient &&
               card.Rarity != CardRarity.Status &&
               card.Rarity != CardRarity.Curse &&
               card.Rarity != CardRarity.Event &&
               card.Rarity != CardRarity.Token &&
               card.Rarity != CardRarity.Quest &&
               card.Rarity != CardRarity.None &&
               card.IsRemovable;
    }
}
