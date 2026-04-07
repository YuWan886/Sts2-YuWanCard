using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
public static class NGeneralStatsGridPatch
{
    [HarmonyPostfix]
    public static void Postfix(NGeneralStatsGrid __instance)
    {
        var pigCharacter = ModelDb.Character<Characters.Pig>();
        if (pigCharacter != null)
        {
            ProgressState progressSave = SaveManager.Instance.Progress;
            CharacterStats statsForCharacter = progressSave.GetOrCreateCharacterStats(pigCharacter.Id);
            NCharacterStats child = NCharacterStats.Create(statsForCharacter);
            Node container = __instance.GetNode("%CharacterStatsContainer");
            container.AddChild(child);
        }
    }
}