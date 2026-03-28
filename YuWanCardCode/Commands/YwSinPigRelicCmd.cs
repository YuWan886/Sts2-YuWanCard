using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Commands;

public class YwPigsCmd : AbstractConsoleCmd
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

    public override string CmdName => "yw";

    public override string Args => "[sinpigrelics]";

    public override string Description => "YuWanCard commands. 'yw sinpigrelics' - obtain all 7 sin pig relics";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length < 1)
        {
            return new CmdResult(false, "Usage: yw sinpigrelics - obtain all 7 sin pig relics");
        }

        if (issuingPlayer == null)
        {
            return new CmdResult(false, "A run is currently not in progress!");
        }

        string subCmd = args[0].ToLowerInvariant();

        if (subCmd == "sinpigrelics")
        {
            return GrantAllPigs(issuingPlayer);
        }

        return new CmdResult(false, $"Unknown subcommand: {subCmd}. Use 'yw sinpigrelics' to obtain all 7 sin pig relics.");
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
                MainFile.Logger.Warn($"YwPigsCmd: Could not find relic {fullId}");
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

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length == 0 || (args.Length == 1 && string.IsNullOrWhiteSpace(args[0])))
        {
            return new CompletionResult
            {
                Candidates = ["sinpigrelics"],
                Type = CompletionType.Subcommand,
                ArgumentContext = CmdName
            };
        }

        if (args.Length == 1)
        {
            string partial = args[0].ToLowerInvariant();
            if ("sinpigrelics".StartsWith(partial))
            {
                return CompleteArgument(["sinpigrelics"], [], partial, CompletionType.Subcommand);
            }
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}
