using YuWanCard.Core.Abstracts;
using YuWanCard.Cards.Event;
using YuWanCard.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Events;

public sealed class HelloHuman : YuWanEventModel
{
    private const int OptionsShown = 2;
    private const int MaxHpGain = 5;
    private const int Gold = 25;
    private const int MaxHpLoss = 5;

    public override ActModel[] Acts => [];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // 4 个候选选项，随机抽 2 个展示给玩家。
        // 事件 Rng 由种子确定性生成，多人环境下各客户端结果一致。
        var candidates = new List<EventOption>
        {
            new(this, TakeRose,
                $"{Id.Entry}.pages.INITIAL.options.TAKE_ROSE",
                HoverTipFactory.FromCardWithCardHoverTips<PiggyBlessing>()),

            new(this, Chat,
                $"{Id.Entry}.pages.INITIAL.options.CHAT",
                HoverTipFactory.FromRelic<SoftWarmth>()),

            new(this, Leave,
                $"{Id.Entry}.pages.INITIAL.options.LEAVE",
                HoverTipFactory.FromRelic<GoodEncounter>()),

            new EventOption(this, Poke,
                $"{Id.Entry}.pages.INITIAL.options.POKE",
                HoverTipFactory.FromRelic<PiggyDoll>())
                .ThatDecreasesMaxHp(MaxHpLoss),
        };

        Rng.Shuffle(candidates);
        return candidates.Take(OptionsShown).ToList();
    }

    private async Task TakeRose()
    {
        await CreatureCmd.GainMaxHp(Owner!.Creature, MaxHpGain);

        var card = Owner.RunState.CreateCard(ModelDb.Card<PiggyBlessing>(), Owner);
        var addResult = await CardPileCmd.Add(card, PileType.Deck);
        if (addResult.success)
        {
            CardCmd.PreviewCardPileAdd(addResult, 2f);
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.TOOK_ROSE.description"));
    }

    private async Task Chat()
    {
        await PlayerCmd.GainGold(Gold, Owner!);
        await RelicCmd.Obtain<SoftWarmth>(Owner!);

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.CHATTED.description"));
    }

    private async Task Leave()
    {
        await RelicCmd.Obtain<GoodEncounter>(Owner!);

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LEFT.description"));
    }

    private async Task Poke()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner!.Creature, MaxHpLoss, isFromCard: false);
        await RelicCmd.Obtain<PiggyDoll>(Owner);

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.POKED.description"));
    }
}
