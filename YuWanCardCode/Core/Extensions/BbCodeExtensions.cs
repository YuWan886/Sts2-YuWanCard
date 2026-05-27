using System.Text.RegularExpressions;

namespace YuWanCard.Core.Extensions;

public static class BbCodeExtensions
{
    private static readonly Regex ColorlOpenTag = new(@"\[colorl=([^\]]+)\]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex ColorlCloseTag = new(@"\[/colorl\]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex BbCodeTag = new(@"\[/?[A-Za-z_]+(?:=[^\]]+)?\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string ExpandExtendedBbCode(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        text = ColorlOpenTag.Replace(text, "[color=$1]");
        return ColorlCloseTag.Replace(text, "[/color]");
    }

    public static bool ContainsBbCodeTag(this string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return BbCodeTag.IsMatch(text);
    }

    public static string StripBbCodeTags(this string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return BbCodeTag.Replace(text, string.Empty);
    }
}
