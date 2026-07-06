using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core;
using YuWanCard.Core.Abstracts;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class OldPigCalendar : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public OldPigCalendar() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: CustomTargetType.AnyOtherPlayer)
    {
        WithPower<OldPigCalendarDoubleDamagePower>(1);
        WithPower<OldPigCalendarNoDamagePower>(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
        {
            return;
        }

        var teammate = cardPlay.Target;
        if (teammate?.Player == null || teammate.Player == Owner)
        {
            return;
        }

        var transferableBuffs = teammate.Powers
            .Where(power => power.IsVisible && power.Type == PowerType.Buff)
            .ToList();

        foreach (var power in transferableBuffs)
        {
            var canonical = ModelDb.GetByIdOrNull<PowerModel>(power.Id);
            if (canonical == null)
            {
                continue;
            }

            int amount = power.Amount <= 0 ? 1 : power.Amount;
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), canonical.ToMutable(), Owner.Creature, amount, Owner.Creature, this);
            await PowerCmd.Remove(power);
        }

        await PowerCmd.Apply<OldPigCalendarDoubleDamagePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
        await PowerCmd.Apply<OldPigCalendarDoubleDamagePower>(new ThrowingPlayerChoiceContext(), teammate, 1, Owner.Creature, this);

        var otherTeammates = Owner.Creature.CombatState.PlayerCreatures
            .Where(creature => creature.IsAlive
                               && creature.Player != null
                               && creature != Owner.Creature
                               && creature != teammate)
            .ToList();

        foreach (var otherTeammate in otherTeammates)
        {
            await PowerCmd.Apply<OldPigCalendarNoDamagePower>(new ThrowingPlayerChoiceContext(), otherTeammate, 1, Owner.Creature, this);
        }
    }
}
