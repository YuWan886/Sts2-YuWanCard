using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class DrainTrait : MaliceTraitPowerBase
{
    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != Owner)
        {
            return;
        }

        foreach (var result in command.Results)
        {
            if (!result.Receiver.IsPlayer || result.Receiver.IsDead || result.UnblockedDamage <= 0)
            {
                continue;
            }

            PowerModel? randomBuff = result.Receiver.Powers
                .Where(p => p.Type == PowerType.Buff && p.IsVisible)
                .OrderBy(_ => CombatState?.RunState.Rng.Shuffle.NextFloat() ?? 0f)
                .FirstOrDefault();

            if (randomBuff != null)
            {
                await PowerCmd.Remove(randomBuff);
            }

            Flash();
            await PowerCmd.Apply<StrengthPower>(Owner, 2 * Amount, Owner, null);
            break;
        }
    }
}
