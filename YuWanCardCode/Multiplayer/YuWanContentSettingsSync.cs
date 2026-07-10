using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Config;

namespace YuWanCard.Multiplayer;

public static class YuWanContentSettingsSync
{
    private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(1);

    private static INetGameService? _registeredNetService;
    private static DateTime _lastRequestUtc = DateTime.MinValue;
    private static int _hostSnapshotVersion;
    private static YuWanContentSettingsSnapshot? _hostSnapshot;
    private static int _clientSnapshotVersion = -1;
    private static YuWanContentSettingsSnapshot? _clientSnapshot;
    private static bool _hasLoggedAwaitingSnapshot;

    public static void Register(INetGameService? netService = null)
    {
        netService ??= RunManager.Instance?.NetService;
        if (netService == null)
        {
            return;
        }

        if (ReferenceEquals(_registeredNetService, netService))
        {
            return;
        }

        if (_registeredNetService != null)
        {
            Unregister(_registeredNetService);
        }

        netService.RegisterMessageHandler<YuWanContentSettingsRequestMessage>(HandleRequestMessage);
        netService.RegisterMessageHandler<YuWanContentSettingsSnapshotMessage>(HandleSnapshotMessage);
        _registeredNetService = netService;
        MainFile.Logger.Info("YuWanContentSettings: Message handlers registered");
    }

    public static void Unregister(INetGameService? netService = null)
    {
        netService ??= _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null)
        {
            ResetClientSnapshotState();
            _registeredNetService = null;
            return;
        }

        netService.UnregisterMessageHandler<YuWanContentSettingsRequestMessage>(HandleRequestMessage);
        netService.UnregisterMessageHandler<YuWanContentSettingsSnapshotMessage>(HandleSnapshotMessage);

        if (ReferenceEquals(_registeredNetService, netService))
        {
            _registeredNetService = null;
        }

