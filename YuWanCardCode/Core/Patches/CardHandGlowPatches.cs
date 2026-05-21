using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Merges registered hand glow rules into the vanilla gold glow channel.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldGlowGold), MethodType.Getter)]
static class CardModelShouldGlowGoldRegistryPatch
{
    [HarmonyPostfix]
    static void Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result && CardHandGlowRegistry.EvaluateGold(__instance))
            __result = true;
    }
}

/// <summary>
/// Merges registered hand glow rules into the vanilla red glow channel.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldGlowRed), MethodType.Getter)]
static class CardModelShouldGlowRedRegistryPatch
{
    [HarmonyPostfix]
    static void Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result && CardHandGlowRegistry.EvaluateRed(__instance))
            __result = true;
    }
}
