using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Core.Multiplayer;

public static class SavedPropertySyncMessageHandler
{
    private static RunLocationTargetedMessageBuffer? _registeredBuffer;

    public static void Register(INetGameService? _ = null)
    {
        var buffer = RunManager.Instance?.RunLocationTargetedBuffer;
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

        buffer.RegisterMessageHandler<SavedPropertySyncMessage>(HandleMessage);
        _registeredBuffer = buffer;
    }

    public static void Unregister(INetGameService? _ = null)
    {
        var buffer = _registeredBuffer ?? RunManager.Instance?.RunLocationTargetedBuffer;
        if (buffer == null)
        {
            return;
        }

        buffer.UnregisterMessageHandler<SavedPropertySyncMessage>(HandleMessage);
        if (ReferenceEquals(_registeredBuffer, buffer))
        {
            _registeredBuffer = null;
        }
    }

    public static void SendState(Player owner, MultiplayerModelIdentityToken modelToken, SavedProperties properties)
    {
        var netService = RunManager.Instance?.NetService;
        var locationBuffer = RunManager.Instance?.RunLocationTargetedBuffer;
        if (netService == null || locationBuffer == null || !netService.IsConnected)
        {
            return;
        }

        if (netService.Type is NetGameType.Singleplayer or NetGameType.Replay)
        {
            return;
        }

        netService.SendMessage(new SavedPropertySyncMessage
        {
            OwnerNetId = owner.NetId,
            ModelToken = modelToken,
            Properties = properties,
            Location = locationBuffer.CurrentLocation
        });
    }

    private static void HandleMessage(SavedPropertySyncMessage message, ulong _senderId)
    {
        if (LocalContext.NetId == message.OwnerNetId)
        {
            return;
        }

        SavedPropertyMultiplayerSync.ApplyRemoteState(message);
    }
}
