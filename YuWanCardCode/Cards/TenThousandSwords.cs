using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace YuWanCard.Cards;

[Pool(typeof(RegentCardPool))]
public class TenThousandSwords : YuWanCardModel
{
    private const int BaseBladeDamage = 10;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override IEnumerable<DynamicVar> CanonicalVars => [new ForgeVar(0)];

    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromForge();

    public TenThousandSwords() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var combatState = CombatState;
        if (combatState == null) return;

        var teammates = combatState.Players
            .Where(p => p.Creature.IsAlive && p != owner)
            .ToList();

        if (teammates.Count == 0) return;

        decimal totalForge = 0m;

        foreach (var teammate in teammates)
        {
            var blades = GetSovereignBlades(teammate, includeExhausted: true);
            foreach (var blade in blades)
            {
                totalForge += blade.DynamicVars.Damage.BaseValue - BaseBladeDamage;
                blade.DynamicVars.Damage.BaseValue = BaseBladeDamage;
            }

            var handBlades = GetSovereignBlades(teammate, includeExhausted: false)
                .Where(b => b.Pile?.Type == PileType.Hand)
                .ToList();

            foreach (var blade in handBlades)
            {
                await CardPileCmd.Add(blade, PileType.Exhaust);
            }
        }

        if (totalForge > 0)
        {
            await ForgeCmd.Forge(totalForge, owner, this);
        }
    }

    public override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    private static IEnumerable<SovereignBlade> GetSovereignBlades(Player player, bool includeExhausted)
    {
        var allCards = player.PlayerCombatState?.AllCards;
        if (allCards == null) return [];
        
        return allCards
            .Where(c => !c.IsDupe && c is SovereignBlade)
            .Where(c => includeExhausted || c.Pile?.Type != PileType.Exhaust)
            .Cast<SovereignBlade>();
    }
}
