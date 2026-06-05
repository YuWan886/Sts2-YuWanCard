using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

/// <summary>
/// When combo >= 5, deal 8-20 damage to a random enemy on each card played.
/// </summary>
[Pool(typeof(SharedRelicPool))]
public sealed class GamblerJoker : BalatroJokerRelicModel
{
    private const int MinDamage = 8;
    private const int MaxDamage = 21;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        if (Owner == null || cardPlay.Card.Owner != Owner)
        {
            return;
        }

        BalatroModifier? modifier = GetModifier(Owner);
        if (modifier == null || modifier.ComboCounter < 5f)
        {
            return;
        }

        ICombatState? combatState = Owner.Creature?.CombatState;
        if (combatState == null)
        {
            return;
        }

        Creature? target = combatState.Enemies
            .Where(enemy => !enemy.IsDead)
            .OrderBy(_ => Owner.RunState.Rng.Niche.NextFloat())
            .FirstOrDefault();
        if (target == null)
        {
            return;
        }

        int damage = Owner.RunState.Rng.Niche.NextInt(MinDamage, MaxDamage);
        await CreatureCmd.Damage(context, target, damage, ValueProp.Move, Owner.Creature, null);
    }

    private static BalatroModifier? GetModifier(Player owner)
    {
        return owner.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
    }
}
