using MegaCrit.Sts2.Core.Commands;
using YuWanCard.Powers;

namespace YuWanCard.Hextech.Relics;

public sealed class PigletCollarForge : HextechPigForgeBase
{
    public override HextechForgeRarity HextechRarity => HextechForgeRarity.Silver;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null)
        {
            return;
        }

        await PowerCmd.Apply<PigFriendsPower>(Owner.Creature, 1, Owner.Creature, null);
    }
}
