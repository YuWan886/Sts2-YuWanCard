using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

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
