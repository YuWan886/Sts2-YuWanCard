using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class ReflectTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ReflectPercent", 30m)];
    protected override string[] AutoUpdateVarNames => ["ReflectPercent"];

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer == Owner || result.TotalDamage <= 0)
        {
            return;
        }

        int reflectDamage = Math.Max(1, (int)Math.Ceiling(result.TotalDamage * 0.3m * Amount));
        Flash();
        await CreatureCmd.Damage(choiceContext, dealer, reflectDamage, ValueProp.Unpowered, Owner, null);
    }
}
