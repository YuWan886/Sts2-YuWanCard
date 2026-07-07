using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using YuWanCard.Core.Transcendence;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(ArchaicTooth))]
public static class ArchaicToothPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("GetTranscendenceStarterCard")]
    public static void PostfixGetTranscendenceStarterCard(Player player, ref CardModel? __result)
    {
        if (__result != null)
        {
            return;
        }

        __result = player.Deck.Cards.FirstOrDefault(TranscendenceRegistry.IsStarterCard);
    }

    [HarmonyPrefix]
    [HarmonyPatch("GetTranscendenceTransformedCard")]
    public static bool PrefixGetTranscendenceTransformedCard(CardModel starterCard, ref CardModel __result)
    {
        try
        {
            var transformedCard = TranscendenceRegistry.CreateTransformedCard(starterCard);
            if (transformedCard == null)
            {
                return true;
            }

            __result = transformedCard;
            return false;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[ArchaicTooth] Failed to transform card {starterCard.Id.Entry}: {ex.Message}");
            return true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("TranscendenceCards", MethodType.Getter)]
    public static void PostfixTranscendenceCards(ref List<CardModel> __result)
    {
        foreach (var card in TranscendenceRegistry.GetRegisteredAncientCards())
        {
            if (__result.Exists(existing => existing.Id == card.Id))
            {
                continue;
            }

            __result.Add(card);
        }
    }
}
