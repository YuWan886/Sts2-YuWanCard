using System.Reflection;
using HarmonyLib;

namespace YuWanCard.Patches;

[HarmonyPatch]
public static class RunHistoryPlayerBadgesPatch
{
    private static Type? RunHistoryPlayerType { get; set; }
    private static PropertyInfo? BadgesProperty { get; set; }
    private static Type? SerializableBadgeType { get; set; }

    public static bool Prepare()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            RunHistoryPlayerType ??= assembly.GetType("MegaCrit.Sts2.Core.Runs.RunHistoryPlayer");
            SerializableBadgeType ??= assembly.GetType("MegaCrit.Sts2.Core.Saves.Runs.SerializableBadge");
        }

        if (RunHistoryPlayerType == null)
        {
            MainFile.Logger.Info("RunHistoryPlayerBadgesPatch: RunHistoryPlayer type not found, skipping patch (main branch)");
            return false;
        }

        BadgesProperty = RunHistoryPlayerType.GetProperty("Badges");
        if (BadgesProperty == null)
        {
            MainFile.Logger.Info("RunHistoryPlayerBadgesPatch: Badges property not found, skipping patch");
            return false;
        }

        MainFile.Logger.Info("RunHistoryPlayerBadgesPatch: Applying patch (beta branch)");
        return true;
    }

    public static MethodBase TargetMethod() => BadgesProperty!.GetMethod!;

    [HarmonyPostfix]
    public static void Postfix(ref object __result)
    {
        if (__result == null)
        {
            MainFile.Logger.Debug("RunHistoryPlayerBadgesPatch: Badges is null, returning empty list");
            var listType = typeof(List<>).MakeGenericType(SerializableBadgeType!);
            __result = Activator.CreateInstance(listType)!;
        }
    }
}
