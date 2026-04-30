using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
public static class NGeneralStatsGridPatch
{
    [HarmonyPostfix]
    public static void Postfix(NGeneralStatsGrid __instance)
    {
        var container = __instance.GetNodeOrNull<Node>("%CharacterStatsContainer");
        if (container == null) return;

        ProgressState progressSave = SaveManager.Instance.Progress;

        foreach (var character in ModelDbCharactersPatch.CustomCharacters)
        {
            if (character is not IYuWanCharacter) continue;

            CharacterStats statsForCharacter = progressSave.GetOrCreateCharacterStats(character.Id);
            NCharacterStats child = NCharacterStats.Create(statsForCharacter);
            container.AddChild(child);
        }
    }
}
