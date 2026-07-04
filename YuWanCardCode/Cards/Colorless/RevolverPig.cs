using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class RevolverPig : YuWanCardModel
{
    private const float HitChance = 1f / 6f;
    private const string RevolverLoadSfxPath = "res://YuWanCard/sounds/vfx/revolver_load.mp3";
    private const string RevolverFireSfxPath = "res://YuWanCard/sounds/vfx/revolver_fire.mp3";

    private bool _hasPlayedLoadSfx;

    public RevolverPig() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyPlayer)
    {
        WithVar("Gold", 20, 5);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
        {
            return;
        }

        var targetPlayer = cardPlay.Target?.Player ?? Owner;
        if (targetPlayer?.Creature == null || targetPlayer.Creature.IsDead)
        {
            return;
        }

        bool hit = DeterministicRandomUtils.RollProbability(Owner.RunState.Rng.CombatCardSelection, HitChance);
        if (hit)
        {
            AudioUtils.Play(RevolverFireSfxPath);
            await RemoveSelfPermanently();
            await CreatureCmd.Kill(targetPlayer.Creature, force: true);
            return;
        }

        await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, targetPlayer);
    }

    protected override PileType GetResultPileTypeForCardPlay()
    {
        return PileType.Hand;
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _hasPlayedLoadSfx = false;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this || _hasPlayedLoadSfx || card.Pile?.Type != PileType.Hand)
        {
            return Task.CompletedTask;
        }

        _hasPlayedLoadSfx = true;
        AudioUtils.Play(RevolverLoadSfxPath);
        return Task.CompletedTask;
    }

    private async Task RemoveSelfPermanently()
    {
        if (Pile?.IsCombatPile == true)
        {
            await CardPileCmd.RemoveFromCombat(this, skipVisuals: true);
        }

        if (DeckVersion?.Pile?.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(DeckVersion, showPreview: false);
        }
    }
}
