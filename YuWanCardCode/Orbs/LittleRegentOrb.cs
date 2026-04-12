using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YuWanCard.Orbs;

public class LittleRegentOrb : CustomOrbModel
{
    public override Color DarkenedColor => new Color("FFD700");

    public override decimal PassiveVal => 3m;
    public override decimal EvokeVal => 6m;

    public override string? CustomIconPath => "res://images/ui/top_panel/character_icon_regent.png";

    public override string? CustomChannelSfx => "event:/sfx/characters/defect/defect_plasma_channel";

    public override Node2D? CreateCustomSprite()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://scenes/orbs/orb_visuals/plasma_orb.tscn");
        if (scene == null) return null;
        return scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
    }

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        await ForgeCmd.Forge(PassiveVal, Owner, this);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        await ForgeCmd.Forge(EvokeVal, Owner, this);
        return new[] { Owner.Creature };
    }

}
