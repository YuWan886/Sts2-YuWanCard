using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class ExposeTrait : MaliceTraitPowerBase
{
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != Owner)
        {
            return;
        }

        foreach (var results in command.Results)
        {
            foreach (var result in results)
            {
                if (result.Receiver.IsPlayer && !result.Receiver.IsDead && result.UnblockedDamage > 0)
                {
                    Flash();
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, result.Receiver, Amount, Owner, null);
                }
            }
        }
    }
}
