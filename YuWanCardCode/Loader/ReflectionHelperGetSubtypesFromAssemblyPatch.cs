using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace YuWanCard.Loader;

/// <summary>
///     <c>ModelIdSerializationCache.Init</c> builds the net-ID map by scanning
///     <c>mod.assembly</c> (the loader) directly via <c>ReflectionHelper.GetSubtypesFromAssembly</c> —
///     it does NOT go through <c>ReflectionHelper.ModTypes</c>. Without this bridge, content
///     models living in the variant assembly never get net IDs and <c>AbstractModel.InitId</c>
///     throws ("could not be mapped to any net ID") during <c>ModelDb.InitIds</c>.
/// </summary>
[HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.GetSubtypesFromAssembly))]
internal static class ReflectionHelperGetSubtypesFromAssemblyPatch
{
    private static void Postfix(Assembly assembly, Type parentType, ref IEnumerable<Type> __result)
    {
        if (!ReferenceEquals(assembly, typeof(LoaderMain).Assembly))
            return;

        var variantTypes = LoaderMain.GetVariantModTypes();
        if (variantTypes.Length == 0)
            return;

        // Apply the same filter as ReflectionHelper.GetSubtypesFromList.
        var extra = variantTypes
            .Where(type => !type.IsAbstract && !type.IsInterface && ReflectionHelper.InheritsOrImplements(type, parentType))
            .ToArray();
        if (extra.Length == 0)
            return;

        __result = __result.Concat(extra).Distinct();
    }
}
