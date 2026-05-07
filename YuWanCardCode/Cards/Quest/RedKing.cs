using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace YuWanCard.Cards.Quest;

[Pool(typeof(QuestCardPool))]
public class RedKing : YuWanCardModel
{
    public override int MaxUpgradeLevel => 0;

    [SavedProperty]
    public int CombatsCompleted { get; set; }

    public RedKing() : base(
        baseCost: -1,
        type: CardType.Quest,
        rarity: CardRarity.Quest,
        target: TargetType.None)
    {
        WithVar("Combats", 3);
        WithKeywords(CardKeyword.Unplayable);
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        CombatsCompleted++;

        if (CombatsCompleted >= DynamicVars["Combats"].BaseValue)
        {
            var sharedPool = ModelDb.RelicPool<SharedRelicPool>();
            var ancientRelics = sharedPool.AllRelics
                .Where(r => r.Rarity == RelicRarity.Ancient)
                .Select(r => r.ToMutable())
                .ToList();

            if (ancientRelics.Count > 0)
            {
                var selectedRelic = Owner.RunState.Rng.Niche.NextItem(ancientRelics);
                if (selectedRelic != null)
                {
                    await RelicCmd.Obtain(selectedRelic, Owner);
                }
            }

            await CardPileCmd.RemoveFromDeck(this);
            PlayerCmd.CompleteQuest(this);
        }
    }
}
