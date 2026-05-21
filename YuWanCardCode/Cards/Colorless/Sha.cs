using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Abstracts;

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
        WithDamage(9);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
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

        if (!attackCommand.Results.Any(result => result.WasTargetKilled)) return;

        YUWANCARD_PermanentReplayCount += 1;
        BaseReplayCount = YUWANCARD_PermanentReplayCount;

        if (DeckVersion is Sha deckSha)
        {
            deckSha.YUWANCARD_PermanentReplayCount += 1;
            deckSha.BaseReplayCount = deckSha.YUWANCARD_PermanentReplayCount;
        }
    }
}
