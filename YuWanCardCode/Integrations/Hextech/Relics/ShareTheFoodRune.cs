using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Hextech;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class ShareTheFoodRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Silver;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PigChargePower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner
            || !cardPlay.Card.Tags.Contains(YuWanTags.FoodPig)
            || Owner?.Creature?.CombatState == null
            || Owner.Creature.CombatState.Players.Count <= 1)
        {
            return;
        }

        Flash();
        foreach (var player in Owner.Creature.CombatState.Players)
        {
            if (player == Owner || player.Creature.IsDead)
            {
                continue;
            }

            await PowerCmd.Apply<PigChargePower>(player.Creature, DynamicVars["PigChargePower"].BaseValue, Owner.Creature, cardPlay.Card);
        }
    }
}
