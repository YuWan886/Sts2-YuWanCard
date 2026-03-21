using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class RainDark : YuWanCardModel
{
    private const float HpPercentage = 0.25f;
    private const int MaxHandSize = 10;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<IntangiblePower>(3m),
        new PowerVar<RainDarkPower>(3m)
    ];

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.FromPower<RainDarkPower>()
    ];

    public RainDark() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Ancient,
        target: TargetType.AllAllies
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var teammates = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer)
            .ToList();

        foreach (var teammate in teammates)
        {
            int targetHp = (int)(teammate.MaxHp * HpPercentage);
            await CreatureCmd.SetCurrentHp(teammate, targetHp);

            await CommonActions.Apply<IntangiblePower>(teammate, this, DynamicVars["IntangiblePower"].IntValue);
            await CommonActions.Apply<RainDarkPower>(teammate, this, DynamicVars["RainDarkPower"].IntValue);

            var player = teammate.Player;
            if (player != null && player.PlayerCombatState != null)
            {
                int currentEnergy = player.PlayerCombatState.Energy;
                if (currentEnergy > 0)
                {
                    await PlayerCmd.GainEnergy(currentEnergy, player);
                }

                var hand = MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand.GetPile(player);
                int cardsToDraw = MaxHandSize - hand.Cards.Count;
                if (cardsToDraw > 0)
                {
                    await CardPileCmd.Draw(choiceContext, cardsToDraw, player);
                }
            }
        }
    }

    public override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
