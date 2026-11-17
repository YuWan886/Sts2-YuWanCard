using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class BlackFlash : YuWanRelicModel
{
    private CardModel? _empoweredAttack;
    private bool _hasEmittedVfx;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public BlackFlash() : base(true)
    {
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner == null) return Task.CompletedTask;
        if (cardPlay.Card.Owner != Owner) return Task.CompletedTask;
        if (!cardPlay.Card.Tags.Contains(CardTag.Strike)) return Task.CompletedTask;
        if (cardPlay.Target == null || cardPlay.Target.Side != CombatSide.Enemy) return Task.CompletedTask;

        if (DeterministicRandomUtils.RollProbability(Owner.RunState.Rng.CombatCardSelection, 0.1f))
        {
            _empoweredAttack = cardPlay.Card;
            MainFile.Logger.Info($"BlackFlash triggered on {cardPlay.Card.Title}, dealing 2.5x damage");
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack()) return 1m;
        if (dealer != Owner?.Creature) return 1m;
        if (cardSource == null) return 1m;
        if (_empoweredAttack == null) return 1m;
        if (cardSource != _empoweredAttack) return 1m;

        return 2.5m;
    }

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner?.Creature) return Task.CompletedTask;
        if (result.TotalDamage <= 0) return Task.CompletedTask;
        if (cardSource == null) return Task.CompletedTask;
        if (cardSource != _empoweredAttack) return Task.CompletedTask;
        if (_hasEmittedVfx) return Task.CompletedTask;

        _hasEmittedVfx = true;

        Flash();
        VfxUtils.PlayAtCreature("res://YuWanCard/scenes/vfx/vfx_black_flash.tscn", target);

        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        _hasEmittedVfx = false;

        if (cardPlay.Card == _empoweredAttack)
        {
            _empoweredAttack = null;
        }

        return Task.CompletedTask;
    }
}
