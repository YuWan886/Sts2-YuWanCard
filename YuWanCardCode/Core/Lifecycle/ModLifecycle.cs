namespace YuWanCard.Core.Lifecycle;

/// <summary>
/// Ordered lifecycle phases during mod initialization.
/// </summary>
public enum ModLifecyclePhase
{
    Initializing,
    PatchesApplied,
    ContentRegistering,
    ContentRegistered,
    ContentFrozen,
    ModelDbReady,
    Initialized
}

/// <summary>
/// Lightweight lifecycle event bus for initialization ordering.
/// Subscribers receive events in registration order. Late subscribers
/// (after a phase has published) are invoked immediately.
/// </summary>
public static class ModLifecycle
{
    private static readonly Dictionary<ModLifecyclePhase, List<Action>> _subscribers = [];
    private static readonly HashSet<ModLifecyclePhase> _publishedPhases = [];
    private static readonly Lock _lock = new();

    public static ModLifecyclePhase CurrentPhase { get; private set; }

    public static void On(ModLifecyclePhase phase, Action callback)
    {
        lock (_lock)
        {
            if (_publishedPhases.Contains(phase))
            {
                callback();
                return;
            }

            if (!_subscribers.ContainsKey(phase))
                _subscribers[phase] = [];
            _subscribers[phase].Add(callback);
        }
    }

    public static void Publish(ModLifecyclePhase phase)
    {
        MainFile.Logger.Debug($"[Lifecycle] {phase}");

        List<Action>? callbacks;
        lock (_lock)
        {
            CurrentPhase = phase;
            _publishedPhases.Add(phase);
            _subscribers.TryGetValue(phase, out callbacks);
            _subscribers.Remove(phase);
        }

        if (callbacks != null)
            foreach (var cb in callbacks)
                SafeInvoke(cb, phase);
    }

    private static void SafeInvoke(Action callback, ModLifecyclePhase phase)
    {
        try { callback(); }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[Lifecycle] Subscriber error during {phase}: {ex.Message}");
        }
    }
}
