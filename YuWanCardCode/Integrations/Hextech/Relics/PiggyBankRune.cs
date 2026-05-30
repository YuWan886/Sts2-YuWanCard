using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Hextech;
using YuWanCard.Powers;

namespace YuWanCard.Relics;

public sealed class PiggyBankRune : HextechSharedRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Silver;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PigChargePower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task AfterGoldGained(Player player)
    {
        if (player != Owner || !HextechPigRuneSharedState.RollPercent(this, 0.5f, 0.25f))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<PigChargePower>(new ThrowingPlayerChoiceContext(), Owner!.Creature, DynamicVars["PigChargePower"].BaseValue, Owner.Creature, null);
    }
}
