using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NActBanner), "_Ready")]
public static class ActBannerPatch
{
    private static readonly MethodInfo StateGetter = AccessTools.PropertyGetter(typeof(RunManager), "State");
    private static readonly FieldInfo ActNumberField = AccessTools.Field(typeof(NActBanner), "_actNumber");

    [HarmonyPostfix]
    public static void Postfix(NActBanner __instance)
    {
        var state = (RunState?)StateGetter.Invoke(RunManager.Instance, null);
        if (state == null)
        {
            return;
        }

        var endlessModifier = EndlessModifier.GetEndlessModifier(state);
        if (endlessModifier == null || endlessModifier.EffectiveLoopCount <= 0)
        {
            return;
        }

        int baseActIndex = state.CurrentActIndex;
        int loopCount = endlessModifier.EffectiveLoopCount;
        int displayActNumber = baseActIndex + 1 + (loopCount * 3);

        var actNumberLabel = ActNumberField.GetValue(__instance);
        if (actNumberLabel == null)
        {
            return;
        }

        var setTextMethod = AccessTools.Method(actNumberLabel.GetType(), "SetTextAutoSize");
        if (setTextMethod != null)
        {
            string displayText = $"阶段 {displayActNumber}";
            setTextMethod.Invoke(actNumberLabel, [displayText]);
        }

        MainFile.Logger.Info($"Endless mode: Displaying Act {displayActNumber} (base: {baseActIndex + 1}, loop: {loopCount})");
    }
}
