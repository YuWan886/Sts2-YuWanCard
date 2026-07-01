using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using YuWanCard.Malice;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(UnlockConsoleCmd))]
public static class UnlockConsoleCmdPatch
{
    private const string MaliceArg = "malice";
    private const string MalicesArg = "malices";

    [HarmonyPatch(nameof(UnlockConsoleCmd.Process))]
    [HarmonyPrefix]
    private static bool ProcessPrefix(string[] args, ref CmdResult __result)
    {
        if (args.Length < 1)
        {
            return true;
        }

        string discoveryType = args[0].ToLowerInvariant();
        if (discoveryType is not MaliceArg and not MalicesArg)
        {
            return true;
        }

        if (args.Length > 1)
        {
            __result = new CmdResult(false, "unlock malice does not accept additional ids.");
            return false;
        }

        try
        {
            UnlockAscensionAndMaliceProgress();
            __result = new CmdResult(true, "Unlocked malice");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"UnlockConsoleCmdPatch: failed to unlock malice - {ex}");
            __result = new CmdResult(false, $"Failed to unlock malice: {ex.Message}");
        }

        return false;
    }

    [HarmonyPatch(nameof(UnlockConsoleCmd.Process))]
    [HarmonyPostfix]
    private static void ProcessPostfix(string[] args, ref CmdResult __result)
    {
        if (!__result.success || args.Length < 1)
        {
            return;
        }

        string discoveryType = args[0].ToLowerInvariant();
        if (discoveryType is not "all" and not "ascensions")
        {
            return;
        }

        try
        {
            UnlockAscensionAndMaliceProgress();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"UnlockConsoleCmdPatch: failed to sync malice after unlock {discoveryType} - {ex}");
        }
    }

    [HarmonyPatch(nameof(UnlockConsoleCmd.GetArgumentCompletions))]
    [HarmonyPostfix]
    private static void GetArgumentCompletionsPostfix(string[] args, ref CompletionResult __result)
    {
        if (args.Length > 1)
        {
            return;
        }

        string partial = args.Length == 0 ? string.Empty : args[0];
        if (!MaliceArg.Contains(partial, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var candidates = (__result.Candidates ?? [])
            .Concat([MaliceArg])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        __result = new CompletionResult
        {
            Candidates = candidates,
            Type = CompletionType.Argument,
            ArgumentContext = "unlock"
        };
    }

    private static void UnlockAscensionAndMaliceProgress()
    {
        SaveManager.Instance.Progress.MaxMultiplayerAscension = 10;
        foreach (CharacterModel character in ModelDb.AllCharacters)
        {
            CharacterStats stats = SaveManager.Instance.Progress.GetOrCreateCharacterStats(character.Id);
            stats.MaxAscension = Math.Max(stats.MaxAscension, 10);
            stats.PreferredAscension = Math.Max(stats.PreferredAscension, stats.MaxAscension);
        }

        SaveManager.Instance.SaveProgressFile();
        MaliceManager.UnlockAllMalice();
    }
}
