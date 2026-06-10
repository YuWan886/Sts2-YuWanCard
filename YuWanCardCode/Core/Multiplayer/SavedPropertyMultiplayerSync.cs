using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Multiplayer;

internal static class SavedPropertyMultiplayerSync
{
    private static readonly object Gate = new();
    private static int _suppressionDepth;

    public static void BeginSavedPropertiesFill(object model)
    {
        if (model is AbstractModel)
        {
            EnterSuppression();
        }
    }

    public static void EndSavedPropertiesFill(object model)
    {
        if (model is AbstractModel)
        {
            ExitSuppression();
        }
    }

    internal static IDisposable SuppressNotifications()
    {
        return Suppress();
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
