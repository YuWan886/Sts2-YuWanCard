using YuWanCard.Core.Abstracts;
using YuWanCard.Cards.Quest;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Events;

public sealed class SunkenStatueQuest : YuWanEventModel
{
    public override ActModel[] Acts => [];

    protected override string? CustomEventImagePath => "res://images/events/sunken_statue.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("Card", ModelDb.Card<StoneSword>().Title),
        new GoldVar(111),
        new DynamicVar("HpLoss", 7m)
    ];

    public override void CalculateVars()
    {
        DynamicVars.Gold.BaseValue += Rng.NextInt(-10, 11);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, GrabSword, $"{Id.Entry}.pages.INITIAL.options.GRAB_SWORD", HoverTipFactory.FromCardWithCardHoverTips<StoneSword>()),
            new EventOption(this, DiveIntoWater, $"{Id.Entry}.pages.INITIAL.options.DIVE_INTO_WATER").ThatDoesDamage(DynamicVars["HpLoss"].BaseValue)
        ];
    }

    private async Task GrabSword()
    {
        var stoneSwordCard = Owner!.RunState.CreateCard(ModelDb.Card<StoneSword>(), Owner);
        var addResult = await CardPileCmd.Add(stoneSwordCard, PileType.Deck);

        if (addResult.success)
        {
            CardCmd.PreviewCardPileAdd(addResult, 2f);
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.GRAB_SWORD.description"));
    }

    private async Task DiveIntoWater()
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner!);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner!.Creature, DynamicVars["HpLoss"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.DIVE_INTO_WATER.description"));
    }
}
