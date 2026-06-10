using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics.Balatro;
using YuWanCard.Modifiers;

namespace YuWanCard.Relics;

public sealed class ModifierToken : BalatroRelicModel
{
    private const string TokenIconPath = "res://YuWanCard/images/modifiers/balatro.png";

    protected override string BigIconPath => TokenIconPath;

    public override string PackedIconPath => TokenIconPath;

    protected override string PackedIconOutlinePath => TokenIconPath;

    public override RelicRarity Rarity => RelicRarity.None;

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner.RunState is RunState runState)
        {
            BalatroModifier? modifier = BalatroModifier.GetInstance(runState);
            modifier?.AddModifierTokens(Owner, 1);
        }

        await RelicCmd.Remove(this);
    }
}
