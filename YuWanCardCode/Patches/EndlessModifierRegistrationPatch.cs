using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(SavedPropertiesTypeCache), nameof(SavedPropertiesTypeCache.GetJsonPropertiesForType))]
public class SavedPropertiesTypeCachePatch
{
    private static bool _injected = false;

    [HarmonyPrefix]
    public static void Prefix(Type t)
    {
        if (_injected) return;
        
        if (t == typeof(EndlessModifier) || t.Assembly == typeof(EndlessModifier).Assembly)
        {
            if (!SavedPropertiesTypeCacheContains(typeof(EndlessModifier)))
            {
                SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(EndlessModifier));
                MainFile.Logger.Info("EndlessModifier injected into SavedPropertiesTypeCache");
            }
            _injected = true;
        }
    }

    private static bool SavedPropertiesTypeCacheContains(Type type)
    {
        var cache = AccessTools.Field(typeof(SavedPropertiesTypeCache), "_cache")?.GetValue(null) as System.Collections.IDictionary;
        return cache?.Contains(type) == true;
    }
}

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
