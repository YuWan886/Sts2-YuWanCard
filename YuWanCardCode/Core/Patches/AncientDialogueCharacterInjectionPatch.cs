using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Injects localization-defined ancient dialogues for IYuWanCharacter
/// characters into any AncientDialogueSet before PopulateLocKeys runs.
/// Mirrors RitsuLib's AncientDialoguePopulateLocKeysPatch approach.
/// </summary>
[HarmonyPatch]
public static class AncientDialogueCharacterInjectionPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.PopulateLocKeys))]
    static void Prefix(AncientDialogueSet __instance, string ancientEntry)
    {
        foreach (var character in ModelDb.AllCharacters)
        {
            if (character is not IYuWanCharacter)
                continue;

            var characterId = character.Id.Entry;
            if (__instance.CharacterDialogues.ContainsKey(characterId))
                continue;

            var dialogues = AncientDialogueUtil.GetDialoguesForKey("ancients", $"{ancientEntry}.talk.{characterId}.");
            if (dialogues.Count > 0)
                __instance.CharacterDialogues[characterId] = dialogues;
        }
    }
}

/// <summary>
/// Prevents NPE in TheArchitect.WinRun when no valid dialogue is found.
/// Uses [HarmonyPatch] for auto-application; the try/catch in stub
/// creation handles runtime failures on IL2CPP platforms.
/// </summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Events.TheArchitect), "LoadDialogue")]
public static class ArchitectLoadDialogueNullGuard
{
    private static readonly FieldInfo? DialogueField =
        typeof(MegaCrit.Sts2.Core.Models.Events.TheArchitect).GetField("_dialogue",
            BindingFlags.Instance | BindingFlags.NonPublic);

    [HarmonyPostfix]
    static void Postfix(object __instance)
    {
        if (DialogueField == null || DialogueField.GetValue(__instance) != null)
            return;

        var stub = CreateSafeDialogueStub();
        if (stub != null)
            DialogueField.SetValue(__instance, stub);
    }

    private static AncientDialogue? CreateSafeDialogueStub()
    {
        try
        {
            var stub = (AncientDialogue)RuntimeHelpers.GetUninitializedObject(typeof(AncientDialogue));

            var linesField = typeof(AncientDialogue)
                .GetField("<Lines>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? FindFieldOfType(typeof(AncientDialogue), typeof(IReadOnlyList<AncientDialogueLine>));

            if (linesField == null)
                return null;

            linesField.SetValue(stub, Array.Empty<AncientDialogueLine>());

            foreach (var fi in typeof(AncientDialogue).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (fi.FieldType == typeof(ArchitectAttackers))
                    fi.SetValue(stub, ArchitectAttackers.None);
            }

            return stub;
        }
        catch
        {
            return null;
        }
    }

    private static FieldInfo? FindFieldOfType(Type type, Type fieldType)
    {
        foreach (var fi in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            if (fi.FieldType == fieldType)
                return fi;
        return null;
    }
}
