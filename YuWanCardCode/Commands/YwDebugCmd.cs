using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using YuWanCard.Malice;

namespace YuWanCard.Commands;

public class YwDebugCmd : AbstractConsoleCmd
{
    private static readonly string[] SevenSinPigs =
    [
        "arrogant_pig",
        "jealous_pig",
        "furious_pig",
        "lazy_pig",
        "greedy_pig",
        "gluttonous_pig",
        "lustful_pig"
    ];

    private static readonly MethodInfo? GenerateInitialOptionsMethod = 
        typeof(EventModel).GetMethod("GenerateInitialOptions", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    
    private static readonly MethodInfo? SetEventStateMethod = 
        typeof(EventModel).GetMethod("SetEventState", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, 
            [typeof(LocString), typeof(IEnumerable<EventOption>)]);

    public override string CmdName => "yw";

    public override string Args => "[sinpigrelics|regenerateancient|refreshshop|unlockmalice]";

    public override string Description => "YuWanCard debug commands. 'yw sinpigrelics' - obtain all 7 sin pig relics. 'yw regenerateancient' - regenerate current ancient options. 'yw refreshshop' - reroll all shop items. 'yw unlockmalice' - unlock all Malice levels for all characters";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length < 1)
        {
            return new CmdResult(false, "Usage: yw <sinpigrelics|regenerateancient|refreshshop|unlockmalice>");
        }

        string subCmd = args[0].ToLowerInvariant();

        if (subCmd == "unlockmalice")
        {
            return UnlockAllMalice();
        }

        if (issuingPlayer == null)
        {
            return new CmdResult(false, "A run is currently not in progress!");
        }

        if (subCmd == "sinpigrelics")
        {
            return GrantAllPigs(issuingPlayer);
        }
        
        if (subCmd == "regenerateancient")
        {
            return RegenerateAncientOptions(issuingPlayer);
        }

        if (subCmd == "refreshshop")
        {
            return RefreshShop(issuingPlayer);
        }

