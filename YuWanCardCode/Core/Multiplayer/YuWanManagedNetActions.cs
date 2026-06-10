using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Core.Multiplayer;

public sealed record YuWanManagedNetActionDescriptor<T>(
    string ModuleId,
    string ActionKey,
    Func<T, byte[]> Serialize,
    Func<ReadOnlySpan<byte>, T> Deserialize,
    Func<YuWanManagedNetActionContext<T>, Task> Execute,
    GameActionType ActionType,
    string? DisplayName = null);

public readonly record struct YuWanManagedNetActionContext<T>(
    T Message,
    Player Player,
    YuWanManagedGameAction Action,
    GameActionPlayerChoiceContext PlayerChoiceContext);

public sealed class YuWanManagedNetAction : INetAction
{
    public ulong DescriptorOpcode { get; private set; }

    public GameActionType ManagedActionType { get; private set; }

    public byte[] Payload { get; private set; } = [];

    public void Serialize(PacketWriter writer)
    {
        YuWanManagedNetActions.WriteManagedActionBody(writer, DescriptorOpcode, ManagedActionType, Payload);
    }

    public void Deserialize(PacketReader reader)
    {
        if (!YuWanManagedNetActions.TryReadManagedActionBody(
                reader,
                out ulong descriptorOpcode,
                out GameActionType actionType,
                out byte[] payload))
        {
            throw new InvalidOperationException("Malformed YuWan managed action payload.");
        }

        DescriptorOpcode = descriptorOpcode;
        ManagedActionType = actionType;
        Payload = payload;
    }

    public GameAction ToGameAction(Player player)
    {
        return YuWanManagedNetActions.ToGameAction(player, this);
    }

    internal void Initialize(ulong descriptorOpcode, GameActionType actionType, byte[] payload)
    {
        DescriptorOpcode = descriptorOpcode;
        ManagedActionType = actionType;
        Payload = payload;
    }
}

public sealed class YuWanManagedGameAction(
    Player player,
    ulong descriptorOpcode,
    GameActionType actionType,
    byte[] payload)
    : GameAction
{
    public Player Player { get; } = player;

    public ulong DescriptorOpcode { get; } = descriptorOpcode;

    public byte[] Payload { get; } = payload;

    public override ulong OwnerId => Player.NetId;

    public override GameActionType ActionType { get; } = actionType;

    public override bool RecordableToReplay => true;

    protected override async Task ExecuteAction()
    {
        if (!YuWanManagedNetActions.TryGetRegistration(
                DescriptorOpcode,
                ActionType,
                out YuWanManagedNetActions.RegistrationBase registration))
        {
            MainFile.Logger.Error($"ManagedNetAction: missing descriptor opcode {DescriptorOpcode} for action type {ActionType}.");
            return;
        }

        GameActionPlayerChoiceContext choiceContext = new(this);
        try
        {
            await registration.Execute(this, choiceContext);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"ManagedNetAction: action opcode {DescriptorOpcode} type {ActionType} failed: {ex}");
        }
    }

    public override INetAction ToNetAction()
    {
        var netAction = new YuWanManagedNetAction();
        netAction.Initialize(DescriptorOpcode, ActionType, Payload);
        return netAction;
    }

    public override string ToString()
    {
        return YuWanManagedNetActions.TryGetRegistration(
            DescriptorOpcode,
            ActionType,
            out YuWanManagedNetActions.RegistrationBase registration)
            ? registration.DisplayName
            : $"YuWanManagedGameAction player {OwnerId} opcode {DescriptorOpcode} type {ActionType}";
    }
}

public static class YuWanManagedNetActions
{
    private const ulong ManagedActionMagic = 0x59_57_4D_41_4E_41_43_54;
    private const byte Version = 1;
    private const int InitialOffset = 0;
    private const int ByteBits = 8;
    private const int ManagedActionMagicBits = 64;

    private static readonly Lock Gate = new();
    private static readonly Dictionary<ulong, RegistrationBase> Registrations = [];

    public static ulong Register<T>(YuWanManagedNetActionDescriptor<T> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.ModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.ActionKey);
        ArgumentNullException.ThrowIfNull(descriptor.Serialize);
        ArgumentNullException.ThrowIfNull(descriptor.Deserialize);
        ArgumentNullException.ThrowIfNull(descriptor.Execute);
        ValidateActionType(descriptor.ActionType);

