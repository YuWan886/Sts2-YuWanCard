using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class VampiricTrait : MaliceTraitPowerBase
{
    private const decimal LifestealRatio = 0.5m;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("VampPercent", 50m)];

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != Owner || Owner.IsDead)
        {
            return;
        }

        int totalUnblocked = 0;
        foreach (var results in command.Results)
        {
            foreach (var result in results)
            {
                if (result.Receiver.IsPlayer && result.UnblockedDamage > 0)
                {
                    totalUnblocked += (int)result.UnblockedDamage;
                }
            }
        }

        if (totalUnblocked <= 0)
        {
            return;
        }

        int healAmount = Math.Max(1, (int)Math.Ceiling(totalUnblocked * LifestealRatio));
        Flash();
        await CreatureCmd.Heal(Owner, healAmount, playAnim: true);
    }
}
