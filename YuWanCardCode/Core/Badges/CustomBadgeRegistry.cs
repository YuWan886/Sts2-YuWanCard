using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;

namespace YuWanCard.Core.Badges;

/// <summary>
/// Registry for custom badge factories. Register factory functions here,
/// and they will be injected into BadgePool.CreateAll via BadgePoolPatch.
/// </summary>
public static class CustomBadgeRegistry
{
    private static readonly List<Func<SerializableRun, ulong, bool, Badge>> _factories = [];

    public static IReadOnlyList<Func<SerializableRun, ulong, bool, Badge>> Factories => _factories;

    public static void Register(Func<SerializableRun, ulong, bool, Badge> factory)
    {
        _factories.Add(factory);
    }

    public static List<Badge> CreateAll(SerializableRun run, ulong playerId, bool won)
    {
        var badges = new List<Badge>(_factories.Count);
        foreach (var factory in _factories)
        {
            try
            {
                badges.Add(factory(run, playerId, won));
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn($"[CustomBadgeRegistry] Factory failed: {ex.Message}");
            }
        }
        return badges;
    }
}