        return new CmdResult(false, $"Unknown subcommand: {subCmd}. Use 'yw sinpigrelics', 'yw regenerateancient', 'yw refreshshop', or 'yw unlockmalice'.");
    }

    private CmdResult GrantAllPigs(Player player)
    {
        int granted = 0;
        int alreadyOwned = 0;

        foreach (string pigId in SevenSinPigs)
        {
            string fullId = $"YUWANCARD-{pigId}";
            RelicModel? relic = GetRelicById(fullId);

            if (relic == null)
            {
                MainFile.Logger.Warn($"YwDebugCmd: Could not find relic {fullId}");
                continue;
            }

            if (player.GetRelicById(relic.Id) != null)
            {
                alreadyOwned++;
                continue;
            }

            TaskHelper.RunSafely(RelicCmd.Obtain(relic.ToMutable(), player));
            granted++;
        }

        string message = granted > 0
            ? $"Granted {granted} sin pig relics! ({alreadyOwned} already owned)"
            : $"All 7 sin pig relics already owned!";

        return new CmdResult(true, message);
    }

    private static RelicModel? GetRelicById(string id)
    {
        id = id.ToUpperInvariant();
        foreach (var relic in ModelDb.AllRelics)
        {
            if (relic.Id.Entry.Equals(id, StringComparison.OrdinalIgnoreCase))
            {
                return relic;
            }
        }
        return null;
    }

    private CmdResult RegenerateAncientOptions(Player player)
    {
        var currentRoom = RunManager.Instance.State?.CurrentRoom;
        if (currentRoom is not EventRoom eventRoom)
        {
            return new CmdResult(false, "Current room is not an event room!");
        }

        var currentEvent = eventRoom.LocalMutableEvent;
        if (currentEvent is not AncientEventModel ancientEvent)
        {
            return new CmdResult(false, "Current event is not an ancient event!");
        }

        if (currentEvent.IsFinished)
        {
            return new CmdResult(false, "Ancient event already finished!");
        }

        try
        {
            if (GenerateInitialOptionsMethod?.Invoke(currentEvent, null) is not IReadOnlyList<EventOption> newOptions || newOptions.Count == 0)
            {
                return new CmdResult(false, "Failed to generate new options!");
            }

            var description = currentEvent.InitialDescription;
            SetEventStateMethod?.Invoke(currentEvent, [description, newOptions]);

            MainFile.Logger.Info($"YwDebugCmd: Regenerated {newOptions.Count} options for ancient {ancientEvent.Id.Entry}");
            return new CmdResult(true, $"Regenerated {newOptions.Count} options for ancient {ancientEvent.Id.Entry}!");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"YwDebugCmd: Failed to regenerate options - {ex.Message}");
            return new CmdResult(false, $"Failed to regenerate options: {ex.Message}");
        }
    }

    private CmdResult RefreshShop(Player player)
    {
        var nMerchantRoom = NMerchantRoom.Instance;
        if (nMerchantRoom == null)
        {
            return new CmdResult(false, "Not in a merchant room!");
        }

        var nInventory = nMerchantRoom.Inventory;
        var oldInventory = nInventory.Inventory;
        if (oldInventory == null)
        {
            return new CmdResult(false, "No inventory to refresh!");
        }

        try
        {
            var newInventory = MerchantInventory.CreateForNormalMerchant(player);

            typeof(MerchantRoom).GetProperty("Inventory", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(nMerchantRoom.Room, newInventory);

            typeof(NMerchantInventory).GetProperty("Inventory", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(nInventory, null);

            nInventory.Initialize(newInventory, MerchantRoom.Dialogue);

            MainFile.Logger.Info("YwDebugCmd: Shop refreshed successfully");
            return new CmdResult(true, "Shop refreshed!");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"YwDebugCmd: Failed to refresh shop - {ex.Message}");
            return new CmdResult(false, $"Failed to refresh shop: {ex.Message}");
        }
    }

    private CmdResult UnlockAllMalice()
    {
        try
        {
            int unlockedCharacters = MaliceManager.UnlockAllMalice();
            SaveManager.Instance.Progress.MaxMultiplayerAscension = 10;
            foreach (var character in ModelDb.AllCharacters)
            {
                var stats = SaveManager.Instance.Progress.GetOrCreateCharacterStats(character.Id);
                stats.MaxAscension = 10;
                stats.PreferredAscension = 10;
            }
            SaveManager.Instance.SaveProgressFile();

            string message = unlockedCharacters > 0
                ? $"Unlocked max Malice for {unlockedCharacters} characters!"
                : "All available Malice levels were already unlocked!";
            MainFile.Logger.Info($"YwDebugCmd: {message}");
            return new CmdResult(true, message);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"YwDebugCmd: Failed to unlock malice - {ex.Message}");
            return new CmdResult(false, $"Failed to unlock malice: {ex.Message}");
        }
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length == 0 || (args.Length == 1 && string.IsNullOrWhiteSpace(args[0])))
        {
            return new CompletionResult
            {
                Candidates = ["sinpigrelics", "regenerateancient", "refreshshop", "unlockmalice"],
                Type = CompletionType.Subcommand,
                ArgumentContext = CmdName
            };
        }

        if (args.Length == 1)
        {
            string partial = args[0].ToLowerInvariant();
            var candidates = new List<string>();
            
            if ("sinpigrelics".StartsWith(partial))
            {
                candidates.Add("sinpigrelics");
            }
            if ("regenerateancient".StartsWith(partial))
            {
                candidates.Add("regenerateancient");
            }
            if ("refreshshop".StartsWith(partial))
            {
                candidates.Add("refreshshop");
            }
            if ("unlockmalice".StartsWith(partial))
            {
                candidates.Add("unlockmalice");
            }

            if (candidates.Count > 0)
            {
                return CompleteArgument(candidates, [], partial, CompletionType.Subcommand);
            }
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}
