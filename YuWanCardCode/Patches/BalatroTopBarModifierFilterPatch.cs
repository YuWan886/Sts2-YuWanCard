using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NTopBar), nameof(NTopBar.Initialize))]
public static class BalatroTopBarModifierFilterPatch
{
    [HarmonyPrefix]
    public static bool Prefix(IRunState runState)
    {
        return true;
    }

    [HarmonyFinalizer]
    public static Exception? Finalizer(IRunState runState, Exception? __exception)
    {
        return __exception;
    }
}
