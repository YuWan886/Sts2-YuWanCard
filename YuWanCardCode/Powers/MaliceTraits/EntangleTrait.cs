using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
namespace YuWanCard.Powers.MaliceTraits;

public sealed class EntangleTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ConstrictAmount", 3m)];
    protected override string[] AutoUpdateVarNames => ["ConstrictAmount"];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        if (CombatState == null)
        {
            return;
        }

        bool flashed = false;
        foreach (var player in CombatState.Players)
        {
            if (player.Creature.IsDead || player.Creature.HasPower<ConstrictPower>())
            {
                continue;
            }

            if (!flashed)
            {
                Flash();
                flashed = true;
            }

            await PowerCmd.Apply<ConstrictPower>(new ThrowingPlayerChoiceContext(), player.Creature, 2 * Amount, Owner, null);
        }
    }
}
