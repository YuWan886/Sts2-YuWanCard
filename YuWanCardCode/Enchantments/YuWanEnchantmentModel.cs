using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Helpers;

namespace YuWanCard.Enchantments;

public abstract class YuWanEnchantmentModel : CustomEnchantmentModel
{
    protected override string? CustomIconPath => $"res://YuWanCard/images/enchantments/{GetIconFileName()}.png";

    private string GetIconFileName()
    {
        var className = GetType().Name;
        return StringHelper.Slugify(className).Replace('-', '_').ToLowerInvariant();
    }
}
