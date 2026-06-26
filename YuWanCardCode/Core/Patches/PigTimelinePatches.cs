using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;
using YuWanCard.Timeline;
using YuWanCard.Timeline.Epochs;

namespace YuWanCard.Core.Patches;

[HarmonyPatch]
public static class PigTimelinePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NeowEpoch), nameof(NeowEpoch.GetTimelineExpansion))]
    private static void Postfix_NeowEpochGetTimelineExpansion(ref EpochModel[] __result)
    {
        PigTimelineRegistry.EnsureRegistered();

        if (__result.Any(epoch => epoch.Id == Pig1Epoch.EpochId))
        {
            return;
        }

        __result = [.. __result, EpochModel.Get(Pig1Epoch.EpochId)];
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProgressState), nameof(ProgressState.FromSerializable))]
    private static void Prefix_ProgressStateFromSerializable()
    {
        PigTimelineRegistry.EnsureRegistered();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NTimelineScreen), nameof(NTimelineScreen.OnSubmenuOpened))]
    private static void Prefix_OnTimelineOpened()
    {
        PigTimelineRegistry.EnsureRegistered();
        PigTimelineUnlockHelper.EnsurePigRootSlotAvailable(SaveManager.Instance.Progress);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EpochModel), nameof(EpochModel.PackedPortraitPath), MethodType.Getter)]
    private static bool Prefix_EpochPackedPortraitPath(EpochModel __instance, ref string __result)
    {
        if (__instance is not PigEpochBase pigEpoch)
        {
            return true;
        }

        __result = pigEpoch.CustomPackedPortraitPath;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EpochModel), nameof(EpochModel.ResolvedPortraitPath), MethodType.Getter)]
    private static bool Prefix_EpochResolvedPortraitPath(EpochModel __instance, ref string __result)
    {
        if (__instance is not PigEpochBase pigEpoch)
        {
            return true;
        }

        __result = pigEpoch.CustomResolvedPortraitPath;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EpochModel), nameof(EpochModel.IsArtPlaceholder), MethodType.Getter)]
    private static bool Prefix_EpochIsArtPlaceholder(EpochModel __instance, ref bool __result)
    {
        if (__instance is not PigEpochBase pigEpoch)
        {
            return true;
        }

        __result = pigEpoch.UsesPlaceholderPortrait;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.UpdateAfterCombatWon))]
    private static void Postfix_UpdateAfterCombatWon(MegaCrit.Sts2.Core.Entities.Players.Player localPlayer, MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        PigTimelineRegistry.EnsureRegistered();

        if (!PigTimelineUnlockHelper.IsPigCharacter(localPlayer.Character))
        {
            return;
        }

        bool changed = false;

        if (room.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Boss)
        {
            EpochModel? actEpoch = localPlayer.RunState.CurrentActIndex switch
            {
                0 => EpochModel.Get(Pig2Epoch.EpochId),
                1 => EpochModel.Get(Pig3Epoch.EpochId),
                2 => EpochModel.Get(Pig4Epoch.EpochId),
                _ => null
            };

            if (actEpoch != null)
            {
                changed |= PigTimelineUnlockHelper.TryObtainMidRun(actEpoch, localPlayer);
            }

            if (PigTimelineUnlockHelper.CountBossVictories(localPlayer.Character.Id) >= 15)
            {
                changed |= PigTimelineUnlockHelper.TryObtainMidRun(EpochModel.Get(Pig6Epoch.EpochId), localPlayer);
            }
        }
        else if (room.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Elite)
        {
            if (PigTimelineUnlockHelper.CountEliteVictories(localPlayer.Character.Id) >= 15)
            {
                changed |= PigTimelineUnlockHelper.TryObtainMidRun(EpochModel.Get(Pig5Epoch.EpochId), localPlayer);
            }
        }

        if (changed)
        {
            SaveManager.Instance.SaveProgressFile();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.UpdateWithRunData))]
    private static void Postfix_UpdateWithRunData(SerializableRun serializableRun, bool victory)
    {
        PigTimelineRegistry.EnsureRegistered();

        if (!PigTimelineUnlockHelper.TryGetLocalSerializablePlayer(serializableRun, out SerializablePlayer serializablePlayer))
        {
            return;
        }

        if (serializablePlayer.CharacterId == null)
        {
            return;
        }

        if (!PigTimelineUnlockHelper.IsPigCharacter(serializablePlayer.CharacterId))
        {
            return;
        }

        bool changed = PigTimelineUnlockHelper.TryObtainPostRun(
            EpochModel.Get(Pig1Epoch.EpochId),
            serializablePlayer,
            serializableRun);

        if (victory && serializableRun.Ascension == 1)
        {
            changed |= PigTimelineUnlockHelper.TryObtainPostRun(
                EpochModel.Get(Pig7Epoch.EpochId),
                serializablePlayer,
                serializableRun);
        }

        if (changed)
        {
            SaveManager.Instance.SaveProgressFile();
        }
    }
}
