using YuWanCard.Encounters;
using YuWanCard.Events;

namespace YuWanCard.Config;

public readonly record struct YuWanContentSettingsSnapshot(
    bool EnableYuWanEnemyEncounters,
    bool EnableIgnisBossEncounter,
    bool EnableKillerEliteEncounter,
    bool EnableYuWanEvents,
    bool EnableBlacksmithEvent,
    bool EnableHelloHumanEvent,
    bool EnableHorizonEvent,
    bool EnableSkullGoldRushEvent,
    bool EnableSunkenStatueQuestEvent,
    bool EnableZhiZhanZhiShangEvent)
{
    public static YuWanContentSettingsSnapshot CaptureLocal()
    {
        return new YuWanContentSettingsSnapshot(
            YuWanCardConfig.EnableYuWanEnemyEncounters,
            YuWanCardConfig.EnableIgnisBossEncounter,
            YuWanCardConfig.EnableKillerEliteEncounter,
            YuWanCardConfig.EnableYuWanEvents,
            YuWanCardConfig.EnableBlacksmithEvent,
            YuWanCardConfig.EnableHelloHumanEvent,
            YuWanCardConfig.EnableHorizonEvent,
            YuWanCardConfig.EnableSkullGoldRushEvent,
            YuWanCardConfig.EnableSunkenStatueQuestEvent,
            YuWanCardConfig.EnableZhiZhanZhiShangEvent);
    }

    public bool IsEncounterTypeEnabled(Type encounterType)
    {
        if (!EnableYuWanEnemyEncounters)
        {
            return false;
        }

        if (encounterType == typeof(IgnisBoss))
        {
            return EnableIgnisBossEncounter;
        }

        if (encounterType == typeof(KillerElite))
        {
            return EnableKillerEliteEncounter;
        }

        return true;
    }

    public bool IsEventTypeEnabled(Type eventType)
    {
        if (!EnableYuWanEvents)
        {
            return false;
        }

        if (eventType == typeof(Blacksmith))
        {
            return EnableBlacksmithEvent;
        }

        if (eventType == typeof(HelloHuman))
        {
            return EnableHelloHumanEvent;
        }

        if (eventType == typeof(HorizonEvent))
        {
            return EnableHorizonEvent;
        }

        if (eventType == typeof(SkullGoldRush))
        {
            return EnableSkullGoldRushEvent;
        }

        if (eventType == typeof(SunkenStatueQuest))
        {
            return EnableSunkenStatueQuestEvent;
        }

        if (eventType == typeof(ZhiZhanZhiShang))
        {
            return EnableZhiZhanZhiShangEvent;
        }

        return true;
    }
}
