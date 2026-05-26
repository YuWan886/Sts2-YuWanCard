using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Cards;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfDirectWin : WhatIfRelicModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<SadArmyWin>();

    public WhatIfDirectWin() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner?.Creature == null)
        {
            return;
        }

        decimal targetHp = Math.Max(1m, Math.Ceiling(Owner.Creature.MaxHp * 0.1m));
        if (Owner.Creature.CurrentHp > targetHp)
        {
            await CreatureCmd.SetCurrentHp(Owner.Creature, targetHp);
        }

        var sadArmyWin = Owner.RunState.CreateCard(ModelDb.Card<SadArmyWin>(), Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(sadArmyWin, PileType.Deck));
    }
}
