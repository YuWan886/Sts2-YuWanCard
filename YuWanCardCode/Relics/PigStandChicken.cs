using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class PigStandChicken : YuWanRelicModel
{
    private const int BaseDamage = 3;
    private const int EmpoweredDamage = 8;
    private const int AttackThreshold = 3;

    private int _attackCardsPlayedLastTurn;
    private int _attackCardsPlayedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public PigStandChicken() : base(true)
    {
    }

    public override Task BeforeCombatStart()
    {
        _attackCardsPlayedLastTurn = 0;
        _attackCardsPlayedThisTurn = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
        {
            return;
        }

        Creature? target = GetRandomLivingEnemy();
        if (target == null)
        {
            return;
        }

        int damage = _attackCardsPlayedLastTurn >= AttackThreshold ? EmpoweredDamage : BaseDamage;
        Flash();
        await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move, Owner.Creature, null);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != null
            && cardPlay.Card.Owner == Owner
            && cardPlay.Card.Type == CardType.Attack)
        {
            _attackCardsPlayedThisTurn++;
        }

        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner?.Creature == null || !participants.Contains(Owner.Creature))
        {
            return Task.CompletedTask;
        }

        _attackCardsPlayedLastTurn = _attackCardsPlayedThisTurn;
        _attackCardsPlayedThisTurn = 0;
        return Task.CompletedTask;
    }

    private Creature? GetRandomLivingEnemy()
    {
        return Owner?.Creature?.CombatState?.Enemies
            .Where(enemy => !enemy.IsDead)
            .OrderBy(_ => Owner.RunState.Rng.Niche.NextFloat())
            .FirstOrDefault();
    }
}
