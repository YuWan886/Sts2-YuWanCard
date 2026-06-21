using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
public sealed class TrapPig : YuWanRelicModel
{
    private readonly HashSet<Creature> _enemiesThatAttackedYouThisTurn = [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PoisonPower>(2m),
        new PowerVar<WeakPower>(1m),
        new DynamicVar("BonusPoison", 4m),
        new DynamicVar("BonusWeak", 2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<PoisonPower>(),
        HoverTipFactory.FromPower<WeakPower>()
    ];

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public TrapPig() : base(true)
    {
    }

    public override Task BeforeCombatStart()
    {
        _enemiesThatAttackedYouThisTurn.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            _enemiesThatAttackedYouThisTurn.Clear();
        }

        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature || dealer == null || dealer.Side != CombatSide.Enemy || result.TotalDamage <= 0)
        {
            return Task.CompletedTask;
        }

        _enemiesThatAttackedYouThisTurn.Add(dealer);
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner?.Creature == null || !participants.Contains(Owner.Creature))
        {
            return;
        }

        Creature? target = SelectTarget();
        if (target == null)
        {
            _enemiesThatAttackedYouThisTurn.Clear();
            return;
        }

        bool attackedYou = _enemiesThatAttackedYouThisTurn.Contains(target);
        int poison = attackedYou
            ? DynamicVars["BonusPoison"].IntValue
            : DynamicVars["PoisonPower"].IntValue;
        int weak = attackedYou
            ? DynamicVars["BonusWeak"].IntValue
            : DynamicVars["WeakPower"].IntValue;

        Flash();
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, poison, Owner.Creature, null);
        await PowerCmd.Apply<WeakPower>(choiceContext, target, weak, Owner.Creature, null);
        _enemiesThatAttackedYouThisTurn.Clear();
    }

    private Creature? SelectTarget()
    {
        return CombatTargetingUtils.GetDeterministicRandomLivingEnemy(Owner);
    }
}
