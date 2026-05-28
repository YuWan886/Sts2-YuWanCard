using Godot;

namespace YuWanCard.Utils;

/// <summary>
/// Auto-destroys the parent node after a fixed duration.
/// Used by DeathEffectPatch to clean up VFX nodes.
/// </summary>
public partial class DeathEffectAutoDestroy : Node
{
    private double _elapsedTime;
    private const double Duration = 1.5;

    public override void _Process(double delta)
    {
        base._Process(delta);
        _elapsedTime += delta;

        if (_elapsedTime >= Duration)
        {
            var parent = GetParent();
            if (parent != null)
                parent.QueueFree();
            else
                QueueFree();
        }
    }
}
