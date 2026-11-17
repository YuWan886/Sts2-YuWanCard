using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace YuWanCard.Core.Interop;

/// <summary>
/// Shared runtime helpers for optional cross-mod integration.
/// Keeps mod lookup, type resolution, and common Harmony patch wiring in Core
/// so feature integrations only need to express their actual business logic.
/// </summary>
public static class ModCompat
{
    public static IReadOnlyDictionary<string, Assembly> GetLoadedAssemblies()
    {
        Dictionary<string, Assembly> map = [];
        foreach (var mod in ModManager.GetLoadedMods())
        {
            if (mod.manifest?.id is { } id && mod.assemblies is { Count: > 0 } && mod.assemblies[0] is { } asm)
            {
                map[id] = asm;
            }
        }

        return map;
    }

    public static bool TryGetAssembly(string modId, out Assembly? assembly)
    {
        if (GetLoadedAssemblies().TryGetValue(modId, out Assembly? loadedAssembly))
        {
            assembly = loadedAssembly;
            return true;
        }

        assembly = null;
        return false;
    }

    public static bool IsLoaded(string modId)
    {
        return TryGetAssembly(modId, out _);
    }

    public static ModCompatContext? TryCreate(string modId, string? logPrefix = null)
    {
        return TryGetAssembly(modId, out Assembly? assembly)
            ? new ModCompatContext(modId, assembly!, logPrefix)
            : null;
    }

    internal static Type? ResolveType(Assembly targetAssembly, string typeName)
    {
        return Type.GetType($"{typeName}, {targetAssembly.FullName}") ?? targetAssembly.GetType(typeName);
    }
}

public sealed class ModCompatContext
{
    private readonly string _logPrefix;

    internal ModCompatContext(string modId, Assembly assembly, string? logPrefix)
    {
        ModId = modId;
        Assembly = assembly;
        _logPrefix = string.IsNullOrWhiteSpace(logPrefix)
            ? $"ModCompat[{modId}]"
            : logPrefix!;
    }

    public string ModId { get; }
    public Assembly Assembly { get; }

    public Type? ResolveType(string typeName)
    {
        return ModCompat.ResolveType(Assembly, typeName);
    }

    public bool PatchMethod(
        Harmony harmony,
        Type targetType,
        string methodName,
        Type patchOwner,
        string? prefixName = null,
        string? postfixName = null)
    {
        MethodInfo? original = AccessTools.Method(targetType, methodName);
        MethodInfo? prefix = prefixName == null ? null : AccessTools.Method(patchOwner, prefixName);
        MethodInfo? postfix = postfixName == null ? null : AccessTools.Method(patchOwner, postfixName);

        if (original == null || (prefixName != null && prefix == null) || (postfixName != null && postfix == null))
        {
            MainFile.Logger.Warn($"{_logPrefix}: skipped patch {targetType.Name}.{methodName}");
            return false;
        }

        harmony.Patch(
            original,
            prefix: prefix == null ? null : new HarmonyMethod(prefix),
            postfix: postfix == null ? null : new HarmonyMethod(postfix));
        return true;
    }

    public void PatchMethods(
        Harmony harmony,
        Type? targetType,
        Type patchOwner,
        string? prefixName,
        string? postfixName,
        params string[] methodNames)
    {
        if (targetType == null)
        {
            return;
        }

        foreach (string methodName in methodNames)
        {
            PatchMethod(harmony, targetType, methodName, patchOwner, prefixName, postfixName);
        }
    }
}
