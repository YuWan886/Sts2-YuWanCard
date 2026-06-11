using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Modding;
using YuWanCard.Cards;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Singletons;

[RegisterSingleton]
public class PigApotheosisObtainBridge : YuWanSingletonModel
{
    private static readonly Dictionary<CardModel, PigApotheosis> PendingPreventedApotheosisUpgrades = [];

    public override bool ShouldReceiveCombatHooks => true;

    public static void RegisterHooks()
    {
        ModHelper.SubscribeForRunStateHooks(
            $"{MainFile.ModId}.PigApotheosisObtainBridge",
            _ => [ModelDb.Singleton<PigApotheosisObtainBridge>()]);
    }

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (oldPileType != PileType.None || card.Pile?.Type != PileType.Deck)
        {
            return;
        }

        if (card is PigApotheosis pigApotheosis)
        {
            await HandlePigApotheosisObtained(pigApotheosis);
            return;
        }

        if (card.Id == ModelDb.Card<Apotheosis>().Id)
        {
            await HandleApotheosisObtained(card);
        }
    }

    public override bool ShouldAddToDeck(CardModel card)
    {
        if (card.Id != ModelDb.Card<Apotheosis>().Id)
        {
            return true;
        }

        var pigApotheosis = FindPreferredPigApotheosis(card.Owner.Deck.Cards, excludedCard: card);
        if (pigApotheosis == null)
        {
            return true;
        }

        PendingPreventedApotheosisUpgrades[card] = pigApotheosis;
        return false;
    }

    public override Task AfterAddToDeckPrevented(CardModel card)
    {
        if (!PendingPreventedApotheosisUpgrades.Remove(card, out var pigApotheosis))
        {
            return Task.CompletedTask;
        }

        if (pigApotheosis.Pile?.Type == PileType.Deck && pigApotheosis.IsUpgradable)
        {
            CardCmd.Upgrade(pigApotheosis);
        }

        return Task.CompletedTask;
    }

    private static async Task HandlePigApotheosisObtained(PigApotheosis pigApotheosis)
    {
        var apotheosis = pigApotheosis.Owner.Deck.Cards
            .FirstOrDefault(card => card != pigApotheosis && card.Id == ModelDb.Card<Apotheosis>().Id);

        if (apotheosis == null)
        {
            return;
        }

        if (pigApotheosis.IsUpgradable)
        {
            CardCmd.Upgrade(pigApotheosis);
        }

        await CardPileCmd.RemoveFromDeck(apotheosis, showPreview: false);
    }

    private static async Task HandleApotheosisObtained(CardModel apotheosis)
    {
        var pigApotheosis = FindPreferredPigApotheosis(apotheosis.Owner.Deck.Cards, apotheosis);

        if (pigApotheosis == null)
        {
            return;
        }

        if (pigApotheosis.IsUpgradable)
        {
            CardCmd.Upgrade(pigApotheosis);
        }

        await CardPileCmd.RemoveFromDeck(apotheosis, showPreview: false);
    }

    private static PigApotheosis? FindPreferredPigApotheosis(IEnumerable<CardModel> cards, CardModel excludedCard)
    {
        return cards
            .OfType<PigApotheosis>()
            .Where(card => card != excludedCard)
            .OrderByDescending(card => card.IsUpgradable)
            .FirstOrDefault();
    }
}
