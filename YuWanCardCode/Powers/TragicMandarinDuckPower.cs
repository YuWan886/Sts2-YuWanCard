using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers;

public class TragicMandarinDuckPower : YuWanPowerModel
{
    private class Data
    {
        public bool AttackPlayedThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>("GainStrength", 1),
        new PowerVar<DexterityPower>("GainDexterity", 1),
        new DynamicVar("HpLoss", 1)
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        DynamicVars["GainStrength"].BaseValue = cardSource is { IsUpgraded: true } ? 2m : 1m;
        DynamicVars["GainDexterity"].BaseValue = cardSource is { IsUpgraded: true } ? 2m : 1m;
        DynamicVars["HpLoss"].BaseValue = 1m;
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            GetInternalData<Data>().AttackPlayedThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner, DynamicVars["GainStrength"].IntValue, Owner, null);
        await PowerCmd.Apply<DexterityPower>(Owner, DynamicVars["GainDexterity"].IntValue, Owner, null);
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
            await CreatureCmd.Damage(choiceContext, Owner, DynamicVars["HpLoss"].IntValue, ValueProp.Move, Owner);
        }
    }
}
