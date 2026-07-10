using YuWanCard.Encounters;
using YuWanCard.Ancients;

namespace YuWanCard.Config;

public readonly record struct YuWanContentSettingsSnapshot(
    bool EnablePigRewardAllCardPools,
    bool EnableYuWanEnemyEncounters,
    bool EnableIgnisBossEncounter,
    bool EnableKillerEliteEncounter,
    bool EnableFerrousWroughtnautEliteEncounter,
    bool EnableYuWanEvents,
    bool EnablePigPigAncient,
    IReadOnlyDictionary<string, bool> EnabledEvents,
    IReadOnlyDictionary<string, bool> EnabledColorlessCards)
{
    private static readonly Lazy<YuWanContentSettingsSnapshot> AllDisabledSnapshot = new(CreateAllDisabled);

    public static YuWanContentSettingsSnapshot AllDisabled => AllDisabledSnapshot.Value;

    public static YuWanContentSettingsSnapshot CaptureLocal()
    {
        return new YuWanContentSettingsSnapshot(
            YuWanCardConfig.EnablePigRewardAllCardPools,
            YuWanCardConfig.EnableYuWanEnemyEncounters,
            YuWanCardConfig.EnableIgnisBossEncounter,
            YuWanCardConfig.EnableKillerEliteEncounter,
            YuWanCardConfig.EnableFerrousWroughtnautEliteEncounter,
            YuWanCardConfig.EnableYuWanEvents,
            YuWanCardConfig.EnablePigPigAncient,
            YuWanEventSettings.SnapshotStates(),
            YuWanColorlessCardSettings.SnapshotStates());
    }

    public bool ContentEquals(in YuWanContentSettingsSnapshot other)
    {
        return EnablePigRewardAllCardPools == other.EnablePigRewardAllCardPools
               && EnableYuWanEnemyEncounters == other.EnableYuWanEnemyEncounters
               && EnableIgnisBossEncounter == other.EnableIgnisBossEncounter
               && EnableKillerEliteEncounter == other.EnableKillerEliteEncounter
               && EnableFerrousWroughtnautEliteEncounter == other.EnableFerrousWroughtnautEliteEncounter
               && EnableYuWanEvents == other.EnableYuWanEvents
               && EnablePigPigAncient == other.EnablePigPigAncient
               && DictionaryStatesEqual(EnabledEvents, other.EnabledEvents)
               && DictionaryStatesEqual(EnabledColorlessCards, other.EnabledColorlessCards);
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

        if (encounterType == typeof(FerrousWroughtnautElite))
        {
            return EnableFerrousWroughtnautEliteEncounter;
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

        if (!YuWanEventCatalog.TryGetDefinition(eventType, out var definition))
        {
            return true;
        }

        return EnabledEvents.GetValueOrDefault(definition.Key, true);
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

    private static YuWanContentSettingsSnapshot CreateAllDisabled()
    {
        return new YuWanContentSettingsSnapshot(
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            YuWanEventCatalog.Events.ToDictionary(static definition => definition.Key, static _ => false,
                StringComparer.Ordinal),
            YuWanColorlessCardCatalog.Cards.ToDictionary(static definition => definition.Key, static _ => false,
                StringComparer.Ordinal));
    }

    private static bool DictionaryStatesEqual(
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
