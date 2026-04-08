using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using YuWanCard.Characters;
using YuWanCard.RestSite;

namespace YuWanCard.Relics;

[Pool(typeof(PigRelicPool))]
public class PigRoastPork : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
        {
            return false;
        }

        options.Add(new RoastPorkRestSiteOption(player));
        return true;
    }
}
