using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace YuWanCard.Core.Interop;

public static class ModInteropProcessor
{
    private static readonly BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private static readonly FieldInfo WrappedValueField = AccessTools.DeclaredField(typeof(InteropClassWrapper), nameof(InteropClassWrapper.Value));

    /// <summary>
    /// Scan the given assembly for [ModInterop] types and, for each target mod that is actually
    /// loaded, replace the interop stubs with real calls via Harmony transpilers.
    /// </summary>
    public static void Process(Harmony harmony, Assembly sourceAssembly)
    {
        IReadOnlyDictionary<string, Assembly> loaded = ModCompat.GetLoadedAssemblies();

        foreach (var type in sourceAssembly.GetTypes())
        {
            var interop = type.GetCustomAttribute<ModInteropAttribute>();
            if (interop == null) continue;

            if (!loaded.TryGetValue(interop.ModId, out var targetAssembly))
            {
                MainFile.Logger.Debug($"ModInterop: '{interop.ModId}' not loaded, skipping {type.Name} (using fallbacks)");
                continue;
            }

            MainFile.Logger.Debug($"ModInterop: processing {type.Name} → {interop.ModId}");
            ProcessType(harmony, type, targetAssembly, interop.Type, requireStatic: true);
        }
    }

    private static void ProcessType(Harmony harmony, Type interopType, Assembly targetAssembly,
        string? contextType, bool requireStatic)
    {
        foreach (var member in interopType.GetMembers(MemberFlags))
        {
            switch (member)
            {
                case MethodInfo method:
                    if (method.IsSpecialName) continue;
                    if (requireStatic && !method.IsStatic) continue;
                    PatchMethod(harmony, method, targetAssembly, contextType);
                    break;

                case PropertyInfo prop:
                    if (requireStatic && !(prop.SetMethod?.IsStatic ?? true)) continue;
                    PatchProperty(harmony, prop, targetAssembly, contextType);
                    break;

                case TypeInfo nested when nested.IsSubclassOf(typeof(InteropClassWrapper)):
                    PatchNestedType(harmony, nested, targetAssembly, contextType);
                    break;
            }
        }
    }

