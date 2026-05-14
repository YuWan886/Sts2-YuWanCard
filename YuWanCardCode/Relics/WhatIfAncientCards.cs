using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfAncientCards : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public WhatIfAncientCards() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner?.Deck == null)
        {
            return;
        }

        var basicCards = Owner.Deck.Cards
            .Where(c => c.Rarity == CardRarity.Basic && c.IsRemovable)
            .ToList();

        if (basicCards.Count > 0)
        {
            await CardPileCmd.RemoveFromDeck(basicCards, showPreview: false);
        }

        var ancientCards = GetAncientCards(Owner).ToList();
        if (ancientCards.Count == 0)
        {
            return;
        }

        var cardsToAdd = ancientCards
            .Select(card => Owner.RunState.CreateCard(card, Owner))
            .ToList();

        await CardPileCmd.Add(cardsToAdd, PileType.Deck);
    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player != Owner || Owner == null)
        {
            return false;
        }

        var ancientCards = GetAncientCards(Owner).ToList();
        if (ancientCards.Count == 0)
        {
            return false;
        }

        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            var chosen = PickAncientCard(ancientCards, usedIds, Owner.RunState.Rng.Niche);
            cardRewardOptions[i] = new CardCreationResult(Owner.RunState.CreateCard(chosen, Owner));
        }

        return true;
    }

    private static IEnumerable<CardModel> GetAncientCards(Player player)
    {
        return ModelDb.AllCardPools
            .SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(c => c.Rarity == CardRarity.Ancient && !ArchaicTooth.TranscendenceCards.Contains(c))
            .DistinctBy(c => c.Id.Entry);
    }

    private static CardModel PickAncientCard(IReadOnlyList<CardModel> candidates, HashSet<string> usedIds, Rng rng)
    {
        var available = candidates
            .Where(c => c.Id.Entry != null && !usedIds.Contains(c.Id.Entry))
            .ToList();

        var pool = available.Count > 0 ? available : candidates.ToList();
        var selected = pool[rng.NextInt(pool.Count)];
        if (selected.Id.Entry != null)
        {
            usedIds.Add(selected.Id.Entry);
        }

        return selected;
    }
}
