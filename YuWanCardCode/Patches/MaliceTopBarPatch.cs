using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;
using YuWanCard.Modifiers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NTopBarPortraitTip), "OnFocus")]
public static class MaliceTopBarPortraitTipPatch
{
    [HarmonyPostfix]
    public static void Postfix(NTopBarPortraitTip __instance)
    {
        RunState? runState = GetCurrentRunState();
        if (runState == null)
        {
            return;
        }

        var modifier = MaliceModifier.GetMaliceModifier(runState);
        if (modifier == null || modifier.EffectiveMaliceLevel <= 0)
        {
            return;
        }

        NHoverTipSet.Remove(__instance);

        bool achievementsLocked = runState.GameMode.AreAchievementsAndEpochsLocked();
        var localPlayer = LocalContext.GetMe(runState);
        if (localPlayer?.Character == null)
        {
            return;
        }

        var character = localPlayer.Character;
        var ascensionTip = AscensionHelper.GetHoverTip(character, runState.AscensionLevel, achievementsLocked);
        var maliceTip = MaliceModifier.GetHoverTip(modifier.EffectiveMaliceLevel);

        var hoverTips = new IHoverTip[] { ascensionTip, maliceTip };
        var tipSet = NHoverTipSet.CreateAndShow(__instance, hoverTips);
        if (tipSet != null)
        {
            tipSet.GlobalPosition = __instance.GlobalPosition + new Vector2(0f, __instance.Size.Y + 20f);
        }
    }

    private static RunState? GetCurrentRunState()
    {
        return AccessTools.Property(typeof(RunManager), "State")?.GetValue(RunManager.Instance) as RunState;
    }
}

[HarmonyPatch(typeof(NTopBarModifier), nameof(NTopBarModifier._Ready))]
public static class MaliceTopBarModifierPatch
{
    [HarmonyPostfix]
    public static void Postfix(NTopBarModifier __instance)
    {
        var modifierField = AccessTools.Field(typeof(NTopBarModifier), "_modifier");
        var hoverTipField = AccessTools.Field(typeof(NTopBarModifier), "_hoverTip");
        if (modifierField?.GetValue(__instance) is not MaliceModifier modifier || hoverTipField == null)
        {
            return;
        }

        hoverTipField.SetValue(__instance, MaliceModifier.GetHoverTip(modifier.EffectiveMaliceLevel));
    }
}