    // ── method ────────────────────────────────────────────
    internal static void PatchMethod(Harmony harmony, MethodInfo method, Assembly targetAssembly, string? contextType)
    {
        var targetAttr = method.GetCustomAttribute<InteropTargetAttribute>();
        var typeName = targetAttr?.Type ?? contextType;
        if (typeName == null) return;

        var methodName = targetAttr?.Name ?? method.Name;

        try
        {
            var targetType = ResolveType(typeName, targetAssembly);
            if (targetType == null) return;

            var interopParams = method.GetParameters().Select(p => p.ParameterType).ToArray();
            var (targetMethod, offset) = FindTargetMethod(method, targetType, methodName, interopParams);
            if (targetMethod == null) return;

            var loadParams = new List<CodeInstruction>();

            if (targetMethod.ReturnType != typeof(void))
                loadParams.Add(new CodeInstruction(OpCodes.Pop));

            // Load instance for non-static targets
            if (!targetMethod.IsStatic)
            {
                if (method.IsStatic)
                {
                    loadParams.Add(CodeInstruction.LoadArgument(0));
                    if (interopParams[0] != targetType)
                        loadParams.Add(new CodeInstruction(OpCodes.Castclass, targetType));
                }
                else
                {
                    loadParams.Add(CodeInstruction.LoadArgument(0));
                    loadParams.Add(new CodeInstruction(OpCodes.Ldfld, WrappedValueField));
                }
            }

            for (var i = 0; i < targetMethod.GetParameters().Length; i++)
            {
                loadParams.Add(CodeInstruction.LoadArgument(i + offset));
                var srcType = interopParams[i + offset];
                var tgtType = targetMethod.GetParameters()[i].ParameterType;
                if (srcType != tgtType && srcType != typeof(object))
                    loadParams.Add(new CodeInstruction(OpCodes.Castclass, tgtType));
            }

            loadParams.Add(new CodeInstruction(OpCodes.Call, targetMethod));
            PatchViaTranspiler(harmony, method, loadParams);

            MainFile.Logger.Debug($"ModInterop: patched method {method.Name}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"ModInterop: failed to patch method {method.Name} → {typeName}.{methodName}: {ex.Message}");
        }
    }

    // ── property / field ──────────────────────────────────
    internal static void PatchProperty(Harmony harmony, PropertyInfo prop, Assembly targetAssembly, string? contextType)
    {
        var targetAttr = prop.GetCustomAttribute<InteropTargetAttribute>();
        var typeName = targetAttr?.Type ?? contextType;
        if (typeName == null) return;

        var name = targetAttr?.Name ?? prop.Name;

        try
        {
            var targetType = ResolveType(typeName, targetAssembly);
            if (targetType == null) return;

            // Try property first
            var targetProp = targetType.GetProperty(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (targetProp != null && targetProp.PropertyType == prop.PropertyType)
            {
                if (targetProp.GetMethod != null && prop.GetMethod != null)
                {
                    var isStatic = targetProp.GetMethod.IsStatic;
                    PatchViaTranspiler(harmony, prop.GetMethod, isStatic
                        ? [new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Call, targetProp.GetMethod)]
                        : [new CodeInstruction(OpCodes.Pop), CodeInstruction.LoadArgument(0),
                           new CodeInstruction(OpCodes.Ldfld, WrappedValueField),
                           new CodeInstruction(OpCodes.Call, targetProp.GetMethod)]);
                }

                if (targetProp.SetMethod != null && prop.SetMethod != null)
                {
                    var isStatic = targetProp.SetMethod.IsStatic;
                    PatchViaTranspiler(harmony, prop.SetMethod, isStatic
                        ? [CodeInstruction.LoadArgument(0), new CodeInstruction(OpCodes.Call, targetProp.SetMethod)]
                        : [CodeInstruction.LoadArgument(0), new CodeInstruction(OpCodes.Ldfld, WrappedValueField),
                           CodeInstruction.LoadArgument(1), new CodeInstruction(OpCodes.Call, targetProp.SetMethod)]);
                }
                return;
            }

            // Try field
            var targetField = targetType.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (targetField != null && targetField.FieldType == prop.PropertyType)
            {
                if (prop.GetMethod != null)
                {
                    PatchViaTranspiler(harmony, prop.GetMethod, targetField.IsStatic
                        ? [new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ldfld, targetField)]
                        : [new CodeInstruction(OpCodes.Pop), CodeInstruction.LoadArgument(0),
                           new CodeInstruction(OpCodes.Ldfld, WrappedValueField),
                           new CodeInstruction(OpCodes.Ldfld, targetField)]);
                }

                if (prop.SetMethod != null)
                {
                    PatchViaTranspiler(harmony, prop.SetMethod, targetField.IsStatic
                        ? [CodeInstruction.LoadArgument(0), new CodeInstruction(OpCodes.Stfld, targetField)]
                        : [CodeInstruction.LoadArgument(0), new CodeInstruction(OpCodes.Ldfld, WrappedValueField),
                           CodeInstruction.LoadArgument(1), new CodeInstruction(OpCodes.Stfld, targetField)]);
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"ModInterop: failed to patch property {prop.Name}: {ex.Message}");
        }
    }

    // ── nested wrapper type ───────────────────────────────
    internal static void PatchNestedType(Harmony harmony, TypeInfo nested, Assembly targetAssembly, string? contextType)
    {
        var targetAttr = nested.GetCustomAttribute<InteropTargetAttribute>();
        var typeName = targetAttr?.Type ?? targetAttr?.Name ?? contextType;
        if (typeName == null) return;

        try
        {
            var targetType = ResolveType(typeName, targetAssembly);
            if (targetType == null) return;

            foreach (var ctor in nested.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var ctorParams = ctor.GetParameters().Select(p => p.ParameterType).ToArray();
                var targetCtor = targetType.GetConstructor(ctorParams);
                if (targetCtor == null) continue;

                var load = new List<CodeInstruction> { CodeInstruction.LoadArgument(0) };
                for (int i = 0; i < ctorParams.Length; i++)
                    load.Add(CodeInstruction.LoadArgument(i + 1));
                load.Add(new CodeInstruction(OpCodes.Newobj, targetCtor));
                load.Add(new CodeInstruction(OpCodes.Stfld, WrappedValueField));
                PatchViaTranspiler(harmony, ctor, load);
            }

            ProcessType(harmony, nested, targetAssembly, typeName, requireStatic: false);

            MainFile.Logger.Debug($"ModInterop: patched wrapper type {nested.Name}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"ModInterop: failed to patch nested type {nested.Name}: {ex.Message}");
        }
    }

    // ── helpers ───────────────────────────────────────────

    private static Type? ResolveType(string typeName, Assembly targetAssembly)
    {
        return ModCompat.ResolveType(targetAssembly, typeName);
    }

    private static (MethodInfo? method, int offset) FindTargetMethod(
        MethodInfo interopMethod, Type targetType, string methodName, Type[] interopParams)
    {
        foreach (var candidate in targetType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (candidate.Name != methodName) continue;

            var targetParams = candidate.GetParameters();
            var checkParams = candidate.IsStatic ? interopParams : interopParams;

            if (targetParams.Length != checkParams.Length) continue;
            if (!ParametersMatch(targetParams, checkParams)) continue;

            int offset = 0;
            if (!candidate.IsStatic)
            {
                if (!interopMethod.IsStatic) return (candidate, 0);
                if (interopParams.Length < 1)
                    throw new InvalidOperationException($"Non-static target {methodName} requires first parameter as instance");
                offset = 1;
            }

            return (candidate, offset);
        }
        return (null, 0);
    }

    private static bool ParametersMatch(ParameterInfo[] targetParams, Type[] checkParams)
    {
        for (int i = 0; i < targetParams.Length; i++)
        {
            var check = checkParams[i];
            if (check == typeof(object)) continue; // wildcard
            if (check.IsAssignableTo(targetParams[i].ParameterType)) continue;
            return false;
        }
        return true;
    }

    private static void PatchViaTranspiler(Harmony harmony, MethodBase original, List<CodeInstruction> insertBeforeRet)
    {
        var transpiler = new HarmonyMethod(typeof(RetTranspiler), nameof(RetTranspiler.Transpile));
        RetTranspiler.Insert = insertBeforeRet;
        harmony.Patch(original, transpiler: transpiler);
    }
}

// ── reusable transpiler ──────────────────────────────────

file static class RetTranspiler
{
    public static List<CodeInstruction> Insert = [];

    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var list = instructions.ToList();
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].opcode == OpCodes.Ret)
            {
                list.InsertRange(i, Insert);
                break;
            }
        }
        return list;
    }
}
