using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Core.Multiplayer;

public struct SavedPropertySyncMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public required ulong OwnerNetId { get; set; }
    public required MultiplayerModelIdentityToken ModelToken { get; set; }
    public required SavedProperties Properties { get; set; }
    public required RunLocation Location { get; set; }

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;

    RunLocation IRunLocationTargetedMessage.Location => Location;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(OwnerNetId);
        writer.WriteInt(ModelToken.Identity.Value);
        writer.WriteFullModelId(ModelToken.ModelId);
        writer.Write(Properties);
        writer.Write(Location);
    }

    public void Deserialize(PacketReader reader)
    {
        OwnerNetId = reader.ReadULong();
        ModelToken = new MultiplayerModelIdentityToken(
            new MultiplayerModelIdentity(reader.ReadInt()),
            reader.ReadFullModelId());
        Properties = reader.Read<SavedProperties>();
        Location = reader.Read<RunLocation>();
    }
}
