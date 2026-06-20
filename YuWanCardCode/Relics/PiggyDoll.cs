using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class PiggyDoll : YuWanRelicModel
{
    private const int DamageReduction = 3;
    private const int HealPerTurn = 1;

    private bool _reductionAvailableThisTurn = true;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamageReduction", DamageReduction),
        new DynamicVar("Heal", HealPerTurn)
    ];

    public PiggyDoll() : base(true)
    {
    }

    public override Task BeforeCombatStart()
    {
        _reductionAvailableThisTurn = true;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            _reductionAvailableThisTurn = true;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!_reductionAvailableThisTurn)
        {
            return 0m;
        }
        if (Owner?.Creature == null || target != Owner.Creature)
        {
            return 0m;
        }
        if (amount <= 0m)
        {
            return 0m;
        }

        _reductionAvailableThisTurn = false;
        Flash();

        // 负值表示减伤；最多抵消到 0。
        return -Math.Min(DynamicVars["DamageReduction"].BaseValue, amount);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner?.Creature == null || Owner.Creature.IsDead)
        {
            return;
        }

        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
    }
}
