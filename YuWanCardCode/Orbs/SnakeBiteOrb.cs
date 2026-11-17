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
    private const string CompatibleOrbScenePath = "res://scenes/orbs/orb_visuals/dark_orb.tscn";
    private const string DisplayIconPath = "res://YuWanCard/images/enchantments/snake.png";
    private const string TriggerIconPath = "res://YuWanCard/images/enchantments/bite.png";

    public override Color DarkenedColor => new Color("4CAF50");

    public override decimal PassiveVal => 3m;
    public override decimal EvokeVal => 6m;

    public override string? CustomIconPath => DisplayIconPath;
    public override string? CustomTriggerIconPath => TriggerIconPath;

    public override string? CustomSpritePath => CompatibleOrbScenePath;

    protected override string ChannelSfx => "event:/sfx/characters/defect/defect_plasma_channel";

    public override Node2D? CreateCustomSprite()
    {
        var sprite = base.CreateCustomSprite();
        if (sprite == null || CustomIconPath is not string iconPath)
            return sprite;

        var texture = GD.Load<Texture2D>(iconPath);
        if (texture == null)
            return sprite;

        var icon = new Sprite2D
        {
            Name = "SnakeIcon",
            Texture = texture,
            Scale = new Vector2(0.3f, 0.3f)
        };
        sprite.AddChild(icon);
        return sprite;
    }

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        ActivatePassive();
        var enemy = Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
        if (enemy != null)
        {
            await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), enemy, (int)PassiveVal, Owner.Creature, null);
        }
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        var enemy = Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
        if (enemy != null)
        {
            await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), enemy, (int)EvokeVal, Owner.Creature, null);
        }
        return new[] { Owner.Creature };
    }
}
