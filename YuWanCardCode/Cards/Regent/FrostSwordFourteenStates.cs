using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Localization;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(RegentCardPool))]
public class FrostSwordFourteenStates : YuWanCardModel
{
    public override int CanonicalStarCost => 2;

    public FrostSwordFourteenStates() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithVar("Forge", 14);
        WithTips(_ => HoverTipFactory.FromForge());
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        int playerCount = Math.Max(CombatState?.Players.Count ?? 1, 1);
        int totalForge = DynamicVars["Forge"].IntValue * playerCount;
        description.Add("TotalForge", totalForge);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int playerCount = Math.Max(CombatState?.Players.Count ?? 1, 1);
        int forgeAmount = DynamicVars["Forge"].IntValue * playerCount;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await ForgeCmd.Forge(forgeAmount, Owner, this);
    }
}
