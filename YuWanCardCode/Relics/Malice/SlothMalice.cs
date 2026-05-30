using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics.Malice;

[Pool(typeof(MaliceRelicPool))]
public sealed class SlothMalice : MaliceRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public SlothMalice() : base(true)
    {
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner?.Creature || Owner?.Creature == null || result.TotalDamage <= 0 || cardSource == null)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, 3, ValueProp.Unpowered, null);
    }
}
