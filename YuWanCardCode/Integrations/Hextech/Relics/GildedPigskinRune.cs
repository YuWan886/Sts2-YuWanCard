using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Hextech;

namespace YuWanCard.Relics;

public sealed class GildedPigskinRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PlatingThreshold", 5m),
        new PowerVar<StrengthPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player != Owner)
        {
            return;
        }

        int plating = Owner.Creature.GetPower<PlatingPower>()?.Amount ?? 0;
        int bonus = plating / DynamicVars["PlatingThreshold"].IntValue;
        if (bonus <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, bonus * DynamicVars.Strength.BaseValue, Owner.Creature, null);
    }
}
