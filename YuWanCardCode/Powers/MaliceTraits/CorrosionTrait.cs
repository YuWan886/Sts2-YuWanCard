using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class CorrosionTrait : MaliceTraitPowerBase
{
    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != Owner)
        {
            return;
        }

        foreach (var result in command.Results)
        {
            if (result.Receiver.Player == null || result.Receiver.IsDead || result.UnblockedDamage <= 0)
            {
                continue;
            }

            Flash();
            await PowerCmd.Apply<CorrosionEnergyLossPower>(result.Receiver, Amount, Owner, null);
            break;
        }
    }
}
