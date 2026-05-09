using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace YuWanCard.Utils;

public static class AudioUtils
{
    private static readonly Dictionary<string, AudioStream> AudioCache = new();

    public static void Play(string audioPath, string bus = "Master", float volume = 1f)
    {
        if (string.IsNullOrEmpty(audioPath))
        {
            MainFile.Logger.Warn("AudioUtils: Audio path is null or empty");
            return;
        }

        var container = GetPlaybackContainer();
        if (container == null)
        {
            MainFile.Logger.Warn("AudioUtils: No playback container available");
            return;
        }

        if (!AudioCache.TryGetValue(audioPath, out var audioStream))
        {
            audioStream = GD.Load<AudioStream>(audioPath);
            if (audioStream == null)
            {
                MainFile.Logger.Warn($"AudioUtils: Failed to load audio: {audioPath}");
                return;
            }
            AudioCache[audioPath] = audioStream;
            MainFile.Logger.Debug($"AudioUtils: Cached audio: {audioPath}");
        }

        var effectiveVolume = Mathf.Max(0f, volume * GetSfxVolumeScale());

        var audioPlayer = new AudioStreamPlayer
        {
            Stream = audioStream,
            Bus = bus,
            VolumeLinear = effectiveVolume
        };

        container.AddChildSafely(audioPlayer);
        audioPlayer.Finished += () => OnAudioFinished(audioPlayer, audioPath);
        audioPlayer.Play();

        MainFile.Logger.Debug($"AudioUtils: Playing audio: {audioPath}");
    }

    private static void OnAudioFinished(AudioStreamPlayer player, string audioPath)
    {
        if (GodotObject.IsInstanceValid(player))
        {
            player.QueueFree();
            MainFile.Logger.Debug($"AudioUtils: Freed audio player for: {audioPath}");
        }
    }

    public static void ClearCache()
    {
        AudioCache.Clear();
        MainFile.Logger.Info("AudioUtils: Audio cache cleared");
    }

    public static void RemoveFromCache(string audioPath)
    {
        if (AudioCache.Remove(audioPath))
        {
            MainFile.Logger.Debug($"AudioUtils: Removed from cache: {audioPath}");
        }
    }

    public static int CachedCount => AudioCache.Count;

    private static float GetSfxVolumeScale()
    {
        return SaveManager.Instance?.SettingsSave?.VolumeSfx ?? 1f;
    }

    private static Node? GetPlaybackContainer()
    {
        if (NCombatRoom.Instance?.CombatVfxContainer is { } combatVfxContainer)
        {
            return combatVfxContainer;
        }

        if (NGame.Instance?.RootSceneContainer?.CurrentScene is { } currentScene)
        {
            return currentScene;
        }

        return NGame.Instance;
    }
}
