using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Multiplayer;

public static class TeammatePayMessageHandler
{
    private static bool _isRegistered = false;
    private static TeammatePayRequestMessage? _pendingRequest = null;
    private static DateTime _requestTime = DateTime.MinValue;
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(30);
    private static readonly Dictionary<ulong, int> _teammateGoldCache = new();
    private static readonly Dictionary<ulong, TaskCompletionSource<int>> _goldQueryTasks = new();

    public static event Action<TeammatePayRequestMessage>? OnRequestReceived;
    public static event Action<TeammatePayResponseMessage>? OnResponseReceived;
    public static event Action<TeammatePayGoldResponseMessage>? OnGoldResponseReceived;

    public static void Register()
    {
        if (_isRegistered) return;
        
        var netService = RunManager.Instance?.NetService;
        if (netService == null)
        {
            return;
        }

        netService.RegisterMessageHandler<TeammatePayRequestMessage>(HandleRequest);
        netService.RegisterMessageHandler<TeammatePayResponseMessage>(HandleResponse);
        netService.RegisterMessageHandler<TeammatePayGoldQueryMessage>(HandleGoldQuery);
        netService.RegisterMessageHandler<TeammatePayGoldResponseMessage>(HandleGoldResponse);
        _isRegistered = true;
        MainFile.Logger.Info("TeammatePay: Message handlers registered");
    }

    public static bool IsRegistered => _isRegistered;

    public static void Unregister()
    {
        if (!_isRegistered) return;
        
        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;

        netService.UnregisterMessageHandler<TeammatePayRequestMessage>(HandleRequest);
        netService.UnregisterMessageHandler<TeammatePayResponseMessage>(HandleResponse);
        netService.UnregisterMessageHandler<TeammatePayGoldQueryMessage>(HandleGoldQuery);
        netService.UnregisterMessageHandler<TeammatePayGoldResponseMessage>(HandleGoldResponse);
        _isRegistered = false;
    }

