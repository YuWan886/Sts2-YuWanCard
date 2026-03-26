using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class YouArePig : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public YouArePig() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly)
    {
        WithPower<BufferPower>(1);
        WithPower<RegenPower>(3);
        WithPower<YouArePigPower>(1);
        WithKeywords(CardKeyword.Ethereal);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<BufferPower>()));
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<RegenPower>()));
    }

    public override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
        AddKeyword(CardKeyword.Exhaust);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetCreature = cardPlay.Target;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<BufferPower>(targetCreature, DynamicVars["BufferPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<RegenPower>(targetCreature, DynamicVars["RegenPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<YouArePigPower>(targetCreature, DynamicVars["YouArePigPower"].BaseValue, Owner.Creature, this);

        var targetPlayer = targetCreature.Player;
        if (targetPlayer != null)
        {
            PlayerCmd.EndTurn(targetPlayer, canBackOut: false);
        }
    }
}
