using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using YuWanCard.Utils;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class AngelPigPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner)
        {
            return true;
        }

        return Amount <= 0;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;
        if (Amount <= 0) return;

        Flash();
        Amount--;
        await CreatureCmd.Heal(Owner, Owner.MaxHp);

        if (Amount <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);

        CreatureVisualUtils.SwitchCreatureSkin(Owner, "normal");
        await CreatureCmd.TriggerAnim(Owner, "Tf2", 4.0f);
        await Task.Delay(TimeSpan.FromSeconds(4.2f));
        CreatureVisualUtils.SwitchCreatureSkin(Owner, "tianshi");
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        CreatureVisualUtils.SwitchCreatureSkin(oldOwner, "normal");
        await Task.CompletedTask;
    }
}
