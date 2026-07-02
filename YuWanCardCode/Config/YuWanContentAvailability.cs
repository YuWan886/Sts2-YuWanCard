using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Multiplayer;

namespace YuWanCard.Config;

internal static class YuWanContentAvailability
{
    public static bool IsCardEnabled(CardModel? card)
        => card == null || IsCardTypeEnabled(card.GetType());

    public static bool IsCardTypeEnabled<TCard>() where TCard : CardModel
        => IsCardTypeEnabled(typeof(TCard));

    public static bool IsCardTypeEnabled(Type cardType)
    {
        return !YuWanColorlessCardCatalog.TryGetDefinition(cardType, out _)
               || IsColorlessCardTypeEnabled(cardType);
    }

    public static CardModel? TryCreateAvailableCard(Player player, CardModel canonicalCard)
    {
        return IsCardEnabled(canonicalCard)
            ? player.RunState.CreateCard(canonicalCard, player)
            : null;
    }

    public static CardModel? TryCreateAvailableCard<TCard>(Player player) where TCard : CardModel
        => TryCreateAvailableCard(player, ModelDb.Card<TCard>());

    public static IEnumerable<CardModel> FilterAvailableCards(IEnumerable<CardModel> cards)
        => cards.Where(IsCardEnabled);

    public static IEnumerable<CardCreationResult> FilterAvailableCardCreationResults(
        IEnumerable<CardCreationResult> results)
    {
        return results.Where(static result => IsCardEnabled(result.Card));
    }

    public static bool IsColorlessCardTypeEnabled<TCard>() where TCard : CardModel
        => IsColorlessCardTypeEnabled(typeof(TCard));

    public static bool IsColorlessCardTypeEnabled(Type cardType)
    {
        return GetEffectiveSnapshot("colorless_card", cardType).IsColorlessCardTypeEnabled(cardType);
    }

    public static bool IsEncounterTypeEnabled<TEncounter>() where TEncounter : EncounterModel
        => IsEncounterTypeEnabled(typeof(TEncounter));

    public static bool IsEncounterTypeEnabled(Type encounterType)
    {
        return GetEffectiveSnapshot("encounter", encounterType).IsEncounterTypeEnabled(encounterType);
    }

    public static bool IsEventTypeEnabled<TEvent>() where TEvent : EventModel
        => IsEventTypeEnabled(typeof(TEvent));

    public static bool IsEventTypeEnabled(Type eventType)
    {
        return GetEffectiveSnapshot("event", eventType).IsEventTypeEnabled(eventType);
    }

    public static bool IsAncientTypeEnabled<TAncient>() where TAncient : AncientEventModel
        => IsAncientTypeEnabled(typeof(TAncient));

    public static bool IsAncientTypeEnabled(Type ancientType)
    {
        return GetEffectiveSnapshot("ancient", ancientType).IsAncientTypeEnabled(ancientType);
    }

    private static YuWanContentSettingsSnapshot GetEffectiveSnapshot(string contentKind, Type contentType)
    {
        if (YuWanContentSettingsSync.TryGetClientAuthoritativeSnapshot(out var snapshot))
        {
            return snapshot;
        }

        if (YuWanContentSettingsSync.IsClientAwaitingAuthoritativeSnapshot())
        {
            YuWanContentSettingsSync.LogAwaitingAuthoritativeSnapshotUse(contentKind, contentType);
            return YuWanContentSettingsSnapshot.AllDisabled;
        }

        return YuWanContentSettingsSnapshot.CaptureLocal();
    }
}
