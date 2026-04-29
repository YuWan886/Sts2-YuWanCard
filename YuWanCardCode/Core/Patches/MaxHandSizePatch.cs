using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Provides max hand size calculation. 
/// </summary>
public static class MaxHandSizePatch
{
    public const int DefaultMaxHandSize = 10;

    public static int GetMaxHandSize(Player player) => DefaultMaxHandSize;
}
