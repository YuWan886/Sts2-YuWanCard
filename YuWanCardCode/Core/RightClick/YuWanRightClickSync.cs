using System.Buffers.Binary;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Core.RightClick;

internal readonly record struct YuWanRightClickManagedPayload(
    ulong OwnerNetId,
    YuWanRightClickModelKind Kind,
    MultiplayerModelIdentityToken ModelToken,
    YuWanRightClickTrigger Trigger,
    IReadOnlyList<YuWanRightClickBindingId> BindingIds);

public sealed class YuWanRightClickManagedNetAction : INetAction
{
    public GameActionType ManagedActionType { get; private set; }

    public byte[] Payload { get; private set; } = [];

    public void Serialize(PacketWriter writer)
    {
        YuWanRightClickManagedActions.WriteManagedActionBody(writer, ManagedActionType, Payload);
    }

    public void Deserialize(PacketReader reader)
    {
        if (!YuWanRightClickManagedActions.TryReadManagedActionBody(reader, out GameActionType actionType, out byte[] payload))
        {
            throw new InvalidOperationException("Malformed YuWan managed right-click payload.");
        }

        ManagedActionType = actionType;
        Payload = payload;
    }

    public GameAction ToGameAction(Player player)
    {
        return YuWanRightClickManagedActions.ToGameAction(player, this);
    }

    internal void Initialize(GameActionType actionType, byte[] payload)
    {
        ManagedActionType = actionType;
        Payload = payload;
    }
}

public sealed class YuWanRightClickManagedGameAction(
    Player player,
    GameActionType actionType,
    byte[] payload)
    : GameAction
{
    public Player Player { get; } = player;

    public byte[] Payload { get; } = payload;

    public override ulong OwnerId => Player.NetId;

    public override GameActionType ActionType { get; } = actionType;

    public override bool RecordableToReplay => true;

    protected override async Task ExecuteAction()
    {
        GameActionPlayerChoiceContext choiceContext = new(this);
        await YuWanRightClickManagedActions.ExecuteManaged(this, choiceContext);
    }

    public override INetAction ToNetAction()
    {
        var netAction = new YuWanRightClickManagedNetAction();
        netAction.Initialize(ActionType, Payload);
        return netAction;
    }
}

internal static class YuWanRightClickManagedActions
{
    private const ulong ManagedActionMagic = 0x59_57_52_43_4D_41_4E_54;
    private const byte Version = 1;
    private const int InitialOffset = 0;
    private const int ByteBits = 8;
    private const int ManagedActionMagicBits = 64;

    public static bool Request(
        RunManager? runManager,
        YuWanRightClickManagedPayload payload,
        ulong? ownerNetId = null)
    {
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

        GameActionType actionType = CombatManager.Instance.IsInProgress
            ? GameActionType.CombatPlayPhaseOnly
            : GameActionType.NonCombat;
        byte[] serializedPayload = SerializePayload(payload);
        var action = new YuWanRightClickManagedGameAction(player, actionType, serializedPayload);
        rm.ActionQueueSynchronizer.RequestEnqueue(action);
        return true;
    }

    internal static bool TryWriteNetAction(PacketWriter writer, INetAction action)
    {
        if (action is not YuWanRightClickManagedNetAction managed)
        {
            return false;
        }

        managed.Serialize(writer);
        return true;
    }

    internal static INetAction ReadNetAction(PacketReader reader)
    {
        var action = new YuWanRightClickManagedNetAction();
        action.Deserialize(reader);
        return action;
    }

    internal static void WriteManagedActionBody(
        PacketWriter writer,
        GameActionType actionType,
        ReadOnlySpan<byte> payload)
    {
        writer.WriteULong(ManagedActionMagic);
        writer.WriteByte(Version);
        writer.WriteEnum(actionType);
        writer.WriteInt(payload.Length);
        writer.WriteBytes(payload.ToArray(), payload.Length);
    }

    internal static bool TryReadManagedActionBody(
        PacketReader reader,
        out GameActionType actionType,
        out byte[] payload)
    {
        actionType = default;
        payload = [];
        if (reader.ReadULong() != ManagedActionMagic || reader.ReadByte() != Version)
        {
            return false;
        }

        actionType = reader.ReadEnum<GameActionType>();
        if (actionType == GameActionType.None)
        {
            return false;
        }

        int length = reader.ReadInt();
        if (length < 0)
        {
            return false;
        }

        payload = new byte[length];
        reader.ReadBytes(payload, length);
        return true;
    }

    internal static GameAction ToGameAction(Player player, YuWanRightClickManagedNetAction action)
    {
        return new YuWanRightClickManagedGameAction(player, action.ManagedActionType, action.Payload);
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

    internal static async Task ExecuteManaged(
        YuWanRightClickManagedGameAction action,
        GameActionPlayerChoiceContext choiceContext)
    {
        try
        {
            YuWanRightClickManagedPayload payload = DeserializePayload(action.Payload);
            if (payload.OwnerNetId != action.Player.NetId)
            {
                return;
            }

            await YuWanRightClickRegistry.ExecuteManagedPayload(payload, choiceContext, action);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"RightClick: managed action execution failed: {ex}");
        }
    }

    private static byte[] SerializePayload(YuWanRightClickManagedPayload payload)
    {
        var writer = new PacketWriter();
        writer.WriteULong(payload.OwnerNetId);
        writer.WriteEnum(payload.Kind);
        writer.WriteInt(payload.ModelToken.Identity.Value);
        writer.WriteFullModelId(payload.ModelToken.ModelId);
        writer.WriteBool(payload.Trigger.IsController);
        writer.WriteBool(payload.Trigger.Metadata != null);
        if (payload.Trigger.Metadata != null)
        {
            writer.WriteString(payload.Trigger.Metadata);
        }

        writer.WriteInt(payload.BindingIds.Count);
        foreach (YuWanRightClickBindingId bindingId in payload.BindingIds)
        {
            writer.WriteString(bindingId.Id);
        }

        return writer.Buffer[..(int)Math.Ceiling(writer.BitPosition / 8f)];
    }

    private static YuWanRightClickManagedPayload DeserializePayload(ReadOnlySpan<byte> bytes)
    {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());

        ulong ownerNetId = reader.ReadULong();
        YuWanRightClickModelKind kind = reader.ReadEnum<YuWanRightClickModelKind>();
        var token = new MultiplayerModelIdentityToken(
            new MultiplayerModelIdentity(reader.ReadInt()),
            reader.ReadFullModelId());

        bool isController = reader.ReadBool();
        string? metadata = reader.ReadBool() ? reader.ReadString() : null;

        int bindingCount = reader.ReadInt();
        var bindingIds = new List<YuWanRightClickBindingId>(Math.Max(bindingCount, 0));
        for (int i = 0; i < bindingCount; i++)
        {
            string id = reader.ReadString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                bindingIds.Add(new YuWanRightClickBindingId(id.Trim()));
            }
        }

        return new YuWanRightClickManagedPayload(
            ownerNetId,
            kind,
            token,
            new YuWanRightClickTrigger(isController, metadata),
            bindingIds);
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
}
