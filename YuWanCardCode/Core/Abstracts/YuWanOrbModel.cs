using Godot;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanOrbModel : OrbModel, IYuWanContent
{
    public virtual string? CustomIconPath => null;
    public virtual string? CustomSpritePath => null;

    public virtual Node2D? CreateCustomSprite() => null;
}
