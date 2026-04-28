using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigDemonFormPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StrengthGain", 2m)
    ];

    private int StrengthGain => DynamicVars["StrengthGain"].IntValue;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side) return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner, Amount * StrengthGain, Owner, null);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        SwitchCreatureSkin(oldOwner, "normal");

        var pigMinion = PetManager.FindPetByType<PigMinion>(oldOwner);
        if (pigMinion != null && pigMinion.IsAlive)
        {
            SwitchCreatureSkin(pigMinion, "normal");
        }

        await Task.CompletedTask;
    }

    public static void SwitchCreatureSkin(Creature creature, string skinName)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode?.Visuals == null) return;

        var spineNode = creatureNode.Visuals.GetNode("%Visuals");
        if (spineNode == null) return;

        var megaSprite = new MegaSprite(spineNode);
        var skeleton = megaSprite.GetSkeleton();
        if (skeleton == null) return;

        var data = skeleton.GetData();
        var skin = data.FindSkin(skinName);
        if (skin != null)
        {
            skeleton.SetSkin(skin);
            skeleton.SetSlotsToSetupPose();
        }
    }
}
