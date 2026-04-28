using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YuWanCard.Core.Extensions;

/// <summary>
/// Extension methods for DynamicVar. 
/// </summary>
public static class DynamicVarExtensions
{
    public static readonly SpireField<DynamicVar, Func<IHoverTip>> DynamicVarTips = new(() => null!);
    public static readonly SpireField<DynamicVar, decimal?> DynamicVarUpgrades = new(() => null);

    public static TDynamicVar WithUpgrade<TDynamicVar>(this TDynamicVar dynamicVar, decimal upgradeValue)
        where TDynamicVar : DynamicVar
    {
        if (upgradeValue != 0) DynamicVarUpgrades[dynamicVar] = upgradeValue;
        return dynamicVar;
    }

    public static TDynamicVar WithTooltip<TDynamicVar>(this TDynamicVar var, string? locKey = null,
        string locTable = "static_hover_tips") where TDynamicVar : DynamicVar
    {
        string key = locKey ?? var.Name.ToUpperInvariant();
        DynamicVarTips[var] = () =>
        {
            LocString locString = new(locTable, key + ".title");
            LocString locString2 = new(locTable, key + ".description");
            locString.Add(var);
            locString2.Add(var);
            return new HoverTip(locString, locString2);
        };
        return var;
    }
}

[HarmonyPatch(typeof(DynamicVar), nameof(DynamicVar.Clone))]
file class CloneTooltips
{
    [HarmonyPostfix]
    static DynamicVar Copy(DynamicVar __result, DynamicVar __instance)
    {
        DynamicVarExtensions.DynamicVarTips[__result] = DynamicVarExtensions.DynamicVarTips[__instance];
        DynamicVarExtensions.DynamicVarUpgrades[__result] = DynamicVarExtensions.DynamicVarUpgrades[__instance];
        return __result;
    }
}
