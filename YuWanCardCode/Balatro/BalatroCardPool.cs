using Godot;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Balatro;

public sealed class BalatroCardPool : YuWanCardPoolModel
{
    public override string Title => "balatro";

    public override Color ShaderColor => new("E8DCC3");

    public override bool IsColorless => true;

    public override bool IsShared => true;

    public override Color DeckEntryCardColor => new("E8DCC3");

    public override Color EnergyOutlineColor => new("4A3B2A");
}
