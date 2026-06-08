using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Multiplayer;
using YuWanCard.Core.RightClick;

namespace YuWanCard.Core.Patches;

internal static class ManagedActionNetPatchHelpers
{
    private const int ReplayEventTypeBits = 3;
    private const int ReplayGameActionPlayerIdBits = 64;
    private const int ReplayGameActionPayloadOffsetBits = ReplayEventTypeBits + ReplayGameActionPlayerIdBits;

    private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, ActionQueueSet> ActionQueueSetRef =
        AccessTools.FieldRefAccess<ActionQueueSynchronizer, ActionQueueSet>("_actionQueueSet");

    private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, INetGameService> NetServiceRef =
        AccessTools.FieldRefAccess<ActionQueueSynchronizer, INetGameService>("_netService");

    private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, RunLocationTargetedMessageBuffer>
        MessageBufferRef =
            AccessTools.FieldRefAccess<ActionQueueSynchronizer, RunLocationTargetedMessageBuffer>("_messageBuffer");

    private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, List<GameAction>>
        RequestedActionsWaitingForPlayerTurnRef =
            AccessTools.FieldRefAccess<ActionQueueSynchronizer, List<GameAction>>(
                "_requestedActionsWaitingForPlayerTurn");

    public static bool TrySendManagedClientRequest(ActionQueueSynchronizer synchronizer, GameAction action)
    {
        YuWanRightClickManagedActions.EnsureRegistered();
        if (action.ActionType == GameActionType.CombatPlayPhaseOnly
            && synchronizer.CombatState == ActionSynchronizerCombatState.NotPlayPhase)
        {
            RequestedActionsWaitingForPlayerTurnRef(synchronizer).Add(action);
            return true;
        }

        if (NetServiceRef(synchronizer) is not NetClientGameService
                {
                    IsConnected: true,
                    NetClient: not null
                } client)
        {
            return false;
        }

        if (action.ToNetAction() is not YuWanManagedNetAction netAction)
        {
            return false;
        }

        var message = new RequestEnqueueActionMessage
        {
            action = netAction,
            location = MessageBufferRef(synchronizer).CurrentLocation
        };
        SendManagedActionRequest(client, message);
        return true;
    }

    public static bool TrySendManagedHostAnnouncement(
        ActionQueueSynchronizer synchronizer,
        GameAction action,
        ulong actionOwnerId)
    {
        YuWanRightClickManagedActions.EnsureRegistered();
        if (NetServiceRef(synchronizer) is not NetHostGameService { IsConnected: true, NetHost: not null } host)
        {
            return false;
        }

        if (action.ToNetAction() is not YuWanManagedNetAction netAction)
        {
            return false;
        }

        var message = new ActionEnqueuedMessage
        {
            playerId = actionOwnerId,
            location = MessageBufferRef(synchronizer).CurrentLocation,
            action = netAction
        };
        SendManagedActionAnnouncement(host, message);
        ActionQueueSetRef(synchronizer).EnqueueWithoutSynchronizing(action);
        return true;
    }

    private static void SendManagedActionRequest(NetClientGameService client, RequestEnqueueActionMessage message)
    {
        (byte[] bytes, int length) = SerializeManagedActionMessage(client.NetId, message);
        client.NetClient!.SendMessageToHost(bytes, length, message.Mode, message.Mode.ToChannelId());
    }

    private static void SendManagedActionAnnouncement(NetHostGameService host, ActionEnqueuedMessage message)
    {
        (byte[] bytes, int length) = SerializeManagedActionMessage(host.NetId, message);
        foreach (var peer in host.ConnectedPeers)
        {
            if (peer.readyForBroadcasting)
            {
                host.NetHost!.SendMessageToClient(peer.peerId, bytes, length, message.Mode, message.Mode.ToChannelId());
            }
        }
    }

    private static (byte[] Bytes, int Length) SerializeManagedActionMessage(
        ulong senderId,
        RequestEnqueueActionMessage message)
    {
        PacketWriter writer = CreateMessageWriter(senderId, message);
        writer.Write(message.location);
        YuWanManagedNetActions.TryWriteNetAction(writer, message.action);
        return (writer.Buffer, (int)Math.Ceiling(writer.BitPosition / 8f));
    }

    private static (byte[] Bytes, int Length) SerializeManagedActionMessage(
        ulong senderId,
        ActionEnqueuedMessage message)
    {
        PacketWriter writer = CreateMessageWriter(senderId, message);
        writer.WriteULong(message.playerId);
        writer.Write(message.location);
        YuWanManagedNetActions.TryWriteNetAction(writer, message.action);
        return (writer.Buffer, (int)Math.Ceiling(writer.BitPosition / 8f));
    }

    private static PacketWriter CreateMessageWriter(ulong senderId, INetMessage message)
    {
        var writer = new PacketWriter();
        writer.WriteByte((byte)message.ToId());
        writer.WriteULong(senderId);
        return writer;
    }

    public static PacketReader CreateProbeReader(PacketReader reader)
    {
        var probe = new PacketReader();
        probe.Reset(reader.Buffer);
        probe.BitPosition = reader.BitPosition;
        return probe;
    }

