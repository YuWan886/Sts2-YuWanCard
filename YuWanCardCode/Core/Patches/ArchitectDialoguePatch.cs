using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;
using YuWanCard.Core.Utils;

namespace YuWanCard.Core.Patches;

[HarmonyPatch]
public static class ArchitectDialoguePatch
{
    static bool Prepare()
    {
        return !OperatingSystem.IsAndroid();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TheArchitect), nameof(TheArchitect.DefineDialogues))]
    static void AddPigDialogue(ref AncientDialogueSet __result)
    {
        var pigId = "YUWANCARD-PIG";
        if (__result.CharacterDialogues.ContainsKey(pigId))
            return;

        var dialogues = AncientDialogueUtil.GetDialoguesForKey("ancients", "THE_ARCHITECT.talk.YUWANCARD-PIG.");
        if (dialogues.Count > 0)
            __result.CharacterDialogues[pigId] = dialogues;
    }
}
