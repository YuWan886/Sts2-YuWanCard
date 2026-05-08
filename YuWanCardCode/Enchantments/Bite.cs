using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
        if (cardPlay == null || Card == null)
        {
            return;
        }

        var target = cardPlay.Target;
        if (target == null)
        {
            return;
        }

        var poisonAmount = 7 * Amount;
        await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(),target, poisonAmount, Card.Owner?.Creature, Card);
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
