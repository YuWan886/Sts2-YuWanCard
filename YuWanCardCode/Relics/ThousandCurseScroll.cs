using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class ThousandCurseScroll : YuWanRelicModel
{
    private const int MaxStrengthGain = 5;
    private const int MaxDexterityGain = 5;

    [SavedProperty]
    private int YUWANCARD_StrengthGrantedThisCombat { get; set; }

    [SavedProperty]
    private int YUWANCARD_DexterityGrantedThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Event;

    public ThousandCurseScroll() : base(true)
    {
    }

    public override bool IsAllowed(IRunState runState) => false;

    public override Task BeforeCombatStart()
    {
        YUWANCARD_StrengthGrantedThisCombat = 0;
        YUWANCARD_DexterityGrantedThisCombat = 0;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        YUWANCARD_StrengthGrantedThisCombat = 0;
        YUWANCARD_DexterityGrantedThisCombat = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (Owner == null || card.Owner != Owner || card.Type != CardType.Curse || oldPileType != PileType.None)
        {
            return;
        }

        var currentPile = card.Pile;
        if (currentPile == null)
        {
            return;
        }

        Flash();

        if (currentPile.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(card, showPreview: false);
        }
        else
        {
            await CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom, this);
        }

        var ownerCreature = Owner.Creature;
        if (ownerCreature?.CombatState == null)
        {
            return;
        }

        int strengthToGain = Math.Max(0, MaxStrengthGain - YUWANCARD_StrengthGrantedThisCombat) > 0 ? 1 : 0;
        int dexterityToGain = Math.Max(0, MaxDexterityGain - YUWANCARD_DexterityGrantedThisCombat) > 0 ? 1 : 0;

        if (strengthToGain > 0)
        {
            await PowerCmd.Apply<StrengthPower>(ownerCreature, strengthToGain, ownerCreature, card);
            YUWANCARD_StrengthGrantedThisCombat += strengthToGain;
        }

        if (dexterityToGain > 0)
        {
            await PowerCmd.Apply<DexterityPower>(ownerCreature, dexterityToGain, ownerCreature, card);
            YUWANCARD_DexterityGrantedThisCombat += dexterityToGain;
        }
    }
}
