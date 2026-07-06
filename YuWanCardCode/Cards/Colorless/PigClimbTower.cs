using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigClimbTower : YuWanCardModel
{
    public PigClimbTower() : base(
        baseCost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithVars(new PigClimbTowerDamageVar(), new PigClimbTowerBlockVar());
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int floor = Owner.RunState.TotalFloor;
        if (floor <= 0) return;

        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(floor)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        await CreatureCmd.GainBlock(Owner.Creature, floor, ValueProp.Unpowered, null);
    }
}

public class PigClimbTowerDamageVar : DamageVar
{
    public const string Key = "PigClimbTowerDamage";

    public PigClimbTowerDamageVar() : base(Key, 0, ValueProp.Move) { }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        int floor = card.Owner?.RunState.TotalFloor ?? 0;
        BaseValue = floor;
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
    }
}

public class PigClimbTowerBlockVar : BlockVar
{
    public const string Key = "PigClimbTowerBlock";

    public PigClimbTowerBlockVar() : base(Key, 0, ValueProp.Unpowered) { }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        int floor = card.Owner?.RunState.TotalFloor ?? 0;
        BaseValue = floor;
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
    }
}
