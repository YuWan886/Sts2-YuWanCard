using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Characters;
using YuWanCard.Config;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Singletons;

[RegisterSingleton]
public class PigRewardAllCardPoolsBridge : YuWanSingletonModel
{
    public override bool ShouldReceiveCombatHooks => false;

    public static void RegisterHooks()
    {
        ModHelper.SubscribeForRunStateHooks(
            $"{MainFile.ModId}.PigRewardAllCardPoolsBridge",
            _ => [ModelDb.Singleton<PigRewardAllCardPoolsBridge>()]);
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        if (!ShouldRewriteReward(player, cardRewardOptions, creationOptions))
        {
            return false;
        }

        CardCreationResult? pigResult = cardRewardOptions.FirstOrDefault(IsPigPoolResult);
        if (pigResult == null)
        {
            return false;
        }

        int otherCardCount = cardRewardOptions.Count - 1;
        if (otherCardCount <= 0)
        {
            return false;
        }

        HashSet<ModelId> blockedIds = [pigResult.Card.CanonicalInstance.Id];
        List<CardModel> otherPoolCandidates = BuildOtherPoolCandidates(player, blockedIds);
        if (otherPoolCandidates.Count < otherCardCount)
        {
            return false;
        }

        List<CardCreationResult> replacementResults = CreateReplacementResults(
            player,
            otherPoolCandidates,
            otherCardCount,
            creationOptions);
        if (replacementResults.Count != otherCardCount)
        {
            return false;
        }

        cardRewardOptions.Clear();
        cardRewardOptions.Add(pigResult);
        cardRewardOptions.AddRange(replacementResults);
        return true;
    }

    private static bool ShouldRewriteReward(
        Player player,
        IReadOnlyCollection<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        return player.Character is Pig
               && YuWanContentAvailability.ShouldUsePigRewardAllCardPools()
               && cardRewardOptions.Count >= 2
               && creationOptions.Source == CardCreationSource.Encounter
               && !creationOptions.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications);
    }

    private static bool IsPigPoolResult(CardCreationResult result)
        => result.Card.CanonicalInstance.Pool is PigCardPool;

    private static List<CardModel> BuildOtherPoolCandidates(Player player, IReadOnlySet<ModelId> blockedIds)
    {
        return ModelDb.AllCardPools
            .Where(static pool => pool is not PigCardPool and not MockCardPool)
            .SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(static card => card.Pool is not PigCardPool)
            .Where(static card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
            .Where(YuWanContentAvailability.IsCardEnabled)
            .Where(card => !blockedIds.Contains(card.Id))
            .GroupBy(static card => card.Id)
            .Select(static group => group.First())
            .ToList();
    }

    private static List<CardCreationResult> CreateReplacementResults(
        Player player,
        IReadOnlyCollection<CardModel> candidates,
        int count,
        CardCreationOptions sourceOptions)
    {
        var candidateIds = new HashSet<ModelId>(candidates.Select(c => c.Id));
        CardCreationOptions replacementOptions = new CardCreationOptions(
                new[] { player.Character.CardPool },
                sourceOptions.Source,
                GetRarityOddsForCandidatePool(sourceOptions, candidates),
                card => candidateIds.Contains(card.Id))
            .WithFlags(sourceOptions.Flags | CardCreationFlags.NoModifyHooks);
        if (sourceOptions.RngOverride != null)
        {
            replacementOptions.WithRngOverride(sourceOptions.RngOverride);
        }

        return CardFactory.CreateForReward(player, count, replacementOptions).ToList();
    }

    private static CardRarityOddsType GetRarityOddsForCandidatePool(
        CardCreationOptions sourceOptions,
        IReadOnlyCollection<CardModel> candidates)
    {
        return candidates
            .Select(static card => card.Rarity)
            .Distinct()
            .Count() == 1
            ? CardRarityOddsType.Uniform
            : sourceOptions.RarityOdds;
    }
}
