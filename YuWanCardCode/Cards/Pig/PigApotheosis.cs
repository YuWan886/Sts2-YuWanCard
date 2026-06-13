using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigApotheosis : YuWanCardModel, IDustyTomeCard
{
    public override int MaxUpgradeLevel => 2;

    public PigApotheosis() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Ancient,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Innate, CardKeyword.Exhaust);
    }

    public CharacterModel GetDustyTomeCharacter() => ModelDb.Character<Pig>();

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("EffectText",
            new LocString(
                "cards",
                CurrentUpgradeLevel >= 2
                    ? $"{Id.Entry}.upgrade2EffectText"
                    : $"{Id.Entry}.upgrade1EffectText").GetRawText());
    }

    protected override void OnUpgrade()
    {
        if (CurrentUpgradeLevel == 1)
        {
            EnergyCost.UpgradeBy(-1);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        if (CurrentUpgradeLevel >= 2)
        {
            foreach (var player in CombatState?.Players ?? [])
            {
                if (player.PlayerCombatState == null)
                {
                    continue;
                }

                UpgradeCards(player.PlayerCombatState.AllCards);
            }

            return;
        }

        if (Owner.PlayerCombatState != null)
        {
            UpgradeCards(Owner.PlayerCombatState.AllCards);
        }
    }

    private void UpgradeCards(IEnumerable<CardModel> cards)
    {
        foreach (var card in cards)
        {
            if (card != this && card.IsUpgradable)
            {
                CardCmd.Upgrade(card);
            }
        }
    }
}
