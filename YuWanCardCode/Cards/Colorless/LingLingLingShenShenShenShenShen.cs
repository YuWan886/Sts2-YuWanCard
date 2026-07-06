using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core;
using YuWanCard.Core.Extensions;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class LingLingLingShenShenShenShenShen : YuWanCardModel
{
    private const int DrawPileCopyCount = 3;
    private const int AutoPlayCount = 3;

    public override int MaxUpgradeLevel => 0;

    public LingLingLingShenShenShenShenShen() : base(
        baseCost: 4,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override bool IsPlayable => base.IsPlayable && GetSelectableCards().Count > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
        {
            return;
        }

        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1),
            IsSelectableCard,
            this)).FirstOrDefault();
        if (selectedCard == null)
        {
            return;
        }

        for (int i = 0; i < DrawPileCopyCount; i++)
        {
            CardModel? drawPileCopy = CardCopyHelper.CreateCombatCopy(selectedCard, Owner);
            if (drawPileCopy == null)
            {
                continue;
            }

            CardPileAddResult addResult = await CardPileCmd.AddGeneratedCardToCombat(drawPileCopy, PileType.Draw, Owner);
            CardCmd.PreviewCardPileAdd(addResult);
        }

        CardModel? autoPlayCopy = CardCopyHelper.CreateCombatCopy(selectedCard, Owner);
        if (autoPlayCopy == null)
        {
            return;
        }

        autoPlayCopy.BaseReplayCount = AutoPlayCount - 1;

        Creature? target = GetAutoPlayTarget(autoPlayCopy);
        if (NeedsExplicitTarget(autoPlayCopy) && target == null)
        {
            return;
        }

        await CardCmd.AutoPlay(choiceContext, autoPlayCopy, target, AutoPlayType.Default, skipXCapture: true);
    }

    private List<CardModel> GetSelectableCards()
    {
        if (Owner == null)
        {
            return [];
        }

        return PileType.Hand.GetPile(Owner).Cards
            .Where(IsSelectableCard)
            .ToList();
    }

    private bool IsSelectableCard(CardModel card)
    {
        return card.Id != Id;
    }

    private Creature? GetAutoPlayTarget(CardModel card)
    {
        if (Owner?.Creature?.CombatState is not { } combatState)
        {
            return null;
        }

        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(),
            var targetType when targetType == CustomTargetType.AnyOtherPlayer => combatState.Allies
                .FirstOrDefault(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature),
            TargetType.AnyAlly => combatState.Allies
                .FirstOrDefault(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature),
            TargetType.AnyPlayer => Owner.Creature,
            _ => card.PickRandomTarget()
        };
    }

    private static bool NeedsExplicitTarget(CardModel card)
    {
        return card.TargetType is TargetType.AnyEnemy or TargetType.AnyAlly or TargetType.AnyPlayer
               || CustomTargetType.IsCustomSingleTargetType(card.TargetType);
    }
}
