using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Powers;

namespace YuWanCard.Hextech.Relics;

public sealed class PigletCollarForge : HextechPigForgeBase
{
    public override HextechForgeRarity HextechRarity => HextechForgeRarity.Silver;

    public override async Task BeforeCombatStart()
    {
        if (Owner == null)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<PigFriendsPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, Stacked(1), Owner.Creature, null);
    }
}
