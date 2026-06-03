using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Balatro;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(CardModel))]
public static class BalatroCardEditionPersistencePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CardModel.ToSerializable))]
    public static void PersistGenericEdition(CardModel __instance, ref SerializableCard __result)
    {
        BalatroCardEditionHelper.WriteGenericEditionToSerializable(__instance, __result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CardModel.FromSerializable))]
    public static void RestoreGenericEdition(SerializableCard save, ref CardModel __result)
    {
        BalatroCardEditionHelper.RestoreGenericEditionFromSerializable(__result, save);
    }
}
