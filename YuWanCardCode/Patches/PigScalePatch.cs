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

[HarmonyPatch]
public class PigScalePatch
{
    private static readonly Dictionary<uint, int> _initialMaxHpMap = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Hook), "AfterPlayerTurnStart", typeof(CombatState), typeof(PlayerChoiceContext), typeof(Player))]
    static void OnPlayerTurnStart(CombatState combatState, PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Character is Pig && player.Creature != null && NCombatRoom.Instance != null)
        {
            UpdateScale(player.Creature);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Hook), "BeforeCombatStart", typeof(IRunState), typeof(CombatState))]
    static void OnCombatStart(IRunState runState, CombatState? combatState)
    {
        if (runState is RunState run && run.Players != null)
        {
            foreach (var player in run.Players)
            {
                if (player.Character is Pig pig && player.Creature != null && player.Creature.CombatId.HasValue)
                {
                    _initialMaxHpMap[player.Creature.CombatId.Value] = pig.StartingHp;
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Hook), "AfterCombatEnd", typeof(IRunState), typeof(CombatState), typeof(CombatRoom))]
    static void OnCombatEnd(IRunState runState, CombatState combatState, CombatRoom room)
    {
        if (runState is RunState run && run.Players != null)
        {
            foreach (var player in run.Players)
            {
                if (player.Character is Pig && player.Creature != null && player.Creature.CombatId.HasValue)
                {
                    _initialMaxHpMap.Remove(player.Creature.CombatId.Value);
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CreatureCmd), "SetCurrentHp", typeof(Creature), typeof(decimal))]
    static void OnHpChanged(Creature creature, decimal amount)
    {
        if (creature.Player != null && creature.Player.Character is Pig && NCombatRoom.Instance != null)
        {
            UpdateScale(creature);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Creature), "SetCurrentHpInternal", typeof(decimal))]
    static void OnHpChangedInternal(Creature __instance)
    {
        if (__instance.Player != null && __instance.Player.Character is Pig && NCombatRoom.Instance != null)
        {
            UpdateScale(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Creature), "LoseHpInternal", typeof(decimal), typeof(ValueProp))]
    static void OnLoseHpInternal(Creature __instance)
    {
        if (__instance.Player != null && __instance.Player.Character is Pig && NCombatRoom.Instance != null)
        {
            UpdateScale(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CreatureCmd), "GainMaxHp", typeof(Creature), typeof(decimal))]
    static void OnMaxHpChanged(Creature creature, decimal amount)
    {
        if (creature.Player != null && creature.Player.Character is Pig && NCombatRoom.Instance != null)
        {
            UpdateScale(creature);
        }
    }

    private static void UpdateScale(Creature creature)
    {
        if (creature == null || NCombatRoom.Instance == null || creature.Player == null || !creature.CombatId.HasValue)
            return;

        if (!_initialMaxHpMap.TryGetValue(creature.CombatId.Value, out int initialMaxHp))
        {
            initialMaxHp = ((Pig)creature.Player.Character).StartingHp;
            _initialMaxHpMap[creature.CombatId.Value] = initialMaxHp;
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
