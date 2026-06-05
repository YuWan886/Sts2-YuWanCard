using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Core.Multiplayer;

public readonly record struct MultiplayerModelIdentity(int Value)
{
    public static readonly MultiplayerModelIdentity None = new(0);

    public bool IsValid => Value != 0;
}

public readonly record struct MultiplayerModelIdentityToken(MultiplayerModelIdentity Identity, ModelId ModelId)
{
    public bool IsValid => Identity.IsValid;
}

internal static class MultiplayerModelIdentityRegistry
{
    private static readonly object Gate = new();
    private static readonly Dictionary<AbstractModel, int> ObjectToIdentity = new(new ReferenceComparer());
    private static readonly Dictionary<int, AbstractModel> IdentityToObject = [];
    private static int _nextIdentity = 1;

    public static void Clear()
    {
        lock (Gate)
        {
            ObjectToIdentity.Clear();
            IdentityToObject.Clear();
            _nextIdentity = 1;
        }
    }

    public static MultiplayerModelIdentity EnsureRegistered(AbstractModel? model)
    {
        if (model is not { IsMutable: true })
        {
            return MultiplayerModelIdentity.None;
        }

        lock (Gate)
        {
            if (ObjectToIdentity.TryGetValue(model, out int existing))
            {
                return new MultiplayerModelIdentity(existing);
            }

            int value = _nextIdentity++;
            ObjectToIdentity[model] = value;
            IdentityToObject[value] = model;
            return new MultiplayerModelIdentity(value);
        }
    }

    public static bool TryGetToken(AbstractModel? model, out MultiplayerModelIdentityToken token)
    {
        token = default;
        if (model == null)
        {
            return false;
        }

        lock (Gate)
        {
            if (!ObjectToIdentity.TryGetValue(model, out int identity) || identity == 0)
            {
                return false;
            }

            token = new MultiplayerModelIdentityToken(new MultiplayerModelIdentity(identity), model.Id);
            return true;
        }
    }

    public static bool TryResolve(MultiplayerModelIdentityToken token, out AbstractModel model)
    {
        model = null!;
        if (!token.IsValid)
        {
            return false;
        }

        lock (Gate)
        {
            if (!IdentityToObject.TryGetValue(token.Identity.Value, out AbstractModel? resolved))
            {
                return false;
            }

            if (resolved.Id != token.ModelId)
            {
                return false;
            }

            model = resolved;
            return true;
        }
    }

    public static void Unregister(AbstractModel? model)
    {
        if (model == null)
        {
            return;
        }

        lock (Gate)
        {
            if (!ObjectToIdentity.Remove(model, out int identity))
            {
                return;
            }

            if (IdentityToObject.TryGetValue(identity, out AbstractModel? current) && ReferenceEquals(current, model))
            {
                IdentityToObject.Remove(identity);
            }
        }
    }

    public static void RegisterCardTree(CardModel? card)
    {
        if (card == null)
        {
            return;
        }

        EnsureRegistered(card);
        EnsureRegistered(card.Affliction);
        EnsureRegistered(card.Enchantment);
    }

    public static void RegisterPlayerInventory(Player? player)
    {
        if (player == null)
        {
            return;
        }

        foreach (CardModel card in player.Deck.Cards)
        {
            RegisterCardTree(card);
        }

        foreach (var relic in player.Relics)
        {
            EnsureRegistered(relic);
        }

        foreach (var potion in player.PotionSlots)
        {
            EnsureRegistered(potion);
        }
    }

    public static void RegisterRunModifiers(RunState? runState)
    {
        if (runState == null)
        {
            return;
        }

        foreach (ModifierModel modifier in runState.Modifiers)
        {
            EnsureRegistered(modifier);
        }
    }

    public static PlayerInventoryIdentitySnapshot CapturePlayerInventory(Player player)
    {
        return new PlayerInventoryIdentitySnapshot(
            CaptureCards(player.Deck.Cards),
            CaptureModels(player.Relics),
            CaptureModels(player.PotionSlots));
    }

    public static void RestorePlayerInventory(Player player, PlayerInventoryIdentitySnapshot snapshot)
    {
        RestoreCards(snapshot.DeckCards, player.Deck.Cards);
        RestoreModels(snapshot.Relics, player.Relics);
        RestoreModels(snapshot.Potions, player.PotionSlots);
    }

    private static CardIdentitySnapshot[] CaptureCards(IReadOnlyList<CardModel> cards)
    {
        var result = new CardIdentitySnapshot[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            result[i] = CaptureCardTree(cards[i]);
        }

        return result;
    }

