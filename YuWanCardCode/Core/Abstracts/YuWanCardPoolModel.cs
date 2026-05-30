using Godot;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Patches;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanCardPoolModel : CardPoolModel, IYuWanContent
{
    public override string CardFrameMaterialPath => "card_frame_red";

    public override string EnergyColorName => CustomEnergyIconPatches.RegisterPoolEnergyIcon(Id, BigEnergyIconPath, TextEnergyIconPath);

    public virtual Color ShaderColor => new("FFFFFF");

    public virtual float H => ShaderColor.H;
    public virtual float S => ShaderColor.S;
    public virtual float V => ShaderColor.V;

    public virtual bool IsShared => false;

    public virtual string? BigEnergyIconPath => null;
    public virtual string? TextEnergyIconPath => null;

    public virtual Texture2D? CustomFrame(CardModel card) => null;

    protected override CardModel[] GenerateAllCards() => [];
}
