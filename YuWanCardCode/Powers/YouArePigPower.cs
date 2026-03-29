using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YuWanCard.Powers;

public class YouArePigPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private const string PigVisualsPath = "res://YuWanCard/scenes/characters/pig.tscn";

    private NCreatureVisuals? _pigVisuals;
    private NCreatureVisuals? _originalVisuals;
    private NCreature? _creatureNode;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (_creatureNode == null) return;

        _originalVisuals = _creatureNode.Visuals;
        if (_originalVisuals == null) return;

        _pigVisuals = NodeFactory<NCreatureVisuals>.CreateFromScene(PigVisualsPath);
        if (_pigVisuals == null)
        {
            MainFile.Logger.Warn("Failed to create NCreatureVisuals from pig scene");
            return;
        }

        _originalVisuals.Visible = false;

        _creatureNode.AddChild(_pigVisuals);

        if (Owner.Side == CombatSide.Enemy)
        {
            _pigVisuals.Scale = new Godot.Vector2(-_pigVisuals.Scale.X, _pigVisuals.Scale.Y);
        }

        Flash();

        await Task.CompletedTask;
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_originalVisuals != null)
        {
            _originalVisuals.Visible = true;
        }

        if (_pigVisuals != null)
        {
            _pigVisuals.QueueFree();
            _pigVisuals = null;
        }

        _originalVisuals = null;
        _creatureNode = null;

        await Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
