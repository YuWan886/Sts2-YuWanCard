using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Enchantments;

public sealed class Bite : YuWanEnchantmentModel
{
    public override bool ShowAmount => true;
    public override bool IsStackable => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PoisonAmountVar(this)];

    public override bool CanEnchant(CardModel card)
    {
        if (!base.CanEnchant(card))
        {
            return false;
        }

        return card.TargetType is TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay == null || Card == null || Card.Owner?.Creature == null)
        {
            return;
        }

        var poisonAmount = 7 * Amount;
        await PowerCmd.Apply<PoisonPower>(GetTargets(cardPlay), poisonAmount, Card.Owner.Creature, Card);
    }

    private IEnumerable<Creature> GetTargets(CardPlay cardPlay)
    {
        if (Card == null)
        {
            return Array.Empty<Creature>();
        }

        return Card.TargetType switch
        {
            TargetType.AllEnemies => Card.CombatState?.HittableEnemies ?? Array.Empty<Creature>(),
            TargetType.AnyEnemy => GetSingleTarget(cardPlay.Target),
            TargetType.RandomEnemy => GetRandomEnemyTarget(cardPlay.Target),
            _ => Array.Empty<Creature>()
        };
    }

    private static IEnumerable<Creature> GetSingleTarget(Creature? target)
    {
        return target == null ? Array.Empty<Creature>() : new[] { target };
    }

    private IEnumerable<Creature> GetRandomEnemyTarget(Creature? target)
    {
        if (target != null)
        {
            return new[] { target };
        }

        var combatState = Card?.CombatState;
        var rng = Card?.Owner?.RunState?.Rng;
        if (combatState == null || rng == null)
        {
            return Array.Empty<Creature>();
        }

        var randomTarget = rng.CombatTargets.NextItem(combatState.HittableEnemies);
        return randomTarget == null ? Array.Empty<Creature>() : new[] { randomTarget };
    }
    }

    private sealed class PoisonAmountVar(Bite enchantment) : DynamicVar("Poison", 7m)
    {
        private int _cachedAmount = -1;
        private decimal _cachedValue = 7m;

        public new decimal BaseValue
        {
            get
            {
                if (_cachedAmount != enchantment.Amount)
                {
                    _cachedAmount = enchantment.Amount;
                    _cachedValue = 7m * enchantment.Amount;
                }
                return _cachedValue;
            }
        }

        protected override decimal GetBaseValueForIConvertible() => BaseValue;

        public override string ToString() => ((int)BaseValue).ToString();
    }
}
