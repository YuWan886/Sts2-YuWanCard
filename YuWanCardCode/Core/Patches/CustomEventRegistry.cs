using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Registers custom event models so they appear in the game.
/// Shared events (Acts.Length == 0) are injected into ModelDb.AllSharedEvents.
/// Act-specific events require per-act-type Harmony patching (not yet implemented).
/// </summary>
public static class CustomEventRegistry
{
    public static readonly List<EventModel> SharedEvents = [];
    public static readonly List<EventModel> ActEvents = [];

    public static void Register(EventModel eventModel)
    {
        if (ContentRegistry.IsFrozen)
        {
            MainFile.Logger.Warn(
                $"CustomEventRegistry: Register called after freeze for {eventModel.GetType().Name}");
            return;
        }

        if (eventModel is not YuWanEventModel yuWanEvent)
        {
            SharedEvents.Add(eventModel);
            return;
        }

        if (yuWanEvent.Acts.Length == 0)
        {
            if (!SharedEvents.Contains(eventModel))
                SharedEvents.Add(eventModel);
        }
        else
        {
            if (!ActEvents.Contains(eventModel))
                ActEvents.Add(eventModel);

            MainFile.Logger.Warn(
                $"CustomEventRegistry: act-specific event '{eventModel.Id.Entry}' registered, but per-act-type Harmony patching is not yet implemented. It will not appear in-game.");
        }
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedEvents), MethodType.Getter)]
static class AllSharedEventsPatch
{
    [HarmonyPostfix]
    static IEnumerable<EventModel> AddCustomEvents(IEnumerable<EventModel> __result)
    {
        return [.. __result, .. CustomEventRegistry.SharedEvents];
    }
}

[HarmonyPriority(Priority.High)]
[HarmonyPatch(typeof(EventModel), nameof(EventModel.CreateInitialPortrait))]
static class CustomEventInitialPortraitPatch
{
    static bool Prefix(EventModel __instance, ref Texture2D __result)
    {
        if (__instance is YuWanEventModel ev)
        {
            var imagePath = ev.GetYuWanEventImagePath();
            if (imagePath != null)
            {
                try
                {
                    __result = PreloadManager.Cache.GetTexture2D(imagePath);
                    return false;
                }
                catch (Exception ex)
                {
                    MainFile.Logger.Warn(
                        $"CustomEventInitialPortrait: Failed to load custom image '{imagePath}' for event '{__instance.Id.Entry}', falling back to default. Error: {ex.Message}");
                }
            }
        }
        return true;
    }
}

[HarmonyPriority(Priority.High)]
[HarmonyPatch(typeof(EventModel), nameof(EventModel.CreateBackgroundScene))]
static class CustomEventBackgroundScenePatch
{
    static bool Prefix(EventModel __instance, ref PackedScene __result)
    {
        if (__instance is YuWanEventModel ev && ev.CustomBackgroundScenePath != null)
        {
            __result = PreloadManager.Cache.GetScene(ev.CustomBackgroundScenePath);
            return false;
        }
        return true;
    }
}

