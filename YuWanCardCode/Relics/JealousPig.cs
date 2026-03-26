using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class JealousPig : YuWanRelicModel
{
    [SavedProperty]
    private bool HasTriggeredThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public JealousPig() : base(true)
    {
    }

    public override Task BeforeCombatStart()
    {
        HasTriggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (HasTriggeredThisCombat)
        {
            return;
        }
        if (Owner == null || Owner.Creature == null)
        {
            return;
        }
        if (power.Owner == null)
        {
            return;
        }
        if (power.Owner.Side == Owner.Creature.Side)
        {
            return;
        }
        if (amount <= 0m)
        {
            return;
        }
        if (!power.IsVisible)
        {
            return;
        }
        if (power.Type != PowerType.Buff)
        {
            return;
        }

        // 检查是否是安全的能力（避免复制怪物专属能力）
        if (!IsSafePower(power))
        {
            return;
        }

        HasTriggeredThisCombat = true;
        Flash();
        MainFile.Logger.Info($"JealousPig: Copying power {power.Id} from enemy");

        var powerCanonical = ModelDb.GetById<PowerModel>(power.Id);
        if (powerCanonical != null)
        {
            await PowerCmd.Apply(powerCanonical.ToMutable(), Owner.Creature, amount, Owner.Creature, null);
        }
    }

    private static bool IsSafePower(PowerModel power)
    {
        // 检查是否是怪物专属能力
        var powerType = power.GetType();
        
        // 检查能力名称是否包含特定关键词（怪物专属能力）
        string powerFullName = powerType.FullName ?? "";
        if (powerFullName.Contains("Monsters"))
        {
            MainFile.Logger.Debug($"JealousPig: Skipping monster power {powerFullName}");
            return false;
        }

        // 检查能力 ID 是否包含特定关键词
        string powerId = power.Id.ToString();
        if (powerId.Contains("HIGH_VOLTAGE"))
        {
            MainFile.Logger.Debug($"JealousPig: Skipping HighVoltagePower");
            return false;
        }

        try
        {
            // 检查能力是否会导致战斗无法结束
            if (power.ShouldStopCombatFromEnding())
            {
                return false;
            }
        }
        catch (NullReferenceException)
        {
            return false;
        }

        return true;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        HasTriggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == Owner ? count + 1 : count;
}
