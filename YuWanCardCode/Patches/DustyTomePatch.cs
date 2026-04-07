using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(DustyTome))]
class DustyTomePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DustyTome.SetupForPlayer))]
    static bool Prefix(DustyTome __instance, Player player)
    {
        var ancientCards = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Ancient 
                && !ArchaicTooth.TranscendenceCards.Contains(c))
            .ToList();

        if (ancientCards.Count == 0)
        {
            var colorlessPool = MegaCrit.Sts2.Core.Models.ModelDb.CardPool<ColorlessCardPool>();
            ancientCards = colorlessPool
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity == MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Ancient 
                    && !ArchaicTooth.TranscendenceCards.Contains(c))
                .ToList();
        }

        if (ancientCards.Count > 0)
        {
            var selectedCard = player.PlayerRng.Rewards.NextItem(ancientCards);
            if (selectedCard != null)
            {
                __instance.AncientCard = selectedCard.Id;
            }
        }

        return false;
    }
}
