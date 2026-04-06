using Godot;
using HarmonyLib;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(Node), "MoveChild")]
public static class NodeMoveChildPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Node __instance, Node childNode, int toIndex)
    {
        if (childNode == null)
        {
            MainFile.Logger.Warn($"MoveChild called with null child, skipping. Parent: {__instance?.Name}, toIndex: {toIndex}");
            return false;
        }
        return true;
    }
}

