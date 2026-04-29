using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public class YuWanModifierRegistrationPatch
{
    private static readonly MethodInfo? _modifierMethod = typeof(ModelDb).GetMethod("Modifier", Type.EmptyTypes);

    [HarmonyPostfix]
    public static void Postfix()
    {
        foreach (var modifier in Modifiers.YuWanModifierModel.RegisteredModifiers)
        {
            var modifierType = modifier.GetType();
            if (!ModelDb.Contains(modifierType))
            {
                ModelDb.Inject(modifierType);
                MainFile.Logger.Info($"{modifierType.Name} registered to ModelDb");
            }
        }
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GoodModifiers), MethodType.Getter)]
[HarmonyPriority(Priority.Low)]
public class YuWanGoodModifiersPatch
{
    private static readonly MethodInfo? _modifierMethod = typeof(ModelDb).GetMethod("Modifier", Type.EmptyTypes);

    [HarmonyPostfix]
    public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
    {
        var existingTypes = new HashSet<Type>(__result.Select(m => m.GetType()));

        var newModifiers = new List<ModifierModel>();
        foreach (var modifier in Modifiers.YuWanModifierModel.RegisteredModifiers)
        {
            if (!existingTypes.Contains(modifier.GetType()))
            {
                var genericMethod = _modifierMethod?.MakeGenericMethod(modifier.GetType());
                if (genericMethod?.Invoke(null, null) is ModifierModel dbModifier)
                {
                    newModifiers.Add(dbModifier);
                }
            }
        }

        if (newModifiers.Count > 0)
        {
            var list = new List<ModifierModel>(__result);
            list.AddRange(newModifiers);
            __result = list.AsReadOnly();
        }
    }
}
