using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Config;

namespace YuWanCard.Core.Patches;

public static class CustomBossRegistry
{
    public static IEnumerable<EncounterModel> GetRegisteredBossesForAct(ActModel act, bool includeDiscoveryOrderOnly)
    {
        foreach (var registration in ContentRegistry.BossRegistrations)
        {
            if (!registration.ActType.IsAssignableFrom(act.GetType()))
            {
                continue;
            }

            if (includeDiscoveryOrderOnly && !registration.IncludeInDiscoveryOrder)
            {
                continue;
            }

            EncounterModel? encounter = TryResolveEncounter(registration.EncounterType);
            if (encounter == null || encounter.RoomType != MegaCrit.Sts2.Core.Rooms.RoomType.Boss)
            {
                continue;
            }

            if (!YuWanContentAvailability.IsEncounterTypeEnabled(registration.EncounterType))
            {
                continue;
            }

            yield return encounter;
        }
    }

    private static EncounterModel? TryResolveEncounter(Type encounterType)
    {
        try
        {
            return ModelDb.GetById<EncounterModel>(ModelDb.GetId(encounterType));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"CustomBossRegistry: Failed to resolve encounter {encounterType.Name}: {ex.Message}");
            return null;
        }
    }

    public static IEnumerable<EncounterModel> AppendRegisteredBosses(
        ActModel act,
        IEnumerable<EncounterModel> source,
        bool discoveryOrderOnly)
    {
        var list = source.ToList();
        foreach (var boss in GetRegisteredBossesForAct(act, discoveryOrderOnly))
        {
            if (!list.Any(existing => existing.Id == boss.Id))
            {
                list.Add(boss);
            }
        }

        return list;
    }
}

[HarmonyPatch]
internal static class CustomActBossEncounterPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(AssemblyScanner.GetLoadableTypes)
            .Where(type => typeof(ActModel).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => AccessTools.Method(type, nameof(ActModel.GenerateAllEncounters)))
            .Where(method => method != null)
            .Distinct()!;
    }

    static void Postfix(ActModel __instance, ref IEnumerable<EncounterModel> __result)
    {
        __result = CustomBossRegistry.AppendRegisteredBosses(__instance, __result, discoveryOrderOnly: false);
    }
}

[HarmonyPatch]
internal static class CustomActBossDiscoveryOrderPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(AssemblyScanner.GetLoadableTypes)
            .Where(type => typeof(ActModel).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => AccessTools.PropertyGetter(type, nameof(ActModel.BossDiscoveryOrder)))
            .Where(method => method != null)
            .Distinct()!;
    }

    static void Postfix(ActModel __instance, ref IEnumerable<EncounterModel> __result)
    {
        __result = CustomBossRegistry.AppendRegisteredBosses(__instance, __result, discoveryOrderOnly: true);
    }
}
