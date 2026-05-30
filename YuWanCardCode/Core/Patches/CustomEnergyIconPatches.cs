using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Hextech;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Patches EnergyIconHelper.GetPath to support custom BigEnergyIconPath per pool,
/// and EnergyIconsFormatter.TryEvaluateFormat to support custom TextEnergyIconPath.
/// Pools register by calling RegisterPoolEnergyIcon(Id, BigEnergyIconPath, TextEnergyIconPath)
/// from their EnergyColorName getter, which returns a unique prefix for this pool.
/// </summary>
public static class CustomEnergyIconPatches
{
    private static readonly Dictionary<string, string> _bigIconPaths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _textIconPaths = new(StringComparer.OrdinalIgnoreCase);

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPath), [typeof(string)]),
            prefix: new HarmonyMethod(typeof(CustomEnergyIconPatches), nameof(EnergyIconHelperGetPathPrefix)));

        harmony.Patch(
            original: AccessTools.Method(typeof(EnergyIconsFormatter), nameof(EnergyIconsFormatter.TryEvaluateFormat)),
            prefix: new HarmonyMethod(typeof(CustomEnergyIconPatches), nameof(EnergyIconsFormatterTryEvaluateFormatPrefix)));

        harmony.Patch(
            original: AccessTools.Method(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPrefix), [typeof(AbstractModel)]),
            prefix: new HarmonyMethod(typeof(CustomEnergyIconPatches), nameof(EnergyIconHelperGetPrefixPrefix)));
    }

    /// <summary>
    /// Registers a pool's custom BigEnergyIconPath and TextEnergyIconPath, and returns
    /// a unique EnergyColorName prefix that triggers the overrides.
    /// </summary>
    public static string RegisterPoolEnergyIcon(ModelId id, string? bigIconPath, string? textIconPath)
    {
        var prefix = $"yuwan_{id.Entry}";
        if (bigIconPath != null)
            _bigIconPaths[prefix] = bigIconPath;
        else
            _bigIconPaths.Remove(prefix);
        if (textIconPath != null)
            _textIconPaths[prefix] = textIconPath;
        else
            _textIconPaths.Remove(prefix);
        return prefix;
    }

    /// <summary>Gets the correct text energy icon path for a given energy color prefix.</summary>
    public static string GetCustomTextIcon(string prefix, string originalPath)
    {
        if (_textIconPaths.TryGetValue(prefix, out string? textIconPath))
            return $"[img]{textIconPath}[/img]";
        if (_bigIconPaths.ContainsKey(prefix))
            return "[img]res://images/packed/sprite_fonts/red_energy_icon.png[/img]";
        return originalPath;
    }

    private static string? GetResolvedPrefix(object? currentValue, string? formatterOptions, out int result)
    {
        result = 0;
        string? prefix = null;
        if (currentValue is EnergyVar energyVar)
        {
            result = Convert.ToInt32(energyVar.PreviewValue);
            if (!string.IsNullOrEmpty(energyVar.ColorPrefix))
            {
                prefix = energyVar.ColorPrefix;
            }
        }
        else if (currentValue is CalculatedVar calculatedVar)
        {
            result = Convert.ToInt32(calculatedVar.Calculate(null));
        }
        else if (currentValue is decimal amount)
        {
            result = (int)amount;
        }
        else if (currentValue is int amountInt)
        {
            result = amountInt;
        }
        else if (currentValue is string prefixValue)
        {
            if (!int.TryParse(formatterOptions, out result))
            {
                return null;
            }
            prefix = prefixValue;
        }
        else
        {
            return null;
        }

        if (string.IsNullOrEmpty(prefix) || prefix.Equals("colorless", StringComparison.OrdinalIgnoreCase))
        {
            prefix = RunManager.Instance.GetLocalCharacterEnergyIconPrefix();
        }

        if (string.IsNullOrEmpty(prefix))
        {
            Log.Warn("No energy prefix found for custom energy icon formatter! Using colorless as a fallback.");
            prefix = "colorless";
        }

        return prefix;
    }

    private static object? GetFormattingInfoProperty(object formattingInfo, string propertyName)
    {
        return formattingInfo.GetType().GetProperty(propertyName)?.GetValue(formattingInfo);
    }

    private static void WriteFormattingInfo(object formattingInfo, string text)
    {
        formattingInfo.GetType().GetMethod("Write", [typeof(string)])?.Invoke(formattingInfo, [text]);
    }

    public static bool EnergyIconHelperGetPathPrefix(string prefix, ref string __result)
    {
        if (_bigIconPaths.TryGetValue(prefix, out string? path))
        {
            __result = path;
            return false;
        }

        return true;
    }

    public static bool EnergyIconsFormatterTryEvaluateFormatPrefix(object __0, ref bool __result)
    {
        var formattingInfo = __0;
        var currentValue = GetFormattingInfoProperty(formattingInfo, "CurrentValue");
        var formatterOptions = GetFormattingInfoProperty(formattingInfo, "FormatterOptions") as string;
        var prefix = GetResolvedPrefix(
            currentValue,
            formatterOptions,
            out var result);

        if (string.IsNullOrEmpty(prefix))
        {
            return true;
        }

        if (!_textIconPaths.ContainsKey(prefix) && !_bigIconPaths.ContainsKey(prefix))
        {
            return true;
        }

        var originalIconMarkup = $"[img]res://images/packed/sprite_fonts/{prefix}_energy_icon.png[/img]";
        var resolvedIconMarkup = GetCustomTextIcon(prefix, originalIconMarkup);
        var output = (result > 0 && result < 4)
            ? string.Concat(Enumerable.Repeat(resolvedIconMarkup, result))
            : currentValue is DynamicVar dynamicVar
                ? dynamicVar.ToHighlightedString(inverse: false) + resolvedIconMarkup
                : $"{result}{resolvedIconMarkup}";

        WriteFormattingInfo(formattingInfo, output);
        __result = true;
        return false;
    }

    /// <summary>
    /// Short-circuit <see cref="EnergyIconHelper.GetPrefix"/> for hextech pig/shared runes
    /// whose <see cref="RelicModel.Pool"/> calls .First() on AllRelicPools and throws
    /// when the rune isn't in any vanilla pool. Returns the pig card-pool energy prefix
    /// so the original GetPool (which would throw) is skipped entirely.
    /// </summary>
    public static bool EnergyIconHelperGetPrefixPrefix(AbstractModel model, ref string __result)
    {
        if (model is RelicModel relic && HextechPigRuneRegistry.IsPigOrSharedRune(relic))
        {
            __result = ModelDb.CardPool<Characters.PigCardPool>().EnergyColorName;
            return false;
        }

        return true;
    }
}
