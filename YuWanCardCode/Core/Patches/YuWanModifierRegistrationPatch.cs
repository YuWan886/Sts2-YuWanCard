using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public class YuWanModifierRegistrationPatch
{
    private static readonly MethodInfo? _modifierMethod = typeof(ModelDb).GetMethod("Modifier", Type.EmptyTypes);

    [HarmonyPostfix]
    public static void Postfix()
    {
        foreach (var modifier in YuWanModifierModel.RegisteredModifiers)
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

    /// <summary>
    /// Thread-static flag set by <see cref="YuWanDailyRunModifierFilterPatch"/> during
    /// <see cref="NDailyRunScreen.RollModifiers"/> to signal that GoodModifiers are being
    /// queried for the daily challenge, so daily-unsafe YuWan modifiers should be excluded.
    /// </summary>
    [ThreadStatic]
    internal static bool IsDailyRunContext;

    [HarmonyPostfix]
    public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
    {
        var existingTypes = new HashSet<Type>(__result.Select(m => m.GetType()));

        var newModifiers = new List<ModifierModel>();
        foreach (var modifier in YuWanModifierModel.RegisteredModifiers)
        {
            // In daily run context, skip modifiers that opt out of daily challenges
            if (IsDailyRunContext && !modifier.AllowedInDailyRun)
                continue;

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

/// <summary>
/// Sets <see cref="YuWanGoodModifiersPatch.IsDailyRunContext"/> around
/// <see cref="NDailyRunScreen.RollModifiers"/> so that the GoodModifiers getter
/// can exclude daily-unsafe YuWan modifiers only when the daily challenge is
/// rolling its modifier set.
/// </summary>
[HarmonyPatch(typeof(NDailyRunScreen), "RollModifiers")]
public class YuWanDailyRunModifierFilterPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        YuWanGoodModifiersPatch.IsDailyRunContext = true;
    }

    [HarmonyPostfix]
    public static void Postfix()
    {
        YuWanGoodModifiersPatch.IsDailyRunContext = false;
    }
}
