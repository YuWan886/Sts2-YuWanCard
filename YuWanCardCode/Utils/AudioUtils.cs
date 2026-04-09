using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Utils;

public static class AudioUtils
{
    private static readonly Dictionary<string, AudioStream> AudioCache = new();

    public static void Play(string audioPath, string bus = "SFX")
    {
        if (string.IsNullOrEmpty(audioPath))
        {
            MainFile.Logger.Warn("AudioUtils: Audio path is null or empty");
            return;
        }

        var container = NCombatRoom.Instance?.CombatVfxContainer;
        if (container == null)
        {
            MainFile.Logger.Warn("AudioUtils: CombatVfxContainer not available");
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

        var audioPlayer = new AudioStreamPlayer
        {
            Stream = audioStream,
            Bus = bus
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
}
