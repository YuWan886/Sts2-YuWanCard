using System.Reflection;
using System.Runtime.CompilerServices;
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

/// <summary>
/// Prevents NPE in TheArchitect.WinRun when no valid dialogue is found.
/// Applied manually (not via [HarmonyPatch]) for Android DMD safety.
/// </summary>
public static class ArchitectLoadDialogueNullGuard
{
    private static readonly FieldInfo? DialogueField =
        typeof(MegaCrit.Sts2.Core.Models.Events.TheArchitect).GetField("_dialogue",
            BindingFlags.Instance | BindingFlags.NonPublic);

    public static void ApplyPatch(Harmony harmony)
    {
        var target = AccessTools.Method(typeof(MegaCrit.Sts2.Core.Models.Events.TheArchitect), "LoadDialogue");
        if (target == null) return;

        var postfix = AccessTools.Method(typeof(ArchitectLoadDialogueNullGuard), nameof(Postfix));
        harmony.Patch(target, postfix: new HarmonyMethod(postfix));
    }

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
