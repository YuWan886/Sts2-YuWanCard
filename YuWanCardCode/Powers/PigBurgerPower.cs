using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigBurgerPower : YuWanPowerModel
{
    private sealed class Data
    {
        public bool TriggeredFoodDrawThisCombat;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PigBurgerPower", 6m)];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || !cardPlay.Card.Tags.Contains(YuWanTags.FoodPig))
        {
            return;
        }

        var data = GetInternalData<Data>();
        if (data.TriggeredFoodDrawThisCombat)
        {
            return;
        }

        data.TriggeredFoodDrawThisCombat = true;
        Flash();
        await CardPileCmd.Draw(context, 1, Owner.Player!);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await CreatureCmd.Heal(Owner, Amount);
    }
}
