using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;
using YuWanCard.Characters;
using YuWanCard.Timeline.Epochs;

namespace YuWanCard.Timeline;

internal static class PigTimelineUnlockHelper
{
    public static bool IsPigCharacter(CharacterModel? character)
    {
        return character is Pig;
    }

    public static bool IsPigCharacter(ModelId characterId)
    {
        CharacterModel? character = ModelDb.GetByIdOrNull<CharacterModel>(characterId);
        return IsPigCharacter(character);
    }

    public static bool EnsurePigRootSlotAvailable(ProgressState progress)
    {
        PigTimelineRegistry.EnsureRegistered();

        string neowId = EpochModel.GetId<NeowEpoch>();
        if (progress.Epochs.All(epoch => epoch.Id != neowId))
        {
            return false;
        }

        SerializableEpoch? pigEpoch = progress.Epochs.FirstOrDefault(epoch => epoch.Id == Pig1Epoch.EpochId);
        if (pigEpoch is { State: not EpochState.ObtainedNoSlot } || pigEpoch?.State == EpochState.NotObtained)
        {
            return false;
        }

        progress.UnlockSlot(Pig1Epoch.EpochId);
        return true;
    }

    public static bool TryObtainMidRun(EpochModel epoch, Player localPlayer)
    {
        PigTimelineRegistry.EnsureRegistered();

        if (localPlayer.RunState.GameMode.AreAchievementsAndEpochsLocked())
        {
            return false;
        }

        if (epoch.Id == Pig1Epoch.EpochId)
        {
            EnsurePigRootSlotAvailable(SaveManager.Instance.Progress);
        }

        if (SaveManager.Instance.Progress.IsEpochObtained(epoch.Id))
        {
            return false;
        }

        SaveManager.Instance.Progress.ObtainEpoch(epoch.Id);
        localPlayer.DiscoveredEpochs.Add(epoch.Id);
        NGame.Instance?.AddChildSafely(NGainEpochVfx.Create(epoch));
        return true;
    }

    public static bool TryObtainPostRun(EpochModel epoch, SerializablePlayer serializablePlayer, SerializableRun serializableRun)
    {
        PigTimelineRegistry.EnsureRegistered();

        if (serializableRun.GameMode.AreAchievementsAndEpochsLocked())
        {
            return false;
        }

        if (epoch.Id == Pig1Epoch.EpochId)
        {
            EnsurePigRootSlotAvailable(SaveManager.Instance.Progress);
        }

        if (SaveManager.Instance.Progress.IsEpochObtained(epoch.Id))
        {
            return false;
        }

        SaveManager.Instance.Progress.ObtainEpoch(epoch.Id);
        serializablePlayer.DiscoveredEpochs.Add(epoch.Id);
        return true;
    }

    public static bool TryGetLocalSerializablePlayer(SerializableRun serializableRun, out SerializablePlayer serializablePlayer)
    {
        if (serializableRun.Players.Count == 1)
        {
            serializablePlayer = serializableRun.Players[0];
            return true;
        }

        ulong localPlayerId = PlatformUtil.GetLocalPlayerId(serializableRun.PlatformType);
        SerializablePlayer? localPlayer = serializableRun.Players.FirstOrDefault(player => player.NetId == localPlayerId);
        if (localPlayer == null)
        {
            serializablePlayer = null!;
            return false;
        }

        serializablePlayer = localPlayer;
        return true;
    }

    public static int CountEliteVictories(ModelId characterId)
    {
        HashSet<ModelId> eliteEncounterIds = ModelDb.Acts
            .SelectMany(act => act.AllEncounters)
            .Where(encounter => encounter.RoomType == RoomType.Elite)
            .Select(encounter => encounter.Id)
            .ToHashSet();

        return CountEncounterVictories(characterId, eliteEncounterIds);
    }

    public static int CountBossVictories(ModelId characterId)
    {
        HashSet<ModelId> bossEncounterIds = ModelDb.Acts
            .SelectMany(act => act.AllBossEncounters)
            .Select(encounter => encounter.Id)
            .ToHashSet();

        return CountEncounterVictories(characterId, bossEncounterIds);
    }

    private static int CountEncounterVictories(ModelId characterId, HashSet<ModelId> encounterIds)
    {
        int wins = 0;

        foreach (EncounterStats encounterStats in SaveManager.Instance.Progress.EncounterStats.Values)
        {
            if (!encounterIds.Contains(encounterStats.Id))
            {
                continue;
            }

            FightStats? fightStats = encounterStats.FightStats.FirstOrDefault(fight => fight.Character == characterId);
            if (fightStats != null)
            {
                wins += fightStats.Wins;
            }
        }

        return wins;
    }
}
