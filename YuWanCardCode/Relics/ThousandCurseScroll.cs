using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Persistence;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class ThousandCurseScroll : YuWanRelicModel
{
    private const int MaxStrengthGain = 5;
    private const int MaxDexterityGain = 5;
    private static readonly SavedAttachedState<ThousandCurseScroll, int> GrantsThisCombatState =
        new(nameof(GrantsThisCombat), () => 0);

    private int GrantsThisCombat
    {
        get => GrantsThisCombatState.GetValueOrDefault(this, 0);
        set => GrantsThisCombatState[this] = value;
    }

    public override RelicRarity Rarity => RelicRarity.Event;

    public ThousandCurseScroll() : base(true)
    {
    }

    public override bool IsAllowed(IRunState runState) => false;

    public override Task BeforeCombatStart()
    {
        GrantsThisCombat = 0;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        GrantsThisCombat = 0;
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

        int strengthToGain = Math.Max(0, MaxStrengthGain - GrantsThisCombat) > 0 ? 1 : 0;
        int dexterityToGain = Math.Max(0, MaxDexterityGain - GrantsThisCombat) > 0 ? 1 : 0;

        if (strengthToGain > 0)
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), ownerCreature, strengthToGain, ownerCreature, card);
        }

        if (dexterityToGain > 0)
        {
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), ownerCreature, dexterityToGain, ownerCreature, card);
        }

        if (strengthToGain > 0 || dexterityToGain > 0)
        {
            GrantsThisCombat += 1;
        }
    }
}
