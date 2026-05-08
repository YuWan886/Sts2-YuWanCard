using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Characters;

namespace YuWanCard.Patches;

file static class PigScaleShared
{
    public static readonly Dictionary<uint, int> InitialMaxHpMap = new();

    public static void UpdateScale(Creature creature)
    {
        if (creature == null || NCombatRoom.Instance == null || creature.Player == null || !creature.CombatId.HasValue)
            return;

        if (!InitialMaxHpMap.TryGetValue(creature.CombatId.Value, out int initialMaxHp))
        {
            initialMaxHp = ((Pig)creature.Player.Character).StartingHp;
            InitialMaxHpMap[creature.CombatId.Value] = initialMaxHp;
        }

        float hpPercent = (float)creature.CurrentHp / initialMaxHp;
        float targetScale = Mathf.Max(0.3f, hpPercent);

        var creatureNode = NCombatRoom.Instance.GetCreatureNode(creature);
        if (creatureNode != null)
        {
            creatureNode.SetDefaultScaleTo(targetScale, 0.1f);
        }
        else
        {
            GD.PrintErr($"[PigScale] WARNING: creatureNode is null!");
        }
    }
}

[HarmonyPatch(typeof(Hook), "AfterPlayerTurnStart", typeof(ICombatState), typeof(PlayerChoiceContext), typeof(Player))]
public static class PigScaleAfterPlayerTurnStartPatch
{
    [HarmonyPostfix]
    static void Postfix(ICombatState combatState, PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Character is Pig && player.Creature != null && NCombatRoom.Instance != null)
        {
            PigScaleShared.UpdateScale(player.Creature);
        }
    }
}

[HarmonyPatch(typeof(Hook), "BeforeCombatStart", typeof(IRunState), typeof(ICombatState))]
public static class PigScaleBeforeCombatStartPatch
{
    [HarmonyPostfix]
    static void Postfix(IRunState runState, ICombatState? combatState)
    {
        if (runState is RunState run && run.Players != null)
        {
            foreach (var player in run.Players)
            {
                if (player.Character is Pig pig && player.Creature != null && player.Creature.CombatId.HasValue)
                {
                    PigScaleShared.InitialMaxHpMap[player.Creature.CombatId.Value] = pig.StartingHp;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(Hook), "AfterCombatEnd", typeof(IRunState), typeof(ICombatState), typeof(CombatRoom))]
public static class PigScaleAfterCombatEndPatch
{
    [HarmonyPostfix]
    static void Postfix(IRunState runState, ICombatState combatState, CombatRoom room)
    {
        if (runState is RunState run && run.Players != null)
        {
            foreach (var player in run.Players)
            {
                if (player.Character is Pig && player.Creature != null && player.Creature.CombatId.HasValue)
                {
                    PigScaleShared.InitialMaxHpMap.Remove(player.Creature.CombatId.Value);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(CreatureCmd), "SetCurrentHp", typeof(Creature), typeof(decimal))]
public static class PigScaleSetCurrentHpPatch
{
    [HarmonyPostfix]
    static void Postfix(Creature creature, decimal amount)
    {
        if (creature.Player != null && creature.Player.Character is Pig && NCombatRoom.Instance != null)
        {
            PigScaleShared.UpdateScale(creature);
        }
    }
}

[HarmonyPatch(typeof(Creature), "SetCurrentHpInternal", typeof(decimal))]
public static class PigScaleSetCurrentHpInternalPatch
{
    [HarmonyPostfix]
    static void Postfix(Creature __instance)
    {
        if (__instance.Player != null && __instance.Player.Character is Pig && NCombatRoom.Instance != null)
        {
            PigScaleShared.UpdateScale(__instance);
        }
    }
}

[HarmonyPatch(typeof(Creature), "set_CurrentHp")]
public static class PigScaleSetCurrentHpPropertyPatch
{
    [HarmonyPostfix]
    static void Postfix(Creature __instance)
    {
        if (__instance.Player != null && __instance.Player.Character is Pig && NCombatRoom.Instance != null)
        {
            PigScaleShared.UpdateScale(__instance);
        }
    }
}

[HarmonyPatch(typeof(CreatureCmd), "GainMaxHp", typeof(Creature), typeof(decimal))]
public static class PigScaleGainMaxHpPatch
{
    [HarmonyPostfix]
    static void Postfix(Creature creature, decimal amount)
    {
        if (creature.Player != null && creature.Player.Character is Pig && NCombatRoom.Instance != null)
        {
            PigScaleShared.UpdateScale(creature);
        }
    }
}
