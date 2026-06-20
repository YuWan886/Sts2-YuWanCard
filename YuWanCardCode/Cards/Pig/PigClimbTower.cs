using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
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

public class PigClimbTowerDamageVar : DynamicVar
{
    public const string Key = "PigClimbTowerDamage";

    public PigClimbTowerDamageVar() : base(Key, 0) { }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        int floor = card.Owner?.RunState.TotalFloor ?? 0;
        BaseValue = floor;
        PreviewValue = floor;
    }
}

public class PigClimbTowerBlockVar : DynamicVar
{
    public const string Key = "PigClimbTowerBlock";

    public PigClimbTowerBlockVar() : base(Key, 0) { }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        int floor = card.Owner?.RunState.TotalFloor ?? 0;
        BaseValue = floor;
        PreviewValue = floor;
    }
}
