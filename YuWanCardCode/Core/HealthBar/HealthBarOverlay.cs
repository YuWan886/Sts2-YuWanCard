using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YuWanCard.Core.HealthBar;

public enum HealthBarOverlayDirection
{
    FromRight,
    FromLeft
}

public readonly record struct HealthBarOverlaySegment(
    int Amount,
    Color Color,
    HealthBarOverlayDirection Direction = HealthBarOverlayDirection.FromRight,
    int Order = 0);

public readonly record struct HealthBarOverlayContext(Creature Creature);

public interface IHealthBarOverlaySource
{
    IEnumerable<HealthBarOverlaySegment> GetHealthBarOverlaySegments(HealthBarOverlayContext context);
}
