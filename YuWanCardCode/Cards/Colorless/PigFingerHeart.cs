using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigFingerHeart : YuWanCardModel
{
    static PigFingerHeart()
    {
        SavedPropertyRegistration.RegisterType(typeof(PigFingerHeart));
    }

    [SavedProperty]
    public int YUWANCARD_Uses { get; set; }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public PigFingerHeart() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: CustomTargetType.AnyOtherPlayer)
    {
        WithVar("MaxHpGain", 3);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MaxHpGain"].UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var targetPlayer = cardPlay.Target?.Player;
        if (targetPlayer == null || targetPlayer == Owner)
        {
            return;
        }

        await CreatureCmd.GainMaxHp(targetPlayer.Creature, DynamicVars["MaxHpGain"].IntValue);

        if (IncreaseUseCount() >= 3)
        {
            await CardPileCmd.RemoveFromDeck(DeckVersion ?? this, showPreview: false);
        }
    }

    private int IncreaseUseCount()
    {
        if (DeckVersion is PigFingerHeart deckCard && !ReferenceEquals(deckCard, this))
        {
            deckCard.YUWANCARD_Uses += 1;
            MirrorUsesLocally(deckCard.YUWANCARD_Uses);
            return deckCard.YUWANCARD_Uses;
        }

        YUWANCARD_Uses += 1;
        return YUWANCARD_Uses;
    }

    private void MirrorUsesLocally(int useCount)
    {
        using (SavedPropertyMultiplayerSync.SuppressNotifications())
        {
            YUWANCARD_Uses = useCount;
        }
    }
}
