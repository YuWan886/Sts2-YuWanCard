using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;
using YuWanCard.Core.Transcendence;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(ArchaicTooth))]
public static class ArchaicToothPatch
{
    private static readonly FieldInfo StarterCardField = AccessTools.Field(typeof(ArchaicTooth), "_serializableStarterCard");
    private static readonly FieldInfo AncientCardField = AccessTools.Field(typeof(ArchaicTooth), "_serializableAncientCard");
    private static readonly MethodInfo UpdateHoverTipsMethod = AccessTools.Method(typeof(ArchaicTooth), "UpdateHoverTips");

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ArchaicTooth.SetupForPlayer))]
    public static bool PrefixSetupForPlayer(ArchaicTooth __instance, Player player)
    {
        var starterCard = player.Deck.Cards.FirstOrDefault(TranscendenceRegistry.IsStarterCard);
        if (starterCard == null || !TranscendenceRegistry.TryGetAncientCard(starterCard, out _))
        {
            return true;
        }

        StarterCardField.SetValue(__instance, starterCard.ToSerializable());
        AncientCardField.SetValue(__instance, TranscendenceRegistry.CreateTransformedCard(starterCard)!.ToSerializable());
        UpdateHoverTipsMethod.Invoke(__instance, null);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ArchaicTooth.AfterObtained))]
    public static bool PrefixAfterObtained(ArchaicTooth __instance, ref Task __result)
    {
        var task = AfterObtainedAsync(__instance);
        __result = task;
        return false;
    }

    private static async Task AfterObtainedAsync(ArchaicTooth relic)
    {
        var starterCard = relic.Owner?.Deck?.Cards.FirstOrDefault(TranscendenceRegistry.IsStarterCard);
        if (starterCard == null)
        {
            return;
        }

        var transformedCard = TranscendenceRegistry.CreateTransformedCard(starterCard);
        if (transformedCard == null)
        {
            return;
        }

        await CardCmd.Transform(starterCard, transformedCard);
    }
}
