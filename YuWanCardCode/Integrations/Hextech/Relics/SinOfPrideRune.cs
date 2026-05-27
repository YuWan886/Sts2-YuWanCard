using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Hextech;

namespace YuWanCard.Relics;

public sealed class SinOfPrideRune : HextechSharedRuneBase
{
    private int _gainedStrengthThisCombat;

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new PowerVar<StrengthPower>(3m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override Task BeforeCombatStart()
    {
        _gainedStrengthThisCombat = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner == null || Owner.Creature.CurrentHp < Owner.Creature.MaxHp)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (Owner == null || creature.Side != MegaCrit.Sts2.Core.Combat.CombatSide.Enemy)
        {
            return;
        }

        int amount = HextechPigRuneSharedState.ScaleWithRingBonus(this, DynamicVars.Strength.IntValue, 1);
        _gainedStrengthThisCombat += amount;
        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, amount, Owner.Creature, null);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, MegaCrit.Sts2.Core.ValueProps.ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || target != Owner.Creature || result.UnblockedDamage <= 0 || _gainedStrengthThisCombat <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, -_gainedStrengthThisCombat, Owner.Creature, null);
        _gainedStrengthThisCombat = 0;
    }
}
