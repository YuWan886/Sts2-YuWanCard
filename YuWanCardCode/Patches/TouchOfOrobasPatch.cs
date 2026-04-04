using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(TouchOfOrobas))]
public static class TouchOfOrobasPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(TouchOfOrobas.RefinementUpgrades), MethodType.Getter)]
    public static void AddPigCarrotUpgrade(ref Dictionary<ModelId, RelicModel> __result)
    {
        var pigCarrot = ModelDb.Relic<PigCarrot>();
        var pigGoldenCarrot = ModelDb.Relic<PigGoldenCarrot>();
        
        if (!__result.ContainsKey(pigCarrot.Id))
        {
            __result[pigCarrot.Id] = pigGoldenCarrot;
        }
    }
}
