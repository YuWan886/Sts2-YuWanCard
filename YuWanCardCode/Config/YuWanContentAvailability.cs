using MegaCrit.Sts2.Core.Models;
using YuWanCard.Multiplayer;

namespace YuWanCard.Config;

internal static class YuWanContentAvailability
{
    public static bool IsEncounterTypeEnabled<TEncounter>() where TEncounter : EncounterModel
        => IsEncounterTypeEnabled(typeof(TEncounter));

    public static bool IsEncounterTypeEnabled(Type encounterType)
    {
        return GetEffectiveSnapshot().IsEncounterTypeEnabled(encounterType);
    }

    public static bool IsEventTypeEnabled<TEvent>() where TEvent : EventModel
        => IsEventTypeEnabled(typeof(TEvent));

    public static bool IsEventTypeEnabled(Type eventType)
    {
        return GetEffectiveSnapshot().IsEventTypeEnabled(eventType);
    }

    private static YuWanContentSettingsSnapshot GetEffectiveSnapshot()
    {
        return YuWanContentSettingsSync.TryGetClientAuthoritativeSnapshot(out var snapshot)
            ? snapshot
            : YuWanContentSettingsSnapshot.CaptureLocal();
    }
}
