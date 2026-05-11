using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Patches EnergyIconHelper.GetPath to support custom BigEnergyIconPath per pool,
/// and EnergyIconsFormatter.TryEvaluateFormat to support custom TextEnergyIconPath.
/// Pools register by calling RegisterPoolEnergyIcon(Id, BigEnergyIconPath, TextEnergyIconPath)
/// from their EnergyColorName getter, which returns a unique prefix for this pool.
/// </summary>
public static class CustomEnergyIconPatches
{
    private static readonly Dictionary<string, string> _bigIconPaths = new();
    private static readonly Dictionary<string, string> _textIconPaths = new();

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

    [HarmonyPatch(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPath), typeof(string))]
    static class IconPatch
    {
        static bool Prefix(string prefix, ref string __result)
        {
            if (_bigIconPaths.TryGetValue(prefix, out string? path))
            {
                __result = path;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Transpiler that redirects sprite font energy icon paths to custom TextEnergyIconPath.
    /// Matches the string.Concat that builds "[img]res://.../sprite_fonts/{prefix}_energy_icon.png[/img]"
    /// and replaces the result for custom prefixes.
    /// </summary>
    [HarmonyPatch(typeof(EnergyIconsFormatter), "TryEvaluateFormat")]
    static class TextIconPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var concat3 = AccessTools.Method(typeof(string), nameof(string.Concat),
                [typeof(string), typeof(string), typeof(string)]);

            for (int i = 2; i < code.Count - 2; i++)
            {
                if (code[i - 2].opcode == OpCodes.Ldstr &&
                    (code[i - 2].operand as string) == "[img]res://images/packed/sprite_fonts/" &&
                    IsLdloc(code[i - 1]) &&
                    code[i].opcode == OpCodes.Ldstr &&
                    (code[i].operand as string) == "_energy_icon.png[/img]" &&
                    code[i + 1].Calls(concat3) &&
                    IsStloc(code[i + 2]))
                {
                    var ldPrefix = code[i - 1];
                    var stText3 = code[i + 2];
                    var ldText3 = StlocToLdloc(stText3);

                    var insert = new List<CodeInstruction>
                    {
                        new(ldPrefix.opcode, ldPrefix.operand),
                        ldText3,
                        CodeInstruction.Call(typeof(CustomEnergyIconPatches), nameof(GetCustomTextIcon)),
                        new(stText3.opcode, stText3.operand),
                    };
                    code.InsertRange(i + 3, insert);
                    break;
                }
            }
            return code;
        }

        static bool IsLdloc(CodeInstruction ci) =>
            ci.opcode == OpCodes.Ldloc_0 || ci.opcode == OpCodes.Ldloc_1 ||
            ci.opcode == OpCodes.Ldloc_2 || ci.opcode == OpCodes.Ldloc_3 ||
            ci.opcode == OpCodes.Ldloc_S || ci.opcode == OpCodes.Ldloc;

        static bool IsStloc(CodeInstruction ci) =>
            ci.opcode == OpCodes.Stloc_0 || ci.opcode == OpCodes.Stloc_1 ||
            ci.opcode == OpCodes.Stloc_2 || ci.opcode == OpCodes.Stloc_3 ||
            ci.opcode == OpCodes.Stloc_S || ci.opcode == OpCodes.Stloc;

        static CodeInstruction StlocToLdloc(CodeInstruction stloc)
        {
            if (stloc.opcode == OpCodes.Stloc_0) return new(OpCodes.Ldloc_0);
            if (stloc.opcode == OpCodes.Stloc_1) return new(OpCodes.Ldloc_1);
            if (stloc.opcode == OpCodes.Stloc_2) return new(OpCodes.Ldloc_2);
            if (stloc.opcode == OpCodes.Stloc_3) return new(OpCodes.Ldloc_3);
            if (stloc.opcode == OpCodes.Stloc_S) return new(OpCodes.Ldloc_S, stloc.operand);
            return new(OpCodes.Ldloc, stloc.operand);
        }
    }
}
