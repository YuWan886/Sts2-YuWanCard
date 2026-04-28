using System.Reflection;
using Godot;
using HarmonyLib;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Patches the non-generic PackedScene.Instantiate(GenEditState) so that registered scenes
/// are auto-converted to the correct node type before Instantiate&lt;T&gt;'s cast.
///
/// The generic Instantiate&lt;T&gt;() calls the non-generic version internally, then casts:
///     return (T)(object)Instantiate(editState);
/// Our postfix runs between Instantiate() returning and the cast, replacing the result.
/// </summary>
[HarmonyPatch]
static class SceneConversionPatch
{
    static MethodBase TargetMethod()
    {
        var method = typeof(PackedScene).GetMethod("Instantiate", 0, [typeof(PackedScene.GenEditState)]);

        if (method == null)
            throw new InvalidOperationException(
                "Could not find PackedScene.Instantiate(GenEditState). " +
                "The Godot API may have changed — scene auto-conversion will not work.");

        return method;
    }

    [HarmonyPostfix]
    static void Postfix(PackedScene __instance, ref Node? __result)
    {
        NodeFactory.TryAutoConvert(__instance, ref __result);
    }
}
