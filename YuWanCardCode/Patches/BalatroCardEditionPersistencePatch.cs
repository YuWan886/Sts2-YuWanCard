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
        BalatroCardEditionHelper.RefreshEditionAfterCardStateRebuild(__result);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CardModel.DowngradeInternal))]
    public static void RefreshEditionAfterDowngrade(CardModel __instance)
    {
        BalatroCardEditionHelper.RefreshEditionAfterCardStateRebuild(__instance);
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
public static class BalatroCardEditionClonePatch
{
    [HarmonyPostfix]
    public static void CopyEditionState(AbstractModel __instance, ref AbstractModel __result)
    {
        if (__instance is CardModel source && __result is CardModel clone)
        {
            BalatroCardEditionHelper.CopyEditionStateToClone(source, clone);
        }
    }
}
