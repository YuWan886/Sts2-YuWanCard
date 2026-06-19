using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class ScorchTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BurnCount", 1m)];
    protected override string[] AutoUpdateVarNames => ["BurnCount"];

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
                await CardPileCmd.AddToCombatAndPreview<Burn>(result.Receiver, PileType.Discard, (int)Amount, addedByPlayer: false);
            }
        }
    }
}
