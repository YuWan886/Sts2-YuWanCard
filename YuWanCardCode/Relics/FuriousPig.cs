using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class FuriousPig : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(4m)];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<FrailPower>()];

    public FuriousPig() : base(true)
    {
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner == null || Owner.Creature == null)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
        await PowerCmd.Apply<FrailPower>(Owner.Creature, 1, Owner.Creature, null);
    }
}
