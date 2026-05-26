using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Malice;
using YuWanCard.Modifiers;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(RunManager), "OnEnded", [typeof(bool)])]
public static class MaliceRunEndedPatch
{
    [HarmonyPrefix]
    public static void Prefix(RunManager __instance, bool isVictory)
    {
        if (!isVictory || __instance.State == null || __instance.State.GameMode != GameMode.Standard)
        {
            return;
        }

        var player = __instance.State.Players.FirstOrDefault();
        var modifier = MaliceModifier.GetMaliceModifier(__instance.State);
        if (player == null || modifier == null || modifier.EffectiveMaliceLevel <= 0)
        {
            return;
        }

        MaliceManager.TryIncrementMalice(player.Character.Id, modifier.EffectiveMaliceLevel);
    }
}

[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
public static class MaliceRunStateCreatePatch
{
    [HarmonyPostfix]
    public static void Postfix(RunState __result)
    {
        if (__result.GameMode != GameMode.Standard)
        {
            return;
        }

        var localPlayer = __result.Players.FirstOrDefault();
        if (localPlayer == null)
        {
            return;
        }

        MaliceManager.EnsureConsistency(localPlayer.Character.Id);

        var modifiers = MaliceModifierPatchHelpers.EnsureMaliceModifier(localPlayer.Character, __result.Modifiers);
        if (!ReferenceEquals(modifiers, __result.Modifiers))
        {
            YuWanReflectionHelper.SetPrivateField(__result, "<Modifiers>k__BackingField", modifiers);
        }
    }
}

public static class MaliceModifierPatchHelpers
{
    public static IReadOnlyList<ModifierModel> EnsureMaliceModifier(CharacterModel character, IReadOnlyList<ModifierModel> modifiers)
    {
        int level = MaliceManager.GetPreferredMalice(character.Id);
        List<ModifierModel> list = modifiers?.ToList() ?? [];

        int existingIndex = list.FindIndex(m => m is MaliceModifier);
        if (level <= 0)
        {
            if (existingIndex >= 0)
            {
                list.RemoveAt(existingIndex);
            }

            return list;
        }

        var modifier = existingIndex >= 0
            ? (MaliceModifier)list[existingIndex]
            : (MaliceModifier)ModelDb.GetById<ModifierModel>(ModelDb.GetId<MaliceModifier>()).ToMutable();

        modifier.YuWanCard_MaliceLevel = level;

        if (existingIndex >= 0)
        {
            list[existingIndex] = modifier;
        }
        else
        {
            list.Add(modifier);
        }

        return list;
    }
}
