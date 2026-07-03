using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class TankPig : YuWanRelicModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PlatingPower>(6m),
        new DynamicVar("Damage", 2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<PlatingPower>()
    ];

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public TankPig() : base(true)
    {
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, null);
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (Owner?.Creature == null || creature != Owner.Creature || amount <= 0)
        {
            return;
        }

        Creature? target = GetRandomLivingEnemy();
        if (target == null)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target, DynamicVars["Damage"].BaseValue, ValueProp.Unpowered, Owner.Creature, cardSource, null);
    }

    private Creature? GetRandomLivingEnemy()
    {
        return CombatTargetingUtils.GetDeterministicRandomLivingEnemy(Owner);
    }
}
