using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class DrainTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DrainBuffs", 1m)];
    protected override string[] AutoUpdateVarNames => ["DrainBuffs"];

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner || !target.IsPlayer || target.IsDead || result.UnblockedDamage <= 0)
        {
            return;
        }

        PowerModel? randomBuff = target.Powers
            .Where(p => p.Type == PowerType.Buff && p.IsVisible)
            .OrderBy(_ => CombatState?.RunState.Rng.Shuffle.NextFloat() ?? 0f)
            .FirstOrDefault();

        if (randomBuff != null)
        {
            await PowerCmd.Remove(randomBuff);
        }

        Flash();
    }
}
