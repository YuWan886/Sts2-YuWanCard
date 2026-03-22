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
[HarmonyPriority(Priority.Low)]
public class GoodModifiersPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
    {
        for (int i = 0; i < __result.Count; i++)
        {
            if (__result[i] is EndlessModifier)
            {
                return;
            }
        }

        var endlessModifier = ModelDb.Modifier<EndlessModifier>();
        if (endlessModifier != null)
        {
            var list = new List<ModifierModel>(__result) { endlessModifier };
            __result = list.AsReadOnly();
        }
    }
}
