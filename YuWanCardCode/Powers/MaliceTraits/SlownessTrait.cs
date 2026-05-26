using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class SlownessTrait : MaliceTraitPowerBase
{
    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != Owner)
        {
            return;
        }

        foreach (var result in command.Results)
        {
            if (result.Receiver.IsPlayer && !result.Receiver.IsDead && result.UnblockedDamage > 0)
            {
                Flash();
                await PowerCmd.Apply<FrailPower>(result.Receiver, Amount, Owner, null);
            }
        }
    }
}
