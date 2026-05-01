using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

[HarmonyPatch]
public static class ArchitectDialoguePatch
{
    private const string ArchitectEntry = "THE_ARCHITECT";

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.PopulateLocKeys))]
    static void Prefix(AncientDialogueSet __instance, string ancientEntry)
    {
        if (ancientEntry != ArchitectEntry)
            return;

        foreach (var character in ModelDb.AllCharacters)
        {
            if (character is not IYuWanCharacter)
                continue;

            var characterId = character.Id.Entry;
            if (__instance.CharacterDialogues.ContainsKey(characterId))
                continue;

            var dialogues = AncientDialogueUtil.GetDialoguesForKey("ancients", $"{ArchitectEntry}.talk.{characterId}.");
            if (dialogues.Count > 0)
                __instance.CharacterDialogues[characterId] = dialogues;
        }
    }
}
