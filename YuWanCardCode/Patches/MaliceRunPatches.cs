using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
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

        var player = __instance.State.Players.FirstOrDefault(p => p.NetId == __instance.NetService.NetId)
            ?? __instance.State.Players.FirstOrDefault();
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

        if (__result.Players.Count > 1)
        {
            return;
        }

        var localPlayer = __result.Players.FirstOrDefault();
        if (localPlayer == null)
        {
            return;
        }

        MaliceManager.EnsureConsistency(localPlayer.Character.Id);

        int level = MaliceManager.GetPreferredMalice(localPlayer.Character.Id);
        var modifiers = MaliceModifierPatchHelpers.EnsureMaliceModifier(__result.Modifiers, level);
        if (!ReferenceEquals(modifiers, __result.Modifiers))
        {
            YuWanReflectionHelper.SetPrivateField(__result, "<Modifiers>k__BackingField", modifiers);
        }
    }
}

public static class MaliceModifierPatchHelpers
{
    private sealed class PendingModifiersBox
    {
        public IReadOnlyList<ModifierModel>? Value { get; set; }
    }

    private static readonly ConditionalWeakTable<StartRunLobby, PendingModifiersBox> PendingModifiers = [];

    public static IReadOnlyList<ModifierModel> EnsureMaliceModifier(IReadOnlyList<ModifierModel> modifiers, int level)
    {
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
            : CreateMaliceModifier(level);

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

    public static int GetMaliceLevel(IReadOnlyList<ModifierModel>? modifiers)
    {
        if (modifiers == null)
        {
            return 0;
        }

        return modifiers.OfType<MaliceModifier>().FirstOrDefault()?.EffectiveMaliceLevel ?? 0;
    }

    public static bool MatchesMaliceLevel(IReadOnlyList<ModifierModel>? modifiers, int level)
    {
        var existing = modifiers?.OfType<MaliceModifier>().FirstOrDefault();
        if (level <= 0)
        {
            return existing == null;
        }

        return existing?.EffectiveMaliceLevel == level;
    }

    public static List<ModifierModel> CloneModifiers(IReadOnlyList<ModifierModel> modifiers)
    {
        return modifiers.Select(modifier => ModifierModel.FromSerializable(modifier.ToSerializable())).ToList();
    }

    public static MaliceModifier CreateMaliceModifier(int level)
    {
        var modifier = (MaliceModifier)ModelDb.GetById<ModifierModel>(ModelDb.GetId<MaliceModifier>()).ToMutable();
        modifier.YuWanCard_MaliceLevel = level;
        return modifier;
    }

    public static void SetPendingRunModifiers(StartRunLobby lobby, IReadOnlyList<ModifierModel> modifiers)
    {
        PendingModifiers.GetOrCreateValue(lobby).Value = CloneModifiers(modifiers);
    }

    public static IReadOnlyList<ModifierModel>? TakePendingRunModifiers(StartRunLobby lobby)
    {
        if (!PendingModifiers.TryGetValue(lobby, out var box))
        {
            return null;
        }

        var modifiers = box.Value;
        box.Value = null;
        return modifiers;
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame.StartNewMultiplayerRun))]
public static class MaliceStartNewMultiplayerRunPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref IReadOnlyList<ModifierModel> modifiers, StartRunLobby lobby)
    {
        if (lobby.GameMode != GameMode.Standard)
        {
            return;
        }

        if (MaliceModifierPatchHelpers.TakePendingRunModifiers(lobby) is { Count: > 0 } pendingModifiers)
        {
            modifiers = pendingModifiers;
            return;
        }

        if (lobby.Modifiers.Count > 0)
        {
            modifiers = MaliceModifierPatchHelpers.CloneModifiers(lobby.Modifiers);
        }
    }
}
