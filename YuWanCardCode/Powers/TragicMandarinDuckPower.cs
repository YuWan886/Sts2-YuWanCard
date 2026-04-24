using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers;

public class TragicMandarinDuckPower : YuWanPowerModel
{
    private class Data
    {
        public bool AttackPlayedThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(2),
        new PowerVar<DexterityPower>(2),
        new DynamicVar("Damage", 6)
    ];

    [SavedProperty]
    public int YUWANCARD_StrengthAmount { get; set; } = 2;

    [SavedProperty]
    public int YUWANCARD_DexterityAmount { get; set; } = 2;

    [SavedProperty]
    public int YUWANCARD_DamageAmount { get; set; } = 6;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (cardSource is { IsUpgraded: true })
        {
            YUWANCARD_StrengthAmount = 3;
            YUWANCARD_DexterityAmount = 3;
            DynamicVars.Strength.BaseValue = 3m;
            DynamicVars.Dexterity.BaseValue = 3m;
        }
        await base.BeforeApplied(target, amount, applier, cardSource);
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, YUWANCARD_StrengthAmount, Owner, cardSource);
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner, YUWANCARD_DexterityAmount, Owner, cardSource);
        await base.AfterApplied(applier, cardSource);
    }

    public override Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            GetInternalData<Data>().AttackPlayedThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner) return Task.CompletedTask;
        if (cardPlay.Card.Type == CardType.Attack)
        {
            GetInternalData<Data>().AttackPlayedThisTurn = true;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;

        var data = GetInternalData<Data>();
        if (!data.AttackPlayedThisTurn)
        {
            Flash();

            var strengthPower = Owner.GetPower<StrengthPower>();
            var dexterityPower = Owner.GetPower<DexterityPower>();

            if (strengthPower != null)
            {
                await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -1, Owner, null);
            }
            if (dexterityPower != null)
            {
                await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, -1, Owner, null);
            }

            var enemies = CombatState?.HittableEnemies;
            if (enemies != null && enemies.Count > 0)
            {
                foreach (var enemy in enemies.ToList())
                {
                    if (enemy == null || !enemy.IsAlive) continue;
                    
                    await CreatureCmd.Damage(choiceContext, enemy, YUWANCARD_DamageAmount, ValueProp.Move, Owner);
                }
            }
        }
    }
}
