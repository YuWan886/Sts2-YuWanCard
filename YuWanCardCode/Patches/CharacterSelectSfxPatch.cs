using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

// NOTE: These transpilers coexist with the global NAudioManager.PlayOneShot
// interception in CustomResourceSfxPatch on purpose, not redundantly. The
// vanilla SfxCmd.Play body is guarded by `!CombatManager.Instance.IsEnding`,
// and CombatManager.Instance is null on the character select screens, so
// letting the original call run would throw a NullReferenceException before
// it ever reaches the audio manager. Replacing the SfxCmd.Play call here
// sidesteps that guard entirely; the global PlayOneShot patch only catches
// flows that already reach the audio manager.
internal static class CharacterSelectSfxRouteHelper
{
    public static void Play(string sfx, float volume)
    {
        if (string.Equals(sfx, Pig.PigCharacterSelectSfxPath, StringComparison.OrdinalIgnoreCase))
        {
            AudioUtils.Play(sfx, volume: volume);
            return;
        }

        SfxCmd.Play(sfx, volume);
    }
}

internal static class CharacterSelectSfxTranspiler
{
    private static readonly MethodInfo OriginalPlayMethod =
        AccessTools.Method(typeof(SfxCmd), nameof(SfxCmd.Play), [typeof(string), typeof(float)])!;

    private static readonly MethodInfo ReplacementPlayMethod =
        AccessTools.Method(typeof(CharacterSelectSfxRouteHelper), nameof(CharacterSelectSfxRouteHelper.Play))!;

    public static IEnumerable<CodeInstruction> ReplacePlayCall(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (!instruction.Calls(OriginalPlayMethod))
            {
                yield return instruction;
                continue;
            }

            var replacement = new CodeInstruction(instruction)
            {
                operand = ReplacementPlayMethod
            };
            yield return replacement;
        }
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), "OnLocalCharacterChangedForRandom")]
internal static class CharacterSelectRandomSfxPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => CharacterSelectSfxTranspiler.ReplacePlayCall(instructions);
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class CharacterSelectScreenSfxPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => CharacterSelectSfxTranspiler.ReplacePlayCall(instructions);
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.SelectCharacter))]
internal static class CustomRunCharacterSelectSfxPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => CharacterSelectSfxTranspiler.ReplacePlayCall(instructions);
}

[HarmonyPatch(typeof(NMultiplayerLoadGameScreen), "AfterMultiplayerStarted")]
internal static class MultiplayerLoadGameCharacterSelectSfxPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => CharacterSelectSfxTranspiler.ReplacePlayCall(instructions);
}
