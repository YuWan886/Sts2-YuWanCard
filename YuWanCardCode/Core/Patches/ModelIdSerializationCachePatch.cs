using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using YuWanCard.Timeline;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
static class ModelIdSerializationCachePatch
{
    [HarmonyPrefix]
    static void RegisterTimelineContent()
    {
        PigTimelineRegistry.EnsureRegistered();
    }
}
