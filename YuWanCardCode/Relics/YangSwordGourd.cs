using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class YangSwordGourd : YuWanRelicModel
{
    private const decimal ForgeAmount = 4m;

    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Forge", ForgeAmount)];

    public YangSwordGourd() : base(true)
    {
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        Flash();
        await ForgeCmd.Forge(ForgeAmount, Owner, this);
    }
}
