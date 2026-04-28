using System.Text;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;

namespace YuWanCard.Core.Utils;

public static class AncientDialogueUtil
{
    private const string ArchitectKey = "THE_ARCHITECT";
    private const string AttackKey = "-attack";
    private const string VisitIndexKey = "-visit";

    public static string SfxPath(string dialogueLoc) =>
        LocString.GetIfExists("ancients", dialogueLoc + ".sfx")?.GetRawText() ?? "";

    public static string BaseLocKey(string ancientId, string charId) => $"{ancientId}.talk.{charId}.";

    public static List<AncientDialogue> GetDialoguesForKey(string locTable, string baseKey, StringBuilder? log = null)
    {
        log?.AppendLine($"Looking for dialogues for '{baseKey}' in {locTable}.json");
        List<AncientDialogue> dialogues = [];
        var isArchitect = baseKey.StartsWith(ArchitectKey);

        int index = 0, visitIndex = 0;
        while (DialogueExists(locTable, baseKey, index))
        {
            log?.Append($"Found dialogue '{index}'");

            if (isArchitect)
            {
                visitIndex = index;
            }
            else
            {
                visitIndex = index switch
                {
                    0 => 0,
                    1 => 1,
                    2 => 4,
                    _ => visitIndex + 3
                };
            }
            var indexLoc = LocString.GetIfExists(locTable, $"{baseKey}{index}{VisitIndexKey}");
            if (indexLoc != null) visitIndex = int.Parse(indexLoc.GetRawText());

            List<string> sfxPaths = [];

            var line = ExistingLine(locTable, baseKey, index, sfxPaths.Count);

            while (line != null)
            {
                sfxPaths.Add(SfxPath(line));
                line = ExistingLine(locTable, baseKey, index, sfxPaths.Count);
            }

            log?.AppendLine($" with {sfxPaths.Count} lines");

            var attackers = ArchitectAttackers.None;
            if (isArchitect)
            {
                attackers = ArchitectAttackers.Architect;
                var attackString = LocString.GetIfExists(locTable, $"{baseKey}{index}{AttackKey}");
                if (Enum.TryParse(attackString?.GetRawText(), true, out ArchitectAttackers result)) attackers = result;
            }

            dialogues.Add(new AncientDialogue(sfxPaths.ToArray())
            {
                VisitIndex = visitIndex,
                EndAttackers = attackers
            });
            ++index;
        }

        return dialogues;
    }

    private static bool DialogueExists(string locTable, string baseKey, int index)
    {
        return LocString.Exists(locTable, $"{baseKey}{index}-0.ancient") ||
               LocString.Exists(locTable, $"{baseKey}{index}-0r.ancient") ||
               LocString.Exists(locTable, $"{baseKey}{index}-0.char") ||
               LocString.Exists(locTable, $"{baseKey}{index}-0r.char");
    }

    private static string? ExistingLine(string locTable, string baseKey, int dialogueIndex, int lineIndex)
    {
        var locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}r.ancient";
        if (LocString.Exists(locTable, locEntry)) return locEntry;

        locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}r.char";
        if (LocString.Exists(locTable, locEntry)) return locEntry;

        locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}.ancient";
        if (LocString.Exists(locTable, locEntry)) return locEntry;

        locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}.char";
        if (LocString.Exists(locTable, locEntry)) return locEntry;

        return null;
    }
}
