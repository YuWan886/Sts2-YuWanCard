using Godot;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanOrbModel : OrbModel, IYuWanContent
{
    public virtual string? CustomIconPath => null;
    public virtual string? CustomSpritePath => null;
    public virtual string? CustomTriggerIconPath => null;

    public virtual Node2D? CreateCustomSprite()
    {
        if (CustomSpritePath is not string path)
            return null;
        var scene = GD.Load<PackedScene>(path);
        return scene.Instantiate<Node2D>();
    }

    public virtual Texture2D? GetTriggerTexture()
    {
        if (CustomTriggerIconPath is not string path)
            return null;
        return GD.Load<Texture2D>(path);
    }
}
