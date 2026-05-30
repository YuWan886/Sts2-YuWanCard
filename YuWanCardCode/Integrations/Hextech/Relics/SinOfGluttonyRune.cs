using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Hextech;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class SinOfGluttonyRune : HextechSharedRuneBase
{
    private int _exhaustTriggersThisTurn;
    private int _foodTriggersThisTurn;

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m),
        new DynamicVar("TriggerLimit", 4m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override Task BeforeCombatStart()
    {
        _exhaustTriggersThisTurn = 0;
        _foodTriggersThisTurn = 0;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _exhaustTriggersThisTurn = 0;
        _foodTriggersThisTurn = 0;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            _exhaustTriggersThisTurn = 0;
            _foodTriggersThisTurn = 0;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (Owner == null || card.Owner != Owner || _exhaustTriggersThisTurn >= DynamicVars["TriggerLimit"].IntValue)
        {
            return;
        }

        _exhaustTriggersThisTurn++;
        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner!.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, card);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (Owner == null
            || cardPlay.Card.Owner != Owner
            || !cardPlay.Card.Tags.Contains(YuWanTags.FoodPig)
            || _foodTriggersThisTurn >= DynamicVars["TriggerLimit"].IntValue)
        {
            return;
        }

        _foodTriggersThisTurn++;
        Flash();
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner!.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, cardPlay.Card);
    }
}