        ulong opcode = ComputeOpcode(descriptor.ModuleId, descriptor.ActionKey);
        lock (Gate)
        {
            if (Registrations.TryGetValue(opcode, out RegistrationBase? existing))
            {
                if (existing is Registration<T> typed
                    && typed.ModuleId == descriptor.ModuleId
                    && typed.ActionKey == descriptor.ActionKey
                    && typed.ActionType == descriptor.ActionType)
                {
                    return opcode;
                }

                throw new InvalidOperationException(
                    $"Managed net action opcode conflict: {descriptor.ModuleId}/{descriptor.ActionKey} -> {opcode}");
            }

            Registrations[opcode] = new Registration<T>(
                descriptor.ModuleId,
                descriptor.ActionKey,
                descriptor.Deserialize,
                descriptor.Execute,
                descriptor.ActionType,
                descriptor.DisplayName);
        }

        return opcode;
    }

    public static bool Request<T>(
        RunManager? runManager,
        YuWanManagedNetActionDescriptor<T> descriptor,
        T message,
        ulong? ownerNetId = null)
    {
        ulong opcode = Register(descriptor);
        RunManager? rm = runManager ?? RunManager.Instance;
        INetGameService? net = rm?.NetService;
        RunState? state = rm?.DebugOnlyGetState();
        if (rm == null || net == null || state == null)
        {
            return false;
        }

        if (!CanSendManagedAction(net))
        {
            return false;
        }

        ulong owner = ownerNetId ?? net.NetId;
        if (owner != net.NetId)
        {
            return false;
        }

        Player? player = state.Players.FirstOrDefault(p => p.NetId == owner);
        if (player == null)
        {
            return false;
        }

        byte[] payload = descriptor.Serialize(message);
        var action = new YuWanManagedGameAction(player, opcode, descriptor.ActionType, payload);
        rm.ActionQueueSynchronizer.RequestEnqueue(action);
        return true;
    }

    internal static bool TryWriteNetAction(PacketWriter writer, INetAction action)
    {
        if (action is not YuWanManagedNetAction managed)
        {
            return false;
        }

        managed.Serialize(writer);
        return true;
    }

    internal static INetAction ReadNetAction(PacketReader reader)
    {
        var action = new YuWanManagedNetAction();
        action.Deserialize(reader);
        return action;
    }

    internal static void WriteManagedActionBody(
        PacketWriter writer,
        ulong descriptorOpcode,
        GameActionType actionType,
        ReadOnlySpan<byte> payload)
    {
        writer.WriteULong(ManagedActionMagic);
        writer.WriteByte(Version);
        writer.WriteULong(descriptorOpcode);
        writer.WriteEnum(actionType);
        writer.WriteInt(payload.Length);
        writer.WriteBytes(payload.ToArray(), payload.Length);
    }

    internal static bool TryReadManagedActionBody(
        PacketReader reader,
        out ulong descriptorOpcode,
        out GameActionType actionType,
        out byte[] payload)
    {
        descriptorOpcode = 0;
        actionType = default;
        payload = [];
        if (reader.ReadULong() != ManagedActionMagic || reader.ReadByte() != Version)
        {
            return false;
        }

        descriptorOpcode = reader.ReadULong();
        actionType = reader.ReadEnum<GameActionType>();
        ValidateActionType(actionType);
        int length = reader.ReadInt();
        if (length < 0)
        {
            return false;
        }

        payload = new byte[length];
        reader.ReadBytes(payload, length);
        return true;
    }

    internal static GameAction ToGameAction(Player player, YuWanManagedNetAction action)
    {
        return new YuWanManagedGameAction(player, action.DescriptorOpcode, action.ManagedActionType, action.Payload);
    }

    internal static bool TryGetRegistration(
        ulong opcode,
        GameActionType actionType,
        out RegistrationBase registration)
    {
        lock (Gate)
        {
            return Registrations.TryGetValue(opcode, out registration!) && registration.ActionType == actionType;
        }
    }

    internal static bool NextPayloadIsManagedAction(PacketReader reader, int bitOffset = InitialOffset)
    {
        return TryPeekULong(reader, bitOffset, out ulong magic)
               && magic == ManagedActionMagic
               && TryPeekByte(reader, bitOffset + ManagedActionMagicBits, out byte version)
               && version == Version;
    }

    internal static bool TryPeekInt(PacketReader reader, int bitOffset, int bits, out int value)
    {
        value = 0;
        if (!TryReadBits(reader, bitOffset, bits, out byte[] buffer))
        {
            return false;
        }

        Span<byte> scratch = stackalloc byte[sizeof(int)];
        buffer.AsSpan().CopyTo(scratch);
        value = BinaryPrimitives.ReadInt32LittleEndian(scratch);
        return true;
    }

    private static bool CanSendManagedAction(INetGameService netService)
    {
        return netService switch
        {
            { Type: NetGameType.Singleplayer } => true,
            { Type: NetGameType.Replay } => false,
            NetClientGameService { IsConnected: true } => true,
            NetHostGameService { IsConnected: true } => true,
            _ => false
        };
    }

    private static ulong ComputeOpcode(string moduleId, string actionKey)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes($"{moduleId}\u001F{actionKey}");
        byte[] hash = SHA256.HashData(utf8);
        ulong opcode = BinaryPrimitives.ReadUInt64LittleEndian(hash);
        return opcode == 0 ? 1UL : opcode;
    }

    private static void ValidateActionType(GameActionType actionType)
    {
        if (actionType is GameActionType.None)
        {
            throw new InvalidOperationException("Managed net actions do not support GameActionType.None.");
        }
    }

    private static bool TryPeekULong(PacketReader reader, int bitOffset, out ulong value)
    {
        value = 0;
        if (!TryReadBits(reader, bitOffset, ManagedActionMagicBits, out byte[] buffer))
        {
            return false;
        }

        value = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        return true;
    }

    private static bool TryPeekByte(PacketReader reader, int bitOffset, out byte value)
    {
        value = 0;
        if (!TryReadBits(reader, bitOffset, ByteBits, out byte[] buffer))
        {
            return false;
        }

        value = buffer[InitialOffset];
        return true;
    }

    private static bool TryReadBits(PacketReader reader, int bitOffset, int bitCount, out byte[] destination)
    {
        destination = new byte[(bitCount + ByteBits - 1) / ByteBits];
        int originBitPosition = reader.BitPosition + bitOffset;
        if (originBitPosition < 0
            || bitCount < 0
            || reader.Buffer.Length * ByteBits - originBitPosition < bitCount)
        {
            return false;
        }

        for (int i = 0; i < bitCount; i++)
        {
            if (GetBit(reader.Buffer, originBitPosition + i))
            {
                destination[i / ByteBits] |= (byte)(1 << (i % ByteBits));
            }
        }

        return true;
    }

    private static bool GetBit(byte[] buffer, int bitPosition)
    {
        return (buffer[bitPosition / ByteBits] & (1 << (bitPosition % ByteBits))) != 0;
    }

    internal abstract class RegistrationBase(
        string moduleId,
        string actionKey,
        GameActionType actionType,
        string? displayName)
    {
        public string ModuleId { get; } = moduleId;

        public string ActionKey { get; } = actionKey;

        public GameActionType ActionType { get; } = actionType;

        public string DisplayName { get; } = string.IsNullOrWhiteSpace(displayName)
            ? $"YuWanManagedGameAction {moduleId}/{actionKey}"
            : displayName!;

        public abstract Task Execute(YuWanManagedGameAction action, GameActionPlayerChoiceContext choiceContext);
    }

    private sealed class Registration<T>(
        string moduleId,
        string actionKey,
        Func<ReadOnlySpan<byte>, T> deserialize,
        Func<YuWanManagedNetActionContext<T>, Task> execute,
        GameActionType actionType,
        string? displayName)
        : RegistrationBase(moduleId, actionKey, actionType, displayName)
    {
        public override async Task Execute(YuWanManagedGameAction action, GameActionPlayerChoiceContext choiceContext)
        {
            T message = deserialize(action.Payload);
            var context = new YuWanManagedNetActionContext<T>(message, action.Player, action, choiceContext);
            await execute(context);
        }
    }
}
