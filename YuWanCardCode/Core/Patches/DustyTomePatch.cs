using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;
using YuWanCard.Core.Transcendence;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(DustyTome))]
public static class DustyTomePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DustyTome.SetupForPlayer))]
    public static bool PrefixSetupForPlayer(DustyTome __instance, Player player)
    {
        if (!TranscendenceRegistry.TryGetDustyTomeAncientCard(player, out var ancientCard))
        {
            return true;
        }

        __instance.AncientCard = ancientCard.Id;
        return false;
    }
}
