using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Orbs;

[RegisterOrb]
public class SnakeBiteOrb : YuWanOrbModel
{
    public override Color DarkenedColor => new Color("4CAF50");

    public override decimal PassiveVal => 3m;
    public override decimal EvokeVal => 6m;

    public override string? CustomIconPath => "res://YuWanCard/images/orbs/snake_bite.png";

    public override string? CustomSpritePath => "res://YuWanCard/scenes/orbs/snake_bite.tscn";

    protected override string ChannelSfx => "event:/sfx/characters/defect/defect_plasma_channel";

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        var enemy = Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
        if (enemy != null)
        {
            await PowerCmd.Apply<PoisonPower>(enemy, (int)PassiveVal, Owner.Creature, null);
        }
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        var enemy = Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
        if (enemy != null)
        {
            await PowerCmd.Apply<PoisonPower>(enemy, (int)EvokeVal, Owner.Creature, null);
        }
        return new[] { Owner.Creature };
    }
}
