using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class Sha : YuWanCardModel
{
    static Sha()
    {
        SavedPropertyRegistration.RegisterType(typeof(Sha));
    }

    [SavedProperty]
    public int YUWANCARD_PermanentReplayCount { get; set; }

    public Sha() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Rare,
        target: TargetType.AnyEnemy)
    {
        WithDamage(9, 3);
        WithKeywords(CardKeyword.Exhaust);
    }



    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        BaseReplayCount = YUWANCARD_PermanentReplayCount;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (!attackCommand.Results.SelectMany(r => r).Any(result => result.WasTargetKilled)) return;
        IncreasePermanentReplayCount();
    }

    private void IncreasePermanentReplayCount()
    {
        if (DeckVersion is Sha deckSha && !ReferenceEquals(deckSha, this))
        {
            deckSha.YUWANCARD_PermanentReplayCount += 1;
            deckSha.BaseReplayCount = deckSha.YUWANCARD_PermanentReplayCount;
            MirrorPermanentReplayCountLocally(deckSha.YUWANCARD_PermanentReplayCount);
            return;
        }

        YUWANCARD_PermanentReplayCount += 1;
        BaseReplayCount = YUWANCARD_PermanentReplayCount;
    }

    private void MirrorPermanentReplayCountLocally(int replayCount)
    {
        using (SavedPropertyMultiplayerSync.SuppressNotifications())
        {
            YUWANCARD_PermanentReplayCount = replayCount;
        }

        BaseReplayCount = replayCount;
    }
}
