using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Cards;

namespace YuWanCard.Events;

public sealed class HorizonEvent : CustomEventModel
{
    public override ActModel[] Acts => [];

    public override string? CustomInitialPortraitPath => "res://YuWanCard/images/events/horizon_event.png";

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex >= 2;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(
                this,
                AddHorizonCard,
                $"{Id.Entry}.pages.INITIAL.options.ADD_CARD",
                HoverTipFactory.FromCardWithCardHoverTips<Horizon>()
            ),
            new EventOption(this, HealAndMaxHp, $"{Id.Entry}.pages.INITIAL.options.HEAL_MAX_HP")
        ];
    }

    private async Task AddHorizonCard()
    {
        var horizonCard = Owner!.RunState.CreateCard(ModelDb.Card<Horizon>(), Owner);
        var addResult = await CardPileCmd.Add(horizonCard, PileType.Deck);

        if (addResult.success)
        {
            CardCmd.PreviewCardPileAdd(addResult, 2f);
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.CARD_ADDED.description"));
    }

    private async Task HealAndMaxHp()
    {
        await CreatureCmd.Heal(Owner!.Creature, Owner.Creature.MaxHp - Owner.Creature.CurrentHp, playAnim: true);
        await CreatureCmd.GainMaxHp(Owner.Creature, 10m);

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.HEALED.description"));
    }
}
