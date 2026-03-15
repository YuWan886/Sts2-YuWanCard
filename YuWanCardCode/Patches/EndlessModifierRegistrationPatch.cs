using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Modifiers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public class EndlessModifierRegistrationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!ModelDb.Contains(typeof(EndlessModifier)))
        {
            ModelDb.Inject(typeof(EndlessModifier));
            MainFile.Logger.Info("EndlessModifier registered to ModelDb");
        }
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GoodModifiers), MethodType.Getter)]
public class GoodModifiersPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
    {
        var list = __result.ToList();
        if (!list.Any(m => m is EndlessModifier))
        {
            list.Add(ModelDb.Modifier<EndlessModifier>());
            __result = list;
        }
    }
}