    public static bool ReplayEventPayloadIsManagedGameAction(PacketReader reader)
    {
        return YuWanManagedNetActions.TryPeekInt(reader, 0, ReplayEventTypeBits, out int eventType)
               && eventType == (int)CombatReplayEventType.GameAction
               && YuWanManagedNetActions.NextPayloadIsManagedAction(reader, ReplayGameActionPayloadOffsetBits);
    }
}

[HarmonyPatch(typeof(ActionQueueSynchronizer))]
public static class RightClickManagedActionRequestEnqueuePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ActionQueueSynchronizer.RequestEnqueue), [typeof(GameAction)])]
    public static bool Prefix(ActionQueueSynchronizer __instance, GameAction action)
    {
        return !ManagedActionNetPatchHelpers.TrySendManagedClientRequest(__instance, action);
    }
}

[HarmonyPatch(typeof(ActionQueueSynchronizer))]
public static class RightClickManagedActionEnqueuePatch
{
    [HarmonyPrefix]
    [HarmonyPatch("EnqueueAction", [typeof(GameAction), typeof(ulong)])]
    public static bool Prefix(ActionQueueSynchronizer __instance, GameAction action, ulong actionOwnerId)
    {
        return !ManagedActionNetPatchHelpers.TrySendManagedHostAnnouncement(__instance, action, actionOwnerId);
    }
}

[HarmonyPatch(typeof(RequestEnqueueActionMessage))]
public static class RightClickManagedActionRequestMessagePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(RequestEnqueueActionMessage.Serialize), [typeof(PacketWriter)])]
    public static bool SerializePrefix(RequestEnqueueActionMessage __instance, PacketWriter writer)
    {
        YuWanRightClickManagedActions.EnsureRegistered();
        if (__instance.action is not YuWanManagedNetAction)
        {
            return true;
        }

        writer.Write(__instance.location);
        YuWanManagedNetActions.TryWriteNetAction(writer, __instance.action);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(RequestEnqueueActionMessage.Deserialize), [typeof(PacketReader)])]
    public static bool DeserializePrefix(ref RequestEnqueueActionMessage __instance, PacketReader reader)
    {
        YuWanRightClickManagedActions.EnsureRegistered();
        PacketReader probe = ManagedActionNetPatchHelpers.CreateProbeReader(reader);
        RunLocation location = probe.Read<RunLocation>();
        if (!YuWanManagedNetActions.NextPayloadIsManagedAction(probe))
        {
            return true;
        }

        INetAction action = YuWanManagedNetActions.ReadNetAction(probe);
        __instance.location = location;
        __instance.action = action;
        reader.BitPosition = probe.BitPosition;
        return false;
    }
}

[HarmonyPatch(typeof(ActionEnqueuedMessage))]
public static class RightClickManagedActionAnnouncementMessagePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ActionEnqueuedMessage.Serialize), [typeof(PacketWriter)])]
    public static bool SerializePrefix(ActionEnqueuedMessage __instance, PacketWriter writer)
    {
        YuWanRightClickManagedActions.EnsureRegistered();
        if (__instance.action is not YuWanManagedNetAction)
        {
            return true;
        }

        writer.WriteULong(__instance.playerId);
        writer.Write(__instance.location);
        YuWanManagedNetActions.TryWriteNetAction(writer, __instance.action);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ActionEnqueuedMessage.Deserialize), [typeof(PacketReader)])]
    public static bool DeserializePrefix(ref ActionEnqueuedMessage __instance, PacketReader reader)
    {
        YuWanRightClickManagedActions.EnsureRegistered();
        PacketReader probe = ManagedActionNetPatchHelpers.CreateProbeReader(reader);
        ulong playerId = probe.ReadULong();
        RunLocation location = probe.Read<RunLocation>();
        if (!YuWanManagedNetActions.NextPayloadIsManagedAction(probe))
        {
            return true;
        }

        INetAction action = YuWanManagedNetActions.ReadNetAction(probe);
        __instance.playerId = playerId;
        __instance.location = location;
        __instance.action = action;
        reader.BitPosition = probe.BitPosition;
        return false;
    }
}

[HarmonyPatch(typeof(CombatReplayEvent))]
public static class RightClickManagedActionReplayPatch
{
    private const int ReplayEventTypeBits = 3;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CombatReplayEvent.Serialize), [typeof(PacketWriter)])]
    public static bool SerializePrefix(CombatReplayEvent __instance, PacketWriter writer)
    {
        YuWanRightClickManagedActions.EnsureRegistered();
        if (__instance.eventType != CombatReplayEventType.GameAction
            || __instance.action is not YuWanManagedNetAction)
        {
            return true;
        }

        writer.WriteInt((int)__instance.eventType, ReplayEventTypeBits);
        writer.WriteULong(__instance.playerId!.Value);
        YuWanManagedNetActions.TryWriteNetAction(writer, __instance.action);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CombatReplayEvent.Deserialize), [typeof(PacketReader)])]
    public static bool DeserializePrefix(ref CombatReplayEvent __instance, PacketReader reader)
    {
        YuWanRightClickManagedActions.EnsureRegistered();
        if (!ManagedActionNetPatchHelpers.ReplayEventPayloadIsManagedGameAction(reader))
        {
            return true;
        }

        __instance.eventType = (CombatReplayEventType)reader.ReadInt(ReplayEventTypeBits);
        __instance.playerId = reader.ReadULong();
        __instance.action = YuWanManagedNetActions.ReadNetAction(reader);
        return false;
    }
}
