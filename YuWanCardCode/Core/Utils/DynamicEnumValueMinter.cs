using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace YuWanCard.Core.Utils;

public sealed class DynamicEnumValueMinter<TEnum> where TEnum : struct, Enum
{
    public const int DefaultReservedFloor = 0x4000_0000;

    private readonly Dictionary<string, TEnum> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<TEnum, string> _byValue = [];
    private readonly Lock _sync = new();

    public int ReservedFloor { get; }

    public DynamicEnumValueMinter() : this(DefaultReservedFloor) { }

    public DynamicEnumValueMinter(int reservedFloor)
    {
        if (Unsafe.SizeOf<TEnum>() != sizeof(int))
            throw new NotSupportedException(
                $"DynamicEnumValueMinter only supports 32-bit backed enums; '{typeof(TEnum).FullName}' is "
                + $"{Unsafe.SizeOf<TEnum>() * 8}-bit.");

        if (reservedFloor < 0)
            throw new ArgumentOutOfRangeException(nameof(reservedFloor),
                "Reserved floor must be non-negative.");

        ReservedFloor = reservedFloor;
    }

    public TEnum Mint(string id)
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
                    $"DynamicEnumValueMinter<{typeof(TEnum).Name}> hash collision: '{normalized}' and '{conflict}' both map to the same numeric value.");

            _byId[normalized] = value;
            _byValue[value] = normalized;
            return value;
        }
    }

    public bool IsDynamic(TEnum value)
    {
        lock (_sync)
        {
            return _byValue.ContainsKey(value);
        }
    }

    private TEnum Compute(string normalizedId)
    {
        var bytes = Encoding.UTF8.GetBytes(normalizedId);
        var hashBytes = SHA256.HashData(bytes);
        var hash = BitConverter.ToUInt32(hashBytes, 0);

        var floor = (uint)ReservedFloor;
        var range = int.MaxValue - floor + 1u;
        var raw = unchecked((int)(floor + hash % range));
        return Unsafe.As<int, TEnum>(ref raw);
    }
}
