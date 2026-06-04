using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Core.RightClick;

public struct YuWanRightClickSyncMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public required ulong OwnerNetId { get; set; }
    public required YuWanRightClickModelKind Kind { get; set; }
    public required MultiplayerModelIdentityToken ModelToken { get; set; }
    public required YuWanRightClickTrigger Trigger { get; set; }
    public required List<YuWanRightClickBindingId> BindingIds { get; set; }
    public required RunLocation Location { get; set; }

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    RunLocation IRunLocationTargetedMessage.Location => Location;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(OwnerNetId);
        writer.WriteEnum(Kind);
        writer.WriteInt(ModelToken.Identity.Value);
        writer.WriteFullModelId(ModelToken.ModelId);
        writer.WriteBool(Trigger.IsController);
        writer.WriteBool(Trigger.Metadata != null);
        if (Trigger.Metadata != null)
        {
            writer.WriteString(Trigger.Metadata);
        }

        writer.WriteInt(BindingIds.Count);
        foreach (YuWanRightClickBindingId bindingId in BindingIds)
        {
            writer.WriteString(bindingId.Id);
        }

        writer.Write(Location);
    }

    public void Deserialize(PacketReader reader)
    {
        OwnerNetId = reader.ReadULong();
        Kind = reader.ReadEnum<YuWanRightClickModelKind>();
        ModelToken = new MultiplayerModelIdentityToken(
            new MultiplayerModelIdentity(reader.ReadInt()),
            reader.ReadFullModelId());

        bool isController = reader.ReadBool();
        string? metadata = reader.ReadBool() ? reader.ReadString() : null;
        Trigger = new YuWanRightClickTrigger(isController, metadata);

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

        BindingIds = bindingIds;
        Location = reader.Read<RunLocation>();
    }
}

public static class YuWanRightClickMessageHandler
{
    private static RunLocationTargetedMessageBuffer? _registeredBuffer;

    public static void Register(INetGameService? _ = null)
    {
        RunLocationTargetedMessageBuffer? buffer = RunManager.Instance?.RunLocationTargetedBuffer;
        if (buffer == null)
        {
            return;
        }

        if (ReferenceEquals(_registeredBuffer, buffer))
        {
            return;
        }

        if (_registeredBuffer != null)
        {
            Unregister();
        }

        buffer.RegisterMessageHandler<YuWanRightClickSyncMessage>(HandleMessage);
        _registeredBuffer = buffer;
    }

    public static void Unregister(INetGameService? _ = null)
    {
        RunLocationTargetedMessageBuffer? buffer = _registeredBuffer ?? RunManager.Instance?.RunLocationTargetedBuffer;
        if (buffer == null)
        {
            return;
        }

        buffer.UnregisterMessageHandler<YuWanRightClickSyncMessage>(HandleMessage);
        if (ReferenceEquals(_registeredBuffer, buffer))
        {
            _registeredBuffer = null;
        }
    }

    public static void Send(YuWanRightClickSyncMessage message)
    {
        INetGameService? netService = RunManager.Instance?.NetService;
        if (netService == null || !netService.IsConnected)
        {
            return;
        }

        if (netService.Type is NetGameType.Singleplayer or NetGameType.Replay)
        {
            return;
        }

        netService.SendMessage(message);
    }

    private static void HandleMessage(YuWanRightClickSyncMessage message, ulong senderId)
    {
        if (LocalContext.NetId == senderId)
        {
            return;
        }

        YuWanRightClickRegistry.HandleRemoteMessage(message);
    }
}
