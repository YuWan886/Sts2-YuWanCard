using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace YuWanCard.Malice;

public static class MaliceManager
{
    public const int MaxMaliceLevel = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static MaliceProgressData? _cache;

    public static int GetMaxMalice(ModelId characterId)
    {
        if (characterId == ModelId.none)
        {
            return 0;
        }

        var progress = GetOrCreateCharacterProgress(characterId);
        int ascensionCap = GetAscensionCap(characterId);
        if (progress.MaxMalice <= 0 && ascensionCap > 0)
        {
            progress.MaxMalice = 1;
        }
        progress.MaxMalice = ClampLevel(progress.MaxMalice, ascensionCap);
        progress.PreferredMalice = ClampLevel(progress.PreferredMalice, progress.MaxMalice);
        return progress.MaxMalice;
    }

    public static int GetPreferredMalice(ModelId characterId)
    {
        if (characterId == ModelId.none)
        {
            return 0;
        }

        var progress = GetOrCreateCharacterProgress(characterId);
        int maxMalice = GetMaxMalice(characterId);
        progress.PreferredMalice = ClampLevel(progress.PreferredMalice, maxMalice);
        return progress.PreferredMalice;
    }

    public static void SetPreferredMalice(ModelId characterId, int level)
    {
        if (characterId == ModelId.none)
        {
            return;
        }

        var progress = GetOrCreateCharacterProgress(characterId);
        int maxMalice = GetMaxMalice(characterId);
        int clamped = ClampLevel(level, maxMalice);
        if (progress.PreferredMalice == clamped)
        {
            return;
        }

        progress.PreferredMalice = clamped;
        Save();
    }

    public static int GetAvailableSelectionMax(ModelId characterId)
    {
        if (characterId == ModelId.none)
        {
            return 0;
        }

        int ascensionCap = GetAscensionCap(characterId);
        int unlocked = GetMaxMalice(characterId);
        return Math.Min(ascensionCap, unlocked);
    }

    public static bool TryIncrementMalice(ModelId characterId, int currentLevel)
    {
        if (characterId == ModelId.none || currentLevel <= 0)
        {
            return false;
        }

        var progress = GetOrCreateCharacterProgress(characterId);
        int ascensionCap = GetAscensionCap(characterId);
        int currentMax = ClampLevel(progress.MaxMalice, ascensionCap);
        if (currentLevel != currentMax || currentMax >= ascensionCap || currentMax >= MaxMaliceLevel)
        {
            return false;
        }

        progress.MaxMalice = ClampLevel(currentMax + 1, ascensionCap);
        progress.PreferredMalice = progress.MaxMalice;
        Save();
        MainFile.Logger.Info($"MaliceManager: unlocked malice {progress.MaxMalice} for {characterId}");
        return true;
    }

    public static void EnsureConsistency(ModelId characterId)
    {
        if (characterId == ModelId.none)
        {
            return;
        }

        var progress = GetOrCreateCharacterProgress(characterId);
        int ascensionCap = GetAscensionCap(characterId);
        int newMax = ClampLevel(progress.MaxMalice, ascensionCap);
        int newPreferred = ClampLevel(progress.PreferredMalice, newMax);
        if (progress.MaxMalice == newMax && progress.PreferredMalice == newPreferred)
        {
            return;
        }

        progress.MaxMalice = newMax;
        progress.PreferredMalice = newPreferred;
        Save();
    }

    public static int UnlockAllMalice()
    {
        int unlocked = 0;
        foreach (var character in ModelDb.AllCharacters)
        {
            if (UnlockCharacterMalice(character.Id))
            {
                unlocked++;
            }
        }

        if (unlocked > 0)
        {
            Save();
            MainFile.Logger.Info($"MaliceManager: unlocked max malice for {unlocked} characters");
        }

        return unlocked;
    }

    public static bool UnlockCharacterMalice(ModelId characterId, int level = MaxMaliceLevel)
    {
        if (characterId == ModelId.none)
        {
            return false;
        }

        var progress = GetOrCreateCharacterProgress(characterId);
        int ascensionCap = GetAscensionCap(characterId);
        int targetLevel = ClampLevel(level, ascensionCap);
        if (targetLevel <= 0)
        {
            return false;
        }

        if (progress.MaxMalice == targetLevel && progress.PreferredMalice == targetLevel)
        {
            return false;
        }

        progress.MaxMalice = targetLevel;
        progress.PreferredMalice = targetLevel;
        return true;
    }

    private static MaliceCharacterProgress GetOrCreateCharacterProgress(ModelId characterId)
    {
        var data = Load();
        string key = characterId.ToString();
        if (data.Characters.TryGetValue(key, out var progress))
        {
            return progress;
        }

        progress = new MaliceCharacterProgress();
        data.Characters[key] = progress;
        return progress;
    }

    private static int GetAscensionCap(ModelId characterId)
    {
        var stats = SaveManager.Instance.Progress.GetStatsForCharacter(characterId);
        return ClampLevel(stats?.MaxAscension ?? 0, MaxMaliceLevel);
    }

    private static int ClampLevel(int level, int max)
    {
        return Math.Clamp(level, 0, Math.Max(0, Math.Min(MaxMaliceLevel, max)));
    }

    private static MaliceProgressData Load()
    {
        if (_cache != null)
        {
            return _cache;
        }

        try
        {
            string path = GetResolvedSavePath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _cache = JsonSerializer.Deserialize<MaliceProgressData>(json, JsonOptions) ?? new MaliceProgressData();
            }
            else
            {
                _cache = new MaliceProgressData();
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"MaliceManager: failed to load malice progress: {ex}");
            _cache = new MaliceProgressData();
        }

        return _cache;
    }

    private static void Save()
    {
        try
        {
            string path = GetResolvedSavePath();
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(Load(), JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"MaliceManager: failed to save malice progress: {ex}");
        }
    }

    private static string GetResolvedSavePath()
    {
        string relative = Path.Combine(UserDataPathProvider.GetProfileDir(SaveManager.Instance.CurrentProfileId), UserDataPathProvider.SavesDir, "malice_progress.json");
        return ProjectSettings.GlobalizePath($"user://{relative.Replace('\\', '/')}");
    }
}
