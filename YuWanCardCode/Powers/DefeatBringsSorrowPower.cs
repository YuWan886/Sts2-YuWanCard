using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Cards;
using YuWanCard.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class DefeatBringsSorrowPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://YuWanCard/images/powers/sad_army_win_power.png";
    public override string? CustomBigIconPath => CustomPackedIconPath;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("IntentDamageThreshold", 0m)];

    [SavedProperty]
    public int YUWANCARD_IntentDamageThreshold
    {
        get => DynamicVars["IntentDamageThreshold"].IntValue;
        set => DynamicVars["IntentDamageThreshold"].BaseValue = value;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (cardSource is DefeatBringsSorrow && CombatState != null)
        {
            YUWANCARD_IntentDamageThreshold = IntentUtils.GetEnemyAttackIntentDamageTotal(CombatState);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side || CombatState == null)
        {
            return;
        }

        int damageDealtThisTurn = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Where(entry => entry.HappenedThisTurn(CombatState) && entry.Dealer == Owner)
            .Sum(entry => entry.Result.UnblockedDamage);

        Flash();

        if (damageDealtThisTurn > YUWANCARD_IntentDamageThreshold)
        {
            foreach (var enemy in CombatState.Enemies.Where(enemy => enemy.IsAlive && enemy.Monster != null))
            {
                await CreatureCmd.Stun(enemy);
            }
        }
        else
        {
            await PowerCmd.Apply<DefeatBringsSorrowStunPower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
        }

        await PowerCmd.Remove(this);
    }
}
