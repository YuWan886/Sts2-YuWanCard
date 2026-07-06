using YuWanCard.Encounters;
using YuWanCard.Events;
using YuWanCard.Ancients;
using YuWanCard.Core;

namespace YuWanCard.Config;

public readonly record struct YuWanContentSettingsSnapshot(
    bool EnablePigRewardAllCardPools,
    bool EnableYuWanEnemyEncounters,
    bool EnableIgnisBossEncounter,
    bool EnableKillerEliteEncounter,
    bool EnableYuWanEvents,
    bool EnablePigPigAncient,
    bool EnableBlacksmithEvent,
    bool EnableHelloHumanEvent,
    bool EnableHorizonEvent,
    bool EnableSkullGoldRushEvent,
    bool EnableSunkenStatueQuestEvent,
    bool EnableZhiZhanZhiShangEvent,
    IReadOnlyDictionary<string, bool> EnabledColorlessCards)
{
    public static YuWanContentSettingsSnapshot AllDisabled { get; } = new(
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        YuWanColorlessCardCatalog.Cards.ToDictionary(static definition => definition.Key, static _ => false,
            StringComparer.Ordinal));

    public static YuWanContentSettingsSnapshot CaptureLocal()
    {
        return new YuWanContentSettingsSnapshot(
            YuWanCardConfig.EnablePigRewardAllCardPools,
            YuWanCardConfig.EnableYuWanEnemyEncounters,
            YuWanCardConfig.EnableIgnisBossEncounter,
            YuWanCardConfig.EnableKillerEliteEncounter,
            YuWanCardConfig.EnableYuWanEvents,
            YuWanCardConfig.EnablePigPigAncient,
            YuWanCardConfig.EnableBlacksmithEvent,
            YuWanCardConfig.EnableHelloHumanEvent,
            YuWanCardConfig.EnableHorizonEvent,
            YuWanCardConfig.EnableSkullGoldRushEvent,
            YuWanCardConfig.EnableSunkenStatueQuestEvent,
            YuWanCardConfig.EnableZhiZhanZhiShangEvent,
            YuWanColorlessCardSettings.SnapshotStates());
    }

    public bool ContentEquals(in YuWanContentSettingsSnapshot other)
    {
        return EnablePigRewardAllCardPools == other.EnablePigRewardAllCardPools
               && EnableYuWanEnemyEncounters == other.EnableYuWanEnemyEncounters
               && EnableIgnisBossEncounter == other.EnableIgnisBossEncounter
               && EnableKillerEliteEncounter == other.EnableKillerEliteEncounter
               && EnableYuWanEvents == other.EnableYuWanEvents
               && EnablePigPigAncient == other.EnablePigPigAncient
               && EnableBlacksmithEvent == other.EnableBlacksmithEvent
               && EnableHelloHumanEvent == other.EnableHelloHumanEvent
               && EnableHorizonEvent == other.EnableHorizonEvent
               && EnableSkullGoldRushEvent == other.EnableSkullGoldRushEvent
               && EnableSunkenStatueQuestEvent == other.EnableSunkenStatueQuestEvent
               && EnableZhiZhanZhiShangEvent == other.EnableZhiZhanZhiShangEvent
               && ColorlessCardStatesEqual(EnabledColorlessCards, other.EnabledColorlessCards);
    }

    public bool IsEncounterTypeEnabled(Type encounterType)
    {
        if (!typeof(IYuWanContent).IsAssignableFrom(encounterType))
        {
            return true;
        }

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
        if (!typeof(IYuWanContent).IsAssignableFrom(eventType))
        {
            return true;
        }

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

    public bool IsAncientTypeEnabled(Type ancientType)
    {
        if (!typeof(IYuWanContent).IsAssignableFrom(ancientType))
        {
            return true;
        }

        if (ancientType == typeof(PigPig))
        {
            return EnablePigPigAncient;
        }

        return true;
    }

    public bool IsColorlessCardTypeEnabled(Type cardType)
    {
        if (!YuWanColorlessCardCatalog.TryGetDefinition(cardType, out var definition))
        {
            return true;
        }

        return EnabledColorlessCards.GetValueOrDefault(definition.Key, true);
    }

    private static bool ColorlessCardStatesEqual(
        IReadOnlyDictionary<string, bool> left,
        IReadOnlyDictionary<string, bool> right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left == null || right == null)
        {
            return left == right;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var (key, enabled) in left)
        {
            if (!right.TryGetValue(key, out bool otherEnabled) || otherEnabled != enabled)
            {
                return false;
            }
        }

        return true;
    }
}
