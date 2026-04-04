using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Characters;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(RunState))]
public static class RunStatePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(RunState.CreateForNewRun))]
    public static void AddPigAllCardsModifier(
        IReadOnlyList<Player> players,
        IReadOnlyList<ActModel> acts,
        ref IReadOnlyList<ModifierModel> modifiers,
        int ascensionLevel,
        string seed)
    {
        foreach (var player in players)
        {
            if (player.Character is Pig)
            {
                var pigAllCardsModifier = ModelDb.Modifier<PigAllCards>().ToMutable();
                var newModifiers = modifiers.ToList();
                newModifiers.Add(pigAllCardsModifier);
                modifiers = newModifiers;
                return;
            }
        }
    }
}
