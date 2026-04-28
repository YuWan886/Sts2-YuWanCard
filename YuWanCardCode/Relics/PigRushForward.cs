using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class PigRushForward : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    private bool _hasTriggeredThisCombat;

    public PigRushForward() : base(true)
    {
    }

    public override Task BeforeCombatStart()
    {
        _hasTriggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (_hasTriggeredThisCombat) return;
        if (dealer != Owner?.Creature) return;
        if (target == null || target.Side != CombatSide.Enemy) return;
        if (cardSource == null) return;
        if (result.TotalDamage <= 0) return;

        _hasTriggeredThisCombat = true;
        Flash();

        decimal maxHpLoss = result.TotalDamage;
        await CreatureCmd.LoseMaxHp(choiceContext, target, maxHpLoss, isFromCard: false);

        MainFile.Logger.Info($"PigRushForward triggered: Reduced {target.Name} max HP by {maxHpLoss}");
    }
}
