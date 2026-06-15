using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Relics;

public abstract class WhatIfUniformCardRelicModel : WhatIfRelicModel
{
    protected WhatIfUniformCardRelicModel() : base(true)
    {
    }

    /// <summary>Predicate selecting which cards this relic restricts the run to.</summary>
    protected abstract bool Matches(CardModel card);

    private List<CardModel> GetCandidateCards(Player player)
    {
        // Prefer cards from the character's own card pool. Only fall back to the
        // global pool set when the character pool yields no matching candidates.
        var characterPool = player.Character?.CardPool;
        if (characterPool != null)
        {
            var characterCandidates = characterPool
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Where(Matches)
                .DistinctBy(c => c.Id.Entry)
                .ToList();

            if (characterCandidates.Count > 0)
            {
                return characterCandidates;
            }
        }

        return ModelDb.AllCardPools
            .SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(Matches)
            .DistinctBy(c => c.Id.Entry)
            .ToList();
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner?.Deck == null)
        {
            return;
        }

        var originalCards = Owner.Deck.Cards
            .Where(c => c.IsTransformable)
            .ToList();

        if (originalCards.Count == 0)
        {
            return;
        }

        var candidates = GetCandidateCards(Owner);
        if (candidates.Count == 0)
        {
            return;
        }

        var transformations = new List<CardTransformation>();
        foreach (var card in originalCards)
        {
            var picked = candidates[Owner.RunState.Rng.Niche.NextInt(candidates.Count)];
            transformations.Add(new CardTransformation(card, Owner.RunState.CreateCard(picked, Owner)));
        }

        await CardCmd.Transform(transformations, null, CardPreviewStyle.None);
    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player != Owner || Owner == null)
        {
            return false;
        }

        var candidates = GetCandidateCards(Owner);
        if (candidates.Count == 0)
        {
            return false;
        }

        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < cardRewardOptions.Count; i++)
        {
            var available = candidates.Where(c => c.Id.Entry != null && !usedIds.Contains(c.Id.Entry)).ToList();
            var pool = available.Count > 0 ? available : candidates;
            var picked = pool[Owner.RunState.Rng.Niche.NextInt(pool.Count)];
            if (picked.Id.Entry != null)
            {
                usedIds.Add(picked.Id.Entry);
            }
            cardRewardOptions[i] = new CardCreationResult(Owner.RunState.CreateCard(picked, Owner));
        }

        return true;
    }
}