        ResetClientSnapshotState();
    }

    public static void UpdateHost(NetHostGameService host)
    {
        if (!host.IsConnected)
        {
            if (ReferenceEquals(_registeredNetService, host))
            {
                Unregister(host);
            }

            return;
        }

        Register(host);
        BroadcastSnapshotIfChanged(host);
    }

    public static void UpdateClient(NetClientGameService client)
    {
        if (!client.IsConnected)
        {
            if (ReferenceEquals(_registeredNetService, client))
            {
                Unregister(client);
            }

            return;
        }

        Register(client);
        MaybeRequestSnapshot(client, force: false);
    }

    public static void ForceClientRequest(INetGameService? netService = null)
    {
        netService ??= _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService is not NetClientGameService client || !client.IsConnected)
        {
            return;
        }

        Register(client);
        MaybeRequestSnapshot(client, force: true);
    }

    public static bool TryGetClientAuthoritativeSnapshot(out YuWanContentSettingsSnapshot snapshot)
    {
        if (_registeredNetService is { Type: NetGameType.Client, IsConnected: true }
            && _clientSnapshot is { } authoritativeSnapshot)
        {
            snapshot = authoritativeSnapshot;
            return true;
        }

        snapshot = default;
        return false;
    }

    public static bool IsClientAwaitingAuthoritativeSnapshot()
    {
        return TryGetConnectedClient(out _)
            && _clientSnapshot == null;
    }

    public static void LogAwaitingAuthoritativeSnapshotUse(string contentKind, Type contentType)
    {
        if (_hasLoggedAwaitingSnapshot || !IsClientAwaitingAuthoritativeSnapshot())
        {
            return;
        }

        _hasLoggedAwaitingSnapshot = true;
        MainFile.Logger.Debug(
            $"YuWanContentSettings: Client awaiting host snapshot; suppressing local fallback for {contentKind} {contentType.Name}");
    }

    private static void MaybeRequestSnapshot(NetClientGameService client, bool force)
    {
        if (_clientSnapshot != null)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        if (!force && now - _lastRequestUtc < RequestInterval)
        {
            return;
        }

        var request = new YuWanContentSettingsRequestMessage();
        client.SendMessage(request);
        _lastRequestUtc = now;
    }

    private static void HandleRequestMessage(YuWanContentSettingsRequestMessage message, ulong senderId)
    {
        if (_registeredNetService is not { Type: NetGameType.Host, IsConnected: true } hostNetService)
        {
            return;
        }

        YuWanContentSettingsSnapshot snapshot = YuWanContentSettingsSnapshot.CaptureLocal();
        if (_hostSnapshot is not { } previous || !previous.ContentEquals(snapshot))
        {
            _hostSnapshot = snapshot;
            _hostSnapshotVersion++;
        }

        hostNetService.SendMessage(CreateSnapshotMessage(snapshot, _hostSnapshotVersion), senderId);
    }

    private static void HandleSnapshotMessage(YuWanContentSettingsSnapshotMessage message, ulong senderId)
    {
        if (_registeredNetService is not { Type: NetGameType.Client, IsConnected: true })
        {
            return;
        }

        if (message.Version < _clientSnapshotVersion)
        {
            return;
        }

        _clientSnapshotVersion = message.Version;
        _clientSnapshot = message.ToSnapshot();
        _hasLoggedAwaitingSnapshot = false;
        MainFile.Logger.Debug($"YuWanContentSettings: Received authoritative snapshot v{message.Version} from {senderId}");
    }

    private static void ResetClientSnapshotState()
    {
        _lastRequestUtc = DateTime.MinValue;
        _clientSnapshotVersion = -1;
        _clientSnapshot = null;
        _hasLoggedAwaitingSnapshot = false;
    }

    private static bool TryGetConnectedClient(out NetClientGameService? client)
    {
        INetGameService? netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService is NetClientGameService connectedClient && connectedClient.IsConnected)
        {
            client = connectedClient;
            return true;
        }

        client = null;
        return false;
    }

    private static void BroadcastSnapshotIfChanged(NetHostGameService host)
    {
        YuWanContentSettingsSnapshot snapshot = YuWanContentSettingsSnapshot.CaptureLocal();
        if (_hostSnapshot is { } previous && previous.ContentEquals(snapshot))
        {
            return;
        }

        _hostSnapshot = snapshot;
        _hostSnapshotVersion++;
        host.SendMessage(CreateSnapshotMessage(snapshot, _hostSnapshotVersion));
    }

    private static YuWanContentSettingsSnapshotMessage CreateSnapshotMessage(
        YuWanContentSettingsSnapshot snapshot,
        int version)
    {
        return new YuWanContentSettingsSnapshotMessage
        {
            Version = version,
            EnablePigRewardAllCardPools = snapshot.EnablePigRewardAllCardPools,
            EnableYuWanEnemyEncounters = snapshot.EnableYuWanEnemyEncounters,
            EnableIgnisBossEncounter = snapshot.EnableIgnisBossEncounter,
            EnableKillerEliteEncounter = snapshot.EnableKillerEliteEncounter,
            EnableFerrousWroughtnautEliteEncounter = snapshot.EnableFerrousWroughtnautEliteEncounter,
            EnableYuWanEvents = snapshot.EnableYuWanEvents,
            EnablePigPigAncient = snapshot.EnablePigPigAncient,
            EnabledEvents = YuWanEventCatalog.Events
                .Select(definition => new YuWanEventState
                {
                    Key = definition.Key,
                    Enabled = snapshot.EnabledEvents.GetValueOrDefault(definition.Key, true)
                })
                .ToList(),
            EnabledColorlessCards = YuWanColorlessCardCatalog.Cards
                .Select(definition => new YuWanColorlessCardState
                {
                    Key = definition.Key,
                    Enabled = snapshot.EnabledColorlessCards.GetValueOrDefault(definition.Key, true)
                })
                .ToList()
        };
    }
}

