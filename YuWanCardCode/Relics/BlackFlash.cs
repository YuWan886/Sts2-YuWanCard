using BaseLib.Utils;
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
    private bool _triggeredThisAttack = false;
    private Creature? _targetCreature = null;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public BlackFlash() : base(true)
    {
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _triggeredThisAttack = false;
        _targetCreature = null;

        if (dealer != Owner?.Creature) return 1m;
        if (cardSource == null) return 1m;
        if (!cardSource.Tags.Contains(CardTag.Strike)) return 1m;
        if (Owner == null) return 1m;
        if (target == null || target.Side != CombatSide.Enemy) return 1m;

        if (Owner.RunState.Rng.Niche.NextFloat() >= 0.1f) return 1m;

        _triggeredThisAttack = true;
        _targetCreature = target;

        MainFile.Logger.Info($"BlackFlash triggered on {cardSource.Title}, dealing 2.5x damage");

        return 2.5m;
    }

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (!_triggeredThisAttack) return Task.CompletedTask;
        if (dealer != Owner?.Creature) return Task.CompletedTask;
        if (result.TotalDamage <= 0) return Task.CompletedTask;

        _triggeredThisAttack = false;

        Flash();

        if (_targetCreature != null)
        {
            VfxUtils.PlayAtCreature("res://YuWanCard/scenes/vfx/vfx_black_flash.tscn", _targetCreature);
        }
        else
        {
            VfxUtils.PlayCentered("res://YuWanCard/scenes/vfx/vfx_black_flash.tscn");
        }

        _targetCreature = null;

        return Task.CompletedTask;
    }
}
