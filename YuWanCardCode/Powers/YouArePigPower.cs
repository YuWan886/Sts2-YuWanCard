using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Characters;

namespace YuWanCard.Powers;

public class YouArePigPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Duration", 1m)];

    private const string PigVisualsPath = "res://YuWanCard/scenes/characters/pig.tscn";

    private NCreatureVisuals? _pigVisuals;
    private NCreatureVisuals? _originalVisuals;
    private NCreature? _creatureNode;
    private Godot.Node2D? _originalBody;
    private CreatureAnimator? _pigAnimator;

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

        _originalBody = _originalVisuals.GetCurrentBody();

        if (_pigVisuals.Bounds != null)
        {
            _pigVisuals.Bounds.Size = Godot.Vector2.Zero;
            _pigVisuals.Bounds.MouseFilter = Godot.Control.MouseFilterEnum.Ignore;
        }

        _pigVisuals.Position = Godot.Vector2.Zero;
        _originalVisuals.AddChild(_pigVisuals);

        _pigVisuals.Scale = new Godot.Vector2(-Math.Abs(_pigVisuals.Scale.X), _pigVisuals.Scale.Y);

        if (_originalBody != null)
            _originalBody.Visible = false;

        await _pigVisuals.AwaitProcessFrame();
        TryEnsurePigAnimator();
        TriggerPigAnimation("Idle");

        Flash();
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_originalBody != null && Godot.GodotObject.IsInstanceValid(_originalBody))
        {
            _originalBody.Visible = true;
        }

        if (_pigVisuals != null && Godot.GodotObject.IsInstanceValid(_pigVisuals))
        {
            _pigVisuals.QueueFree();
            _pigVisuals = null;
        }

        _pigAnimator = null;
        _originalVisuals = null;
        _originalBody = null;
        _creatureNode = null;

        await Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Decrement(this);
        }
    }

    public void TriggerPigAnimation(string trigger)
    {
        if (!TryEnsurePigAnimator())
        {
            return;
        }

        var mappedTrigger = MapTrigger(trigger);
        if (mappedTrigger == null)
        {
            return;
        }

        _pigAnimator?.SetTrigger(mappedTrigger);
    }

    private bool TryEnsurePigAnimator()
    {
        if (_pigAnimator != null)
        {
            return true;
        }

        if (_pigVisuals == null || !Godot.GodotObject.IsInstanceValid(_pigVisuals))
        {
            return false;
        }

        var visualsNode = _pigVisuals.GetNodeOrNull<Godot.Node2D>("%Visuals")
            ?? _pigVisuals.GetNodeOrNull<Godot.Node2D>("Visuals");
        if (visualsNode == null)
        {
            return false;
        }

        var controller = new MegaSprite(visualsNode);
        if (controller.GetSkeleton()?.GetData() == null)
        {
            return false;
        }

        _pigAnimator = Pig.CreateCreatureAnimator(controller);
        return true;
    }

    private static string? MapTrigger(string trigger)
    {
        return trigger switch
        {
            "Idle" => "Idle",
            "Relaxed" => "Relaxed",
            "Revive" => "Idle",
            "Hit" => "Hit",
            "Dead" => "Dead",
            "Cast" => "Cast",
            "PowerUp" => "Cast",
            "Attack" => "Attack",
            "AttackSingle" => "Attack",
            "AttackTriple" => "Attack",
            "Tf" => "Tf",
            "Tf2" => "Tf2",
            _ => null
        };
    }
}
