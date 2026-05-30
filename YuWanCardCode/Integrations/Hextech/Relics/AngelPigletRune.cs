using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Hextech;
using YuWanCard.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YuWanCard.Relics;

public sealed class AngelPigletRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Prismatic;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<AngelPigPower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<IntangiblePower>()];

    public override async Task BeforeCombatStart()
    {
        if (Owner == null)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<AngelPigPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["AngelPigPower"].BaseValue, Owner.Creature, null);
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (Owner == null || creature != Owner.Creature)
        {
            return;
        }

        await PowerCmd.Apply<IntangiblePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    }
}
