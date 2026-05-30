using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Hextech;

namespace YuWanCard.Relics;

public sealed class SinOfSlothRune : HextechSharedRuneBase
{
    private int _cardsPlayedThisTurn;

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardThreshold", 3),
        new PowerVar<StrengthPower>(2m),
        new PowerVar<PlatingPower>(3m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<PlatingPower>()
    ];

    public override Task BeforeCombatStart()
    {
        _cardsPlayedThisTurn = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        // Check previous turn's card count before resetting
        if (_cardsPlayedThisTurn > 0 && _cardsPlayedThisTurn <= DynamicVars["CardThreshold"].IntValue)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
            await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, null);
        }

        _cardsPlayedThisTurn = 0;
    }
}
