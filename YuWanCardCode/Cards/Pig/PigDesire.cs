using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Characters;
using YuWanCard.Utils;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigDesire : YuWanCardModel
{
    private static readonly HashSet<Type> s_powerBlacklist = new()
    {
        typeof(NightmarePower),
        typeof(YouArePigPower),
    };

    private static bool IsBlacklisted(PowerModel power)
    {
        var powerType = power.GetType();
        return s_powerBlacklist.Any(t => t.IsAssignableFrom(powerType));
    }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public PigDesire() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.AnyAlly)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var teammate = cardPlay.Target;
        if (teammate.Player == null || teammate.Player == Owner) return;

        var buffPowers = teammate.Powers
            .Where(p => p.IsVisible && p.Type == PowerType.Buff && PowerSafetyUtils.IsSafePower(p) && !IsBlacklisted(p))
            .ToList();

        foreach (var power in buffPowers)
        {
            var canonical = ModelDb.GetById<PowerModel>(power.Id);
            if (canonical != null)
            {
                await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), canonical.ToMutable(), Owner.Creature, power.Amount, Owner.Creature, null);
            }
        }
    }
}
