using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class ThousandCurseScroll : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public ThousandCurseScroll() : base(true)
    {
    }

    public override bool IsAllowed(IRunState runState) => false;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        int curseCount = PileType.Hand.GetPile(player).Cards.Count(card => card.Type == CardType.Curse);
        if (curseCount <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<ThousandCurseScrollStrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, curseCount, Owner.Creature, null);
        await PowerCmd.Apply<ThousandCurseScrollDexterityPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, curseCount, Owner.Creature, null);
        
    }
}
