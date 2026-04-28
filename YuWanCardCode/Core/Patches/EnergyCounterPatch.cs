using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YuWanCard.Core.Utils;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Ensures custom energy counter scenes are properly type-converted
/// by using our NodeFactory instead of direct Instantiate.
/// </summary>
[HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter.Create))]
static class EnergyCounterPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    static bool CreateFromFactory(Player player, ref NEnergyCounter? __result)
    {
        var path = player.Character.EnergyCounterPath;
        if (path == null || !path.Contains("YuWanCard"))
            return true; // use vanilla path

        __result = NodeFactory.CreateFromScene<NEnergyCounter>(path);
        if (__result != null)
        {
            // Set the player reference (normally done by NEnergyCounter.Create)
            var playerField = AccessTools.Field(typeof(NEnergyCounter), "_player");
            playerField?.SetValue(__result, player);
        }
        return false; // skip original
    }
}
