using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Core.Multiplayer;

internal static class SavedPropertyMultiplayerSync
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Type, MethodInfo?> AfterDeserializedCache = [];
    private static readonly MethodInfo? RelicDisplayRefreshMethod =
        AccessTools.Method(typeof(RelicModel), "InvokeDisplayAmountChanged");
    private static int _suppressionDepth;

    public static void NotifyPotentialStateChange(AbstractModel model)
    {
        if (_suppressionDepth > 0 || !SavedPropertySyncRegistry.IsRegisteredModel(model))
        {
            return;
        }

        if (!TryGetOwner(model, out Player owner) || !LocalContext.IsMe(owner))
        {
            return;
        }

        EnsureIdentity(model);
        if (!MultiplayerModelIdentityRegistry.TryGetToken(model, out MultiplayerModelIdentityToken token))
        {
            return;
        }

        SavedProperties? properties = SavedProperties.From(model);
        if (properties == null)
        {
            return;
        }

        SavedPropertySyncMessageHandler.SendState(owner, token, properties);
    }

    public static void ApplyRemoteState(SavedPropertySyncMessage message)
    {
        if (!MultiplayerModelIdentityRegistry.TryResolve(message.ModelToken, out AbstractModel model))
        {
            MainFile.Logger.Warn(
                $"SavedPropertySync: failed to resolve token {message.ModelToken.Identity.Value} for {message.ModelToken.ModelId}.");
            return;
        }

        using (Suppress())
        {
            message.Properties.FillInternal(model);
            InvokeAfterDeserialized(model);
            RefreshModelDisplay(model);
        }
    }

    public static void BeginSavedPropertiesFill(object model)
    {
        if (model is AbstractModel abstractModel && SavedPropertySyncRegistry.IsRegisteredModel(abstractModel))
        {
            EnterSuppression();
        }
    }

    public static void EndSavedPropertiesFill(object model)
    {
        if (model is AbstractModel abstractModel && SavedPropertySyncRegistry.IsRegisteredModel(abstractModel))
        {
            ExitSuppression();
        }
    }

    private static void EnsureIdentity(AbstractModel model)
    {
        switch (model)
        {
            case MegaCrit.Sts2.Core.Models.CardModel card:
                MultiplayerModelIdentityRegistry.RegisterCardTree(card);
                break;
            case RelicModel relic:
                MultiplayerModelIdentityRegistry.EnsureRegistered(relic);
                break;
        }
    }

    private static bool TryGetOwner(AbstractModel model, out Player owner)
    {
        owner = (model switch
        {
            MegaCrit.Sts2.Core.Models.CardModel card => card.Owner,
            RelicModel relic => relic.Owner,
            _ => null
        })!;

        if (owner == null)
        {
            return false;
        }

        var netService = RunManager.Instance?.NetService;
        return netService is { IsConnected: true }
               && netService.Type is not MegaCrit.Sts2.Core.Multiplayer.Game.NetGameType.Singleplayer
               and not MegaCrit.Sts2.Core.Multiplayer.Game.NetGameType.Replay;
    }

    private static void InvokeAfterDeserialized(AbstractModel model)
    {
        MethodInfo? method;
        lock (Gate)
        {
            if (!AfterDeserializedCache.TryGetValue(model.GetType(), out method))
            {
                method = AccessTools.Method(model.GetType(), "AfterDeserialized");
                AfterDeserializedCache[model.GetType()] = method;
            }
        }

        method?.Invoke(model, null);
    }

    private static void RefreshModelDisplay(AbstractModel model)
    {
        if (model is RelicModel)
        {
            RelicDisplayRefreshMethod?.Invoke(model, null);
        }
    }

    private static IDisposable Suppress()
    {
        EnterSuppression();
        return new SuppressionScope();
    }

    private static void EnterSuppression()
    {
        lock (Gate)
        {
            _suppressionDepth++;
        }
    }

    private static void ExitSuppression()
    {
        lock (Gate)
        {
            if (_suppressionDepth > 0)
            {
                _suppressionDepth--;
            }
        }
    }

    private sealed class SuppressionScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ExitSuppression();
        }
    }
}
