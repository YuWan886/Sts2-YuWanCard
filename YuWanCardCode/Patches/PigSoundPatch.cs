using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Characters;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim")]
public static class PigHurtSoundPatch
{
    private static void TryPlayHurtSound(Creature creature)
    {
        if (creature == null) return;
        
        NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (nCreature == null) return;
        
        var visuals = nCreature.Visuals;
        if (visuals == null) return;
        
        bool shouldPlay = false;
        
        if (creature.IsPlayer && creature.Player?.Character is Pig)
        {
            shouldPlay = true;
        }
        else if (creature.IsMonster && creature.Monster is PigMinion)
        {
            shouldPlay = true;
        }
        
        if (!shouldPlay) return;
        
        visuals.TryExecuteOnNode<AudioStreamPlayer>("HurtSound", 
            sound => sound.Play());
    }

    [HarmonyPostfix]
    public static void Postfix(Creature creature, string triggerName, float waitTime)
    {
        if (triggerName == "Hit")
        {
            TryPlayHurtSound(creature);
        }
    }
}

[HarmonyPatch(typeof(SfxCmd), "PlayDeath", typeof(Player))]
public static class PigDeathSoundPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Player player)
    {
        if (player?.Character is not Pig) return true;
        
        var nCreature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (nCreature == null) return true;
        
        var visuals = nCreature.Visuals;
        if (visuals == null) return true;
        
        if (visuals.TryExecuteOnNode<AudioStreamPlayer>("DieSound", 
            sound => sound.Play()))
        {
            return false;
        }
        
        return true;
    }
}

[HarmonyPatch(typeof(SfxCmd), "PlayDeath", typeof(MonsterModel))]
public static class PigMinionDeathSoundPatch
{
    [HarmonyPrefix]
    public static bool Prefix(MonsterModel? monster)
    {
        if (monster is not PigMinion) return true;
        
        var creature = monster.Creature;
        if (creature == null) return true;
        
        var nCreature = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (nCreature == null) return true;
        
        var visuals = nCreature.Visuals;
        if (visuals == null) return true;
        
        if (visuals.TryExecuteOnNode<AudioStreamPlayer>("DieSound", 
            sound => sound.Play()))
        {
            return false;
        }
        
        return true;
    }
}
