using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class BlindnessTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DazedCount", 2m)];
    protected override string[] AutoUpdateVarNames => ["DazedCount"];

    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != Owner)
        {
            return;
        }

        foreach (var result in command.Results)
        {
            if (result.Receiver.Player != null && !result.Receiver.IsDead && result.UnblockedDamage > 0)
            {
                Flash();
                await CardPileCmd.AddToCombatAndPreview<Dazed>(result.Receiver, PileType.Draw, 2 * Amount, addedByPlayer: false);
            }
        }
    }
}
