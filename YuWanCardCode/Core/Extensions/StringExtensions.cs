namespace YuWanCard.Core.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Removes the prefix from an entry ID.
    /// Handles both colon (:) and hyphen (-) separators.
    /// Examples:
    ///   "WATCHER-pure_water" -> "pure_water"
    ///   "YUWANCARD:pig_strike" -> "pig_strike"
    /// </summary>
    public static string RemovePrefix(this string entryId)
    {
        // Try colon separator first (original behavior)
        int colonIndex = entryId.IndexOf(':');
        if (colonIndex >= 0)
            return entryId[(colonIndex + 1)..];
        
        // Try hyphen separator (for mod prefixes like "WATCHER-")
        int hyphenIndex = entryId.IndexOf('-');
        if (hyphenIndex >= 0)
            return entryId[(hyphenIndex + 1)..];
        
        // No separator found, return as-is
        return entryId;
    }
}
