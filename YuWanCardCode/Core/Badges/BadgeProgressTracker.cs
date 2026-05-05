using System.Collections.Concurrent;

namespace YuWanCard.Core.Badges;

/// <summary>
/// Per-run storage for custom badge progress.
/// Tracks cumulative counters keyed by (playerNetId, badgeId).
/// Reset at the start of each new run.
/// </summary>
public static class BadgeProgressTracker
{
    private static readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, int>> _progress = new();

    public static void AddProgress(ulong playerId, string badgeId, int amount)
    {
        if (amount <= 0) return;
        var playerProgress = _progress.GetOrAdd(playerId, _ => new ConcurrentDictionary<string, int>());
        playerProgress.AddOrUpdate(badgeId, amount, (_, existing) => existing + amount);
    }

    public static int GetProgress(ulong playerId, string badgeId)
    {
        if (_progress.TryGetValue(playerId, out var playerProgress)
            && playerProgress.TryGetValue(badgeId, out var value))
        {
            return value;
        }
        return 0;
    }

    public static void ResetProgress(ulong playerId)
    {
        _progress.TryRemove(playerId, out _);
    }

    public static void ResetAll()
    {
        _progress.Clear();
    }
}
