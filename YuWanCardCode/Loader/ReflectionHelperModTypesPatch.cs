using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace YuWanCard.Loader;

/// <summary>
///     Bridges the loaded content-variant assembly into the game's mod-type discovery.
///     <c>ReflectionHelper.ModTypes</c> only returns <c>mod.assembly.GetTypes()</c> (the
///     loader), so without this the game's <c>ModelDb</c> / <c>ActionTypes</c> /
///     <c>MessageTypes</c> would never see the content models living in the variant.
/// </summary>
[HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes), MethodType.Getter)]
internal static class ReflectionHelperModTypesPatch
{
    private static void Postfix(ref Type[] __result)
    {
        var variantTypes = LoaderMain.GetVariantModTypes();
        if (variantTypes.Length == 0)
            return;

        __result = [.. __result.Concat(variantTypes).Distinct()];
    }
}
