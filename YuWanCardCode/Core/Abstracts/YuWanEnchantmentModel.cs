using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanEnchantmentModel : EnchantmentModel, IYuWanContent
{
    protected virtual string? CustomIconPath => $"res://YuWanCard/images/enchantments/{GetIconFileName()}.png";

    internal string? ResolvedCustomIconPath => CustomIconPath;

    private string GetIconFileName()
    {
        var className = GetType().Name;
        return StringHelper.Slugify(className).Replace('-', '_').ToLowerInvariant();
    }
}
