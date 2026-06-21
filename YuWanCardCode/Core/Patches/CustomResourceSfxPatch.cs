using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Audio;
using YuWanCard.Utils;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Routes mod-local audio resources through AudioUtils, because SfxCmd only
/// forwards FMOD-style event paths to the game's audio manager.
/// </summary>
[HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.Play), typeof(string), typeof(float))]
public static class CustomResourceSfxPatch
{
    [HarmonyPrefix]
    public static bool Prefix(string sfx, float volume)
    {
        if (string.IsNullOrWhiteSpace(sfx) ||
            !sfx.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        AudioUtils.Play(sfx, volume: volume);
        return false;
    }
}

/// <summary>
/// Some game UI flows reach the audio manager directly or bypass patched callers
/// due to runtime inlining, so intercept the final one-shot dispatch as well.
/// </summary>
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot), typeof(string), typeof(float))]
public static class CustomResourceAudioManagerSfxPatch
{
    [HarmonyPrefix]
    public static bool Prefix(string path, float volume)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        AudioUtils.Play(path, volume: volume);
        return false;
    }
}

[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot), typeof(string), typeof(Dictionary<string, float>), typeof(float))]
public static class CustomResourceAudioManagerParameterizedSfxPatch
{
    [HarmonyPrefix]
    public static bool Prefix(string path, Dictionary<string, float> parameters, float volume)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        AudioUtils.Play(path, volume: volume);
        return false;
    }
}
