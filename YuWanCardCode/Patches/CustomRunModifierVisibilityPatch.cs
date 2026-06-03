using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NCustomRunModifiersList), "GetAllModifiers")]
public static class CustomRunModifierVisibilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<ModifierModel> __result)
    {
        __result = __result.Where(static modifier => modifier is not YuWanModifierModel { AllowedInCustomRun: false });
    }
}