public struct YuWanContentSettingsRequestMessage : INetMessage, IPacketSerializable
{
    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
    }

    public void Deserialize(PacketReader reader)
    {
    }
}

public struct YuWanContentSettingsSnapshotMessage : INetMessage, IPacketSerializable
{
    public required int Version { get; set; }
    public required bool EnablePigRewardAllCardPools { get; set; }
    public required bool EnableYuWanEnemyEncounters { get; set; }
    public required bool EnableIgnisBossEncounter { get; set; }
    public required bool EnableKillerEliteEncounter { get; set; }
    public required bool EnableFerrousWroughtnautEliteEncounter { get; set; }
    public required bool EnableYuWanEvents { get; set; }
    public required bool EnablePigPigAncient { get; set; }
    public required List<YuWanEventState> EnabledEvents { get; set; }
    public required List<YuWanColorlessCardState> EnabledColorlessCards { get; set; }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(Version);
        writer.WriteBool(EnablePigRewardAllCardPools);
        writer.WriteBool(EnableYuWanEnemyEncounters);
        writer.WriteBool(EnableIgnisBossEncounter);
        writer.WriteBool(EnableKillerEliteEncounter);
        writer.WriteBool(EnableFerrousWroughtnautEliteEncounter);
        writer.WriteBool(EnableYuWanEvents);
        writer.WriteBool(EnablePigPigAncient);
        writer.WriteInt(EnabledEvents.Count);
        foreach (var entry in EnabledEvents)
        {
            writer.WriteString(entry.Key);
            writer.WriteBool(entry.Enabled);
        }
        writer.WriteInt(EnabledColorlessCards.Count);
        foreach (var entry in EnabledColorlessCards)
        {
            writer.WriteString(entry.Key);
            writer.WriteBool(entry.Enabled);
        }
    }

    public void Deserialize(PacketReader reader)
    {
        Version = reader.ReadInt();
        EnablePigRewardAllCardPools = reader.ReadBool();
        EnableYuWanEnemyEncounters = reader.ReadBool();
        EnableIgnisBossEncounter = reader.ReadBool();
        EnableKillerEliteEncounter = reader.ReadBool();
        EnableFerrousWroughtnautEliteEncounter = reader.ReadBool();
        EnableYuWanEvents = reader.ReadBool();
        EnablePigPigAncient = reader.ReadBool();
        int eventCount = reader.ReadInt();
        EnabledEvents = new List<YuWanEventState>(eventCount);
        for (int i = 0; i < eventCount; i++)
        {
            EnabledEvents.Add(new YuWanEventState
            {
                Key = reader.ReadString(),
                Enabled = reader.ReadBool()
            });
        }

        int colorlessCount = reader.ReadInt();
        EnabledColorlessCards = new List<YuWanColorlessCardState>(colorlessCount);
        for (int i = 0; i < colorlessCount; i++)
        {
            EnabledColorlessCards.Add(new YuWanColorlessCardState
            {
                Key = reader.ReadString(),
                Enabled = reader.ReadBool()
            });
        }
    }

    public YuWanContentSettingsSnapshot ToSnapshot()
    {
        return new YuWanContentSettingsSnapshot(
            EnablePigRewardAllCardPools,
            EnableYuWanEnemyEncounters,
            EnableIgnisBossEncounter,
            EnableKillerEliteEncounter,
            EnableFerrousWroughtnautEliteEncounter,
            EnableYuWanEvents,
            EnablePigPigAncient,
            EnabledEvents.ToDictionary(static entry => entry.Key, static entry => entry.Enabled,
                StringComparer.Ordinal),
            EnabledColorlessCards.ToDictionary(static entry => entry.Key, static entry => entry.Enabled,
                StringComparer.Ordinal));
    }
}

public struct YuWanEventState
{
    public required string Key { get; set; }
    public required bool Enabled { get; set; }
}

public struct YuWanColorlessCardState
{
    public required string Key { get; set; }
    public required bool Enabled { get; set; }
}
