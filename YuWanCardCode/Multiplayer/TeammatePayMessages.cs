using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Multiplayer;

public struct TeammatePayRequestMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public required int PurchaseId { get; set; }
    public required ulong RequesterNetId { get; set; }
    public required ulong TargetNetId { get; set; }
    public required int GoldAmount { get; set; }
    public required string EntryId { get; set; }
    public required string EntryName { get; set; }
    public required int EntryIndex { get; set; }
    public required TeammatePayEntryType EntryType { get; set; }
    public required RunLocation Location { get; set; }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    RunLocation IRunLocationTargetedMessage.Location => Location;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(PurchaseId);
        writer.WriteULong(RequesterNetId);
        writer.WriteULong(TargetNetId);
        writer.WriteInt(GoldAmount);
        writer.WriteString(EntryId);
        writer.WriteString(EntryName);
        writer.WriteInt(EntryIndex);
        writer.WriteEnum(EntryType);
        writer.Write(Location);
    }

    public void Deserialize(PacketReader reader)
    {
        PurchaseId = reader.ReadInt();
        RequesterNetId = reader.ReadULong();
        TargetNetId = reader.ReadULong();
        GoldAmount = reader.ReadInt();
        EntryId = reader.ReadString();
        EntryName = reader.ReadString();
        EntryIndex = reader.ReadInt();
        EntryType = reader.ReadEnum<TeammatePayEntryType>();
        Location = reader.Read<RunLocation>();
    }
}

public struct TeammatePayResponseMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public required int PurchaseId { get; set; }
    public required ulong RequesterNetId { get; set; }
    public required ulong ResponderNetId { get; set; }
    public required bool Accepted { get; set; }
    public required int GoldAmount { get; set; }
    public required string EntryId { get; set; }
    public required int EntryIndex { get; set; }
    public required TeammatePayEntryType EntryType { get; set; }
    public required RunLocation Location { get; set; }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    RunLocation IRunLocationTargetedMessage.Location => Location;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(PurchaseId);
        writer.WriteULong(RequesterNetId);
        writer.WriteULong(ResponderNetId);
        writer.WriteBool(Accepted);
        writer.WriteInt(GoldAmount);
        writer.WriteString(EntryId);
        writer.WriteInt(EntryIndex);
        writer.WriteEnum(EntryType);
        writer.Write(Location);
    }

    public void Deserialize(PacketReader reader)
    {
        PurchaseId = reader.ReadInt();
        RequesterNetId = reader.ReadULong();
        ResponderNetId = reader.ReadULong();
        Accepted = reader.ReadBool();
        GoldAmount = reader.ReadInt();
        EntryId = reader.ReadString();
        EntryIndex = reader.ReadInt();
        EntryType = reader.ReadEnum<TeammatePayEntryType>();
        Location = reader.Read<RunLocation>();
    }
}

public enum TeammatePayEntryType
{
    Card,
    Relic,
    Potion,
    CardRemoval
}

public struct TeammatePayGoldQueryMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public required ulong RequesterNetId { get; set; }
    public required RunLocation Location { get; set; }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    RunLocation IRunLocationTargetedMessage.Location => Location;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(RequesterNetId);
        writer.Write(Location);
    }

    public void Deserialize(PacketReader reader)
    {
        RequesterNetId = reader.ReadULong();
        Location = reader.Read<RunLocation>();
    }
}

public struct TeammatePayGoldResponseMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public required ulong ResponderNetId { get; set; }
    public required int GoldAmount { get; set; }
    public required RunLocation Location { get; set; }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    RunLocation IRunLocationTargetedMessage.Location => Location;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(ResponderNetId);
        writer.WriteInt(GoldAmount);
        writer.Write(Location);
    }

    public void Deserialize(PacketReader reader)
    {
        ResponderNetId = reader.ReadULong();
        GoldAmount = reader.ReadInt();
        Location = reader.Read<RunLocation>();
    }
}
