using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Utils;

public sealed class ModCardTagRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, ModCardTagRegistry> Registries = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CardTag> Definitions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<CardTag, string> DefinitionsByCardTag = [];
    private static readonly DynamicEnumValueMinter Minter = new();

    private readonly string _modId;
    private string? _freezeReason;

    public static bool IsFrozen { get; private set; }

    private ModCardTagRegistry(string modId)
    {
        _modId = modId;
    }

    public static ModCardTagRegistry For(string modId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);

        lock (SyncRoot)
        {
            if (Registries.TryGetValue(modId, out var existing))
                return existing;

            var created = new ModCardTagRegistry(modId);
            Registries[modId] = created;
            return created;
        }
    }

    public static void FreezeRegistrations(string reason)
    {
        lock (SyncRoot)
        {
            if (IsFrozen) return;
            IsFrozen = true;
            foreach (var registry in Registries.Values)
                registry._freezeReason = reason;
        }
        MainFile.Logger.Info($"[ModCardTagRegistry] Card tag registration frozen ({reason})");
    }

    public CardTag RegisterOwned(string localTagStem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localTagStem);
        var id = $"{_modId.ToUpperInvariant()}-{localTagStem.ToUpperInvariant()}";
        return RegisterCore(id);
    }

    public CardTag Register(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return RegisterCore(id);
    }

    private CardTag RegisterCore(string id)
    {
        EnsureMutable("register card tags");

        var normalizedId = id.Trim().ToUpperInvariant();
        lock (SyncRoot)
        {
            if (Definitions.TryGetValue(normalizedId, out var existing))
                return existing;

            var cardTagValue = Minter.Mint(normalizedId);
            Definitions[normalizedId] = cardTagValue;
            DefinitionsByCardTag[cardTagValue] = normalizedId;

            MainFile.Logger.Info($"[ModCardTagRegistry] Registered tag: {normalizedId} (CardTag=0x{(int)cardTagValue:X8})");
            return cardTagValue;
        }
    }

    public static bool TryGetCardTag(string id, out CardTag value)
    {
        lock (SyncRoot)
        {
            return Definitions.TryGetValue(id.Trim().ToUpperInvariant(), out value);
        }
    }

    public static CardTag GetCardTag(string id)
    {
        if (TryGetCardTag(id, out var value))
            return value;
        throw new KeyNotFoundException($"Card tag '{id}' is not registered.");
    }

    public static bool IsModCardTag(CardTag value)
    {
        lock (SyncRoot)
        {
            return DefinitionsByCardTag.ContainsKey(value);
        }
    }

    private void EnsureMutable(string operation)
    {
        if (!IsFrozen) return;
        throw new InvalidOperationException(
            $"Cannot {operation} after card tag registration has been frozen ({_freezeReason ?? "unknown"}).");
    }
}

internal sealed class DynamicEnumValueMinter
{
    public const int DefaultReservedFloor = 0x4000_0000;

    private readonly Dictionary<string, CardTag> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<CardTag, string> _byValue = [];
    private readonly object _sync = new();

    public int ReservedFloor { get; }

    public DynamicEnumValueMinter() : this(DefaultReservedFloor) { }

    public DynamicEnumValueMinter(int reservedFloor)
    {
        if (reservedFloor < 0)
            throw new ArgumentOutOfRangeException(nameof(reservedFloor), "Reserved floor must be non-negative.");

        ReservedFloor = reservedFloor;
    }

    public CardTag Mint(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var normalized = id.Trim().ToUpperInvariant();

        lock (_sync)
        {
            if (_byId.TryGetValue(normalized, out var existing))
                return existing;

            var value = Compute(normalized);

            if (_byValue.TryGetValue(value, out var conflict))
                throw new InvalidOperationException(
                    $"DynamicEnumValueMinter hash collision: '{normalized}' and '{conflict}' both map to the same numeric value.");

            _byId[normalized] = value;
            _byValue[value] = normalized;
            return value;
        }
    }

    private CardTag Compute(string normalizedId)
    {
        var bytes = Encoding.UTF8.GetBytes(normalizedId);
        var hashBytes = SHA256.HashData(bytes);
        var hash = BitConverter.ToUInt32(hashBytes, 0);

        var floor = (uint)ReservedFloor;
        var range = int.MaxValue - floor + 1u;
        var raw = unchecked((int)(floor + hash % range));
        return Unsafe.As<int, CardTag>(ref raw);
    }
}

public static class ModCardTagExtensions
{
    public static void AddModCardTag(this CardModel card, CardTag tag)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (card.Tags is not HashSet<CardTag> storage)
            throw new InvalidOperationException("CardModel.Tags is not backed by a mutable HashSet<CardTag>.");

        storage.Add(tag);
    }

    public static bool HasModCardTag(this CardModel card, CardTag tag)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Tags.Contains(tag);
    }
}
