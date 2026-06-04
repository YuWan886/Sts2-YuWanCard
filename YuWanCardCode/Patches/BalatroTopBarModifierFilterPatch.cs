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
        if (runState is RunState state)
        {
            MainFile.Logger.Info(
                $"[BalatroDebug] NTopBar.Initialize keeping modifiers visible=[{string.Join(", ", state.Modifiers.Select(static m => m.Id.Entry))}]");
        }

        return true;
    }

    [HarmonyFinalizer]
    public static Exception? Finalizer(IRunState runState, Exception? __exception)
    {
        if (runState is RunState state)
        {
            MainFile.Logger.Info(
                $"[BalatroDebug] NTopBar.Initialize completed with modifiers visible=[{string.Join(", ", state.Modifiers.Select(static m => m.Id.Entry))}] exception={__exception?.GetType().Name ?? "null"}");
        }
        return __exception;
    }
}
