using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Hextech;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class PigletDashRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Silver;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3m, ValueProp.Unpowered)];

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || !cardPlay.Card.Tags.Contains(YuWanTags.FoodPig))
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner!.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
    }
}