    public static void SendRequest(TeammatePayRequestMessage request)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null || !netService.IsConnected)
        {
            MainFile.Logger.Warn("TeammatePay: Cannot send request - not connected");
            return;
        }

        netService.SendMessage(request, request.TargetNetId);
        MainFile.Logger.Debug($"TeammatePay: Request sent to {request.TargetNetId}");
    }

    public static void SendResponse(TeammatePayResponseMessage response)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null || !netService.IsConnected)
        {
            MainFile.Logger.Warn("TeammatePay: Cannot send response - not connected");
            return;
        }

        netService.SendMessage(response, response.RequesterNetId);
        MainFile.Logger.Debug($"TeammatePay: Response sent to {response.RequesterNetId}, accepted: {response.Accepted}");
        
        ClearPendingRequest();
    }

    private static void HandleRequest(TeammatePayRequestMessage message, ulong senderId)
    {
        MainFile.Logger.Debug($"TeammatePay: Request received from {senderId}, pending: {_pendingRequest.HasValue}");
        
        if (_pendingRequest.HasValue && DateTime.Now - _requestTime < _requestTimeout)
        {
            MainFile.Logger.Debug("TeammatePay: Already have pending request, rejecting new one");
            SendResponse(new TeammatePayResponseMessage
            {
                PurchaseId = message.PurchaseId,
                RequesterNetId = message.RequesterNetId,
                ResponderNetId = LocalContext.NetId ?? 0,
                Accepted = false,
                GoldAmount = 0,
                EntryId = "",
                EntryIndex = -1,
                EntryType = message.EntryType,
                Location = message.Location
            });
            return;
        }

        _pendingRequest = message;
        _requestTime = DateTime.Now;
        MainFile.Logger.Debug($"TeammatePay: Invoking OnRequestReceived event");
        OnRequestReceived?.Invoke(message);
    }

    private static void HandleResponse(TeammatePayResponseMessage message, ulong senderId)
    {
        MainFile.Logger.Debug($"TeammatePay: Response received from {senderId}, accepted: {message.Accepted}");
        OnResponseReceived?.Invoke(message);
    }

    public static TeammatePayRequestMessage? GetPendingRequest()
    {
        if (_pendingRequest.HasValue && DateTime.Now - _requestTime < _requestTimeout)
        {
            return _pendingRequest.Value;
        }
        _pendingRequest = null;
        return null;
    }

    public static void ClearPendingRequest()
    {
        _pendingRequest = null;
        MainFile.Logger.Debug("TeammatePay: Pending request cleared");
    }

    public static async Task<bool> DeductGoldLocally(int goldAmount)
    {
        var localPlayer = LocalContext.GetMe(RunManager.Instance?.State);
        if (localPlayer == null)
        {
            MainFile.Logger.Warn("TeammatePay: No local player found");
            return false;
        }

        if (localPlayer.Gold < goldAmount)
        {
            MainFile.Logger.Warn($"TeammatePay: Not enough gold. Has: {localPlayer.Gold}, Needs: {goldAmount}");
            return false;
        }

        await PlayerCmd.LoseGold(goldAmount, localPlayer);
        MainFile.Logger.Info($"TeammatePay: Deducted {goldAmount} gold from local player");
        return true;
    }

    public static void QueryTeammateGold(ulong targetNetId)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null || !netService.IsConnected)
        {
            MainFile.Logger.Warn("TeammatePay: Cannot query gold - not connected");
            return;
        }

        var localPlayer = LocalContext.GetMe(RunManager.Instance?.State);
        if (localPlayer == null) return;

        var query = new TeammatePayGoldQueryMessage
        {
            RequesterNetId = localPlayer.NetId,
            Location = RunManager.Instance!.RunLocationTargetedBuffer.CurrentLocation
        };

        netService.SendMessage(query, targetNetId);
        MainFile.Logger.Debug($"TeammatePay: Gold query sent to {targetNetId}");
    }

    public static async Task<Dictionary<ulong, int>> QueryAllTeammatesGold()
    {
        var result = new Dictionary<ulong, int>();
        var runState = RunManager.Instance?.State;
        var localPlayer = LocalContext.GetMe(runState);
        
        if (runState == null || localPlayer == null) return result;

        var netService = RunManager.Instance?.NetService;
        if (netService == null || !netService.IsConnected) return result;

        var teammates = new List<ulong>();
        foreach (var player in runState.Players)
        {
            if (player.NetId != localPlayer.NetId)
            {
                teammates.Add(player.NetId);
            }
        }

        if (teammates.Count == 0) return result;

        var tcs = new TaskCompletionSource<bool>();
        var remainingResponses = teammates.Count;
        var timeout = Task.Delay(2000);

        void OnResponse(TeammatePayGoldResponseMessage msg)
        {
            result[msg.ResponderNetId] = msg.GoldAmount;
            _teammateGoldCache[msg.ResponderNetId] = msg.GoldAmount;
            
            if (System.Threading.Interlocked.Decrement(ref remainingResponses) == 0)
            {
                tcs.TrySetResult(true);
            }
        }

        OnGoldResponseReceived += OnResponse;

        try
        {
            var query = new TeammatePayGoldQueryMessage
            {
                RequesterNetId = localPlayer.NetId,
                Location = RunManager.Instance!.RunLocationTargetedBuffer.CurrentLocation
            };

            foreach (var teammateId in teammates)
            {
                netService.SendMessage(query, teammateId);
            }

            MainFile.Logger.Debug($"TeammatePay: Gold query sent to {teammates.Count} teammates");

            await Task.WhenAny(tcs.Task, timeout);

            foreach (var teammateId in teammates)
            {
                if (!result.ContainsKey(teammateId) && _teammateGoldCache.TryGetValue(teammateId, out var cachedGold))
                {
                    result[teammateId] = cachedGold;
                }
            }
        }
        finally
        {
            OnGoldResponseReceived -= OnResponse;
        }

        return result;
    }

    private static void HandleGoldQuery(TeammatePayGoldQueryMessage message, ulong senderId)
    {
        var localPlayer = LocalContext.GetMe(RunManager.Instance?.State);
        if (localPlayer == null) return;

        MainFile.Logger.Debug($"TeammatePay: Gold query received from {senderId}, my gold: {localPlayer.Gold}");

        var response = new TeammatePayGoldResponseMessage
        {
            ResponderNetId = localPlayer.NetId,
            GoldAmount = localPlayer.Gold,
            Location = message.Location
        };

        var netService = RunManager.Instance?.NetService;
        if (netService != null && netService.IsConnected)
        {
            netService.SendMessage(response, senderId);
            MainFile.Logger.Debug($"TeammatePay: Gold response sent to {senderId}");
        }
    }

    private static void HandleGoldResponse(TeammatePayGoldResponseMessage message, ulong senderId)
    {
        MainFile.Logger.Debug($"TeammatePay: Gold response received from {senderId}, gold: {message.GoldAmount}");
        _teammateGoldCache[message.ResponderNetId] = message.GoldAmount;
        OnGoldResponseReceived?.Invoke(message);
    }

    public static int GetCachedGold(ulong netId)
    {
        return _teammateGoldCache.TryGetValue(netId, out var gold) ? gold : -1;
    }

    public static void ClearGoldCache()
    {
        _teammateGoldCache.Clear();
    }
}
