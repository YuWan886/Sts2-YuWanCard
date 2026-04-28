using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Orbs;

public class LittleRegentOrb : YuWanOrbModel
{
    public override Color DarkenedColor => new Color("FFD700");

    public override decimal PassiveVal => 3m;
    public override decimal EvokeVal => 6m;

    public override string? CustomIconPath => "res://YuWanCard/images/card_portraits/little_regent.png";
    
    public override string? CustomSpritePath => "res://scenes/orbs/orb_visuals/plasma_orb.tscn";

    protected override string ChannelSfx => "event:/sfx/characters/defect/defect_plasma_channel";

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
