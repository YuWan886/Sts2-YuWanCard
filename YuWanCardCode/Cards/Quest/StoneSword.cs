using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace YuWanCard.Cards.Quest;

[Pool(typeof(QuestCardPool))]
public class StoneSword : YuWanCardModel
{
    public override int MaxUpgradeLevel => 0;

    [SavedProperty]
    public int ElitesDefeated { get; set; }

    public StoneSword() : base(
        baseCost: -1,
        type: CardType.Quest,
        rarity: CardRarity.Quest,
        target: TargetType.None)
    {
        WithTip(typeof(SwordOfStone));
        WithVar("Elites", 5);
        WithKeywords(CardKeyword.Unplayable);
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (room.RoomType == RoomType.Elite)
        {
            ElitesDefeated++;

            if (ElitesDefeated >= DynamicVars["Elites"].BaseValue)
            {
                var relic = ModelDb.Relic<SwordOfStone>().ToMutable();
                await RelicCmd.Obtain(relic, Owner);
                await CardPileCmd.RemoveFromDeck(this);
                PlayerCmd.CompleteQuest(this);
            }
        }
    }
}
