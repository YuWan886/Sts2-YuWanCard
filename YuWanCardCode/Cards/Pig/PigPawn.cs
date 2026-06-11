using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Monsters;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigPawn : YuWanCardModel
{
    public PigPawn() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: CustomTargetType.AnyPigPawnTarget)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<YouArePigPower>()));
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { IsDead: false } target)
            return;

        if (target.Monster is PigMinion)
        {
            int goldToGain = target.CurrentHp / 5;
            if (goldToGain > 0)
            {
                await PlayerCmd.GainGold(goldToGain, Owner);
            }

            await PetManager.KillPet(target);
            return;
        }

        if (!target.HasPower<YouArePigPower>())
            return;

        int hpToLose = target.CurrentHp / 2;
        if (hpToLose <= 0)
            return;

        int hpBefore = target.CurrentHp;
        await CreatureCmd.Damage(
            choiceContext,
            target,
            hpToLose,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Owner.Creature,
            this);

        int gainedGold = hpBefore - target.CurrentHp;
        if (gainedGold > 0)
        {
            await PlayerCmd.GainGold(gainedGold, Owner);
        }
    }
}