    private static ModelIdentitySnapshot[] CaptureModels<TModel>(IReadOnlyList<TModel> models)
        where TModel : AbstractModel?
    {
        var result = new ModelIdentitySnapshot[models.Count];
        for (int i = 0; i < models.Count; i++)
        {
            result[i] = Capture(models[i]);
        }

        return result;
    }

    private static ModelIdentitySnapshot Capture(AbstractModel? model)
    {
        return model != null && TryGetToken(model, out MultiplayerModelIdentityToken token)
            ? new ModelIdentitySnapshot(model, token)
            : default;
    }

    private static CardIdentitySnapshot CaptureCardTree(CardModel? card)
    {
        if (card == null)
        {
            return default;
        }

        return new CardIdentitySnapshot(
            Capture(card),
            Capture(card.Affliction),
            Capture(card.Enchantment));
    }

    private static void RestoreCards(
        IReadOnlyList<CardIdentitySnapshot> previous,
        IReadOnlyList<CardModel> current)
    {
        int count = Math.Min(previous.Count, current.Count);
        for (int i = 0; i < count; i++)
        {
            RestoreCardTree(previous[i], current[i]);
        }

        for (int i = count; i < previous.Count; i++)
        {
            Unregister(previous[i]);
        }

        for (int i = count; i < current.Count; i++)
        {
            RegisterCardTree(current[i]);
        }
    }

    private static void RestoreModels<TModel>(
        IReadOnlyList<ModelIdentitySnapshot> previous,
        IReadOnlyList<TModel> current)
        where TModel : AbstractModel?
    {
        int count = Math.Min(previous.Count, current.Count);
        for (int i = 0; i < count; i++)
        {
            Restore(previous[i], current[i]);
        }

        for (int i = count; i < previous.Count; i++)
        {
            Unregister(previous[i].Model);
        }

        for (int i = count; i < current.Count; i++)
        {
            EnsureRegistered(current[i]);
        }
    }

    private static void Restore(ModelIdentitySnapshot previous, AbstractModel? current)
    {
        if (current == null)
        {
            Unregister(previous.Model);
            return;
        }

        if (previous.Token.IsValid && previous.Token.ModelId == current.Id)
        {
            BindIdentity(current, previous.Token.Identity);
            return;
        }

        Unregister(previous.Model);
        EnsureRegistered(current);
    }

    private static void RestoreCardTree(CardIdentitySnapshot previous, CardModel? current)
    {
        if (current == null)
        {
            Unregister(previous);
            return;
        }

        Restore(previous.Card, current);
        Restore(previous.Affliction, current.Affliction);
        Restore(previous.Enchantment, current.Enchantment);
    }

    private static void Unregister(CardIdentitySnapshot snapshot)
    {
        Unregister(snapshot.Card.Model);
        Unregister(snapshot.Affliction.Model);
        Unregister(snapshot.Enchantment.Model);
    }

    private static void BindIdentity(AbstractModel model, MultiplayerModelIdentity identity)
    {
        if (!identity.IsValid || !model.IsMutable)
        {
            return;
        }

        lock (Gate)
        {
            if (ObjectToIdentity.Remove(model, out int oldIdentity)
                && IdentityToObject.TryGetValue(oldIdentity, out AbstractModel? oldCurrent)
                && ReferenceEquals(oldCurrent, model))
            {
                IdentityToObject.Remove(oldIdentity);
            }

            if (IdentityToObject.TryGetValue(identity.Value, out AbstractModel? previous))
            {
                ObjectToIdentity.Remove(previous);
            }

            ObjectToIdentity[model] = identity.Value;
            IdentityToObject[identity.Value] = model;
        }

        if (model is CardModel card)
        {
            RegisterCardTree(card);
        }
    }

    public readonly record struct PlayerInventoryIdentitySnapshot(
        CardIdentitySnapshot[] DeckCards,
        ModelIdentitySnapshot[] Relics,
        ModelIdentitySnapshot[] Potions);

    public readonly record struct CardIdentitySnapshot(
        ModelIdentitySnapshot Card,
        ModelIdentitySnapshot Affliction,
        ModelIdentitySnapshot Enchantment);

    public readonly record struct ModelIdentitySnapshot(
        AbstractModel? Model,
        MultiplayerModelIdentityToken Token);

    private sealed class ReferenceComparer : IEqualityComparer<AbstractModel>
    {
        public bool Equals(AbstractModel? x, AbstractModel? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(AbstractModel obj)
        {
            return ReferenceEqualityComparer.Instance.GetHashCode(obj);
        }
    }
}
