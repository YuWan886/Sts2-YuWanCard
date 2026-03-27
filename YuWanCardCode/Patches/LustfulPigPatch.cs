using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Reflection;
using YuWanCard.Relics;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(AttackCommand))]
public class LustfulPigPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Execute")]
    public static bool ModifyAttackTarget(AttackCommand __instance, ref PlayerChoiceContext? choiceContext)
    {
        // 获取攻击者
        var attacker = __instance.Attacker;
        if (attacker == null || attacker.Monster == null)
        {
            return true;
        }

        // 检查是否有玩家拥有色欲猪遗物
        var combatState = attacker.CombatState;
        if (combatState == null)
        {
            return true;
        }

        // 检查攻击者是否是敌人
        if (!attacker.IsEnemy)
        {
            return true;
        }

        // 检查是否有玩家拥有色欲猪遗物
        var hasLustfulPig = combatState.Players.Any(p => p.GetRelic<LustfulPig>() != null);
        if (!hasLustfulPig)
        {
            return true;
        }

        // 40% 概率让敌人自己攻击自己
        var rng = combatState.RunState.Rng.CombatTargets;
        if (rng.NextFloat() <= 0.4f)
        {
            // 获取要攻击的玩家的名称
            string playerName = "Player";
            
            // 记录当前平台类型
            var platformType = RunManager.Instance.NetService.Platform;
            
            // 尝试获取当前攻击的目标玩家
            var singleTargetField = typeof(AttackCommand).GetField("_singleTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            if (singleTargetField != null)
            {
                if (singleTargetField.GetValue(__instance) is Creature target && target.IsPlayer && target.Player != null)
                {
                    playerName = PlatformUtil.GetPlayerName(platformType, target.Player.NetId);
                }
                else
                {
                }
            }
            
            // 如果没有找到目标玩家，使用第一个玩家的名称
            if (playerName == "Player" && combatState.Players.Count > 0)
            {
                var firstPlayer = combatState.Players[0];
                playerName = PlatformUtil.GetPlayerName(platformType, firstPlayer.NetId);
            }
            else if (playerName == "Player")
            {
            }
            
            // 如果玩家名称是数字，尝试获取玩家的角色名称
            if (int.TryParse(playerName, out _) && combatState.Players.Count > 0)
            {
                var firstPlayer = combatState.Players[0];
                if (firstPlayer.Character != null)
                {
                    // 获取角色名称
                    var characterName = firstPlayer.Character.Title?.GetFormattedText() ?? "Player";
                    if (!string.IsNullOrEmpty(characterName) && !int.TryParse(characterName, out _))
                    {
                        playerName = characterName;
                    }
                }
            }

            // 创建本地化对话框内容
            var dialogueLocString = new LocString("monsters", "LUSTFUL_PIG_FOR.player");
            dialogueLocString.Add("playerName", playerName);
            string dialogueText = dialogueLocString.GetFormattedText();
            
            // 显示对话框
            var speechBubble = NSpeechBubbleVfx.Create(dialogueText, attacker, 3.0);
            if (speechBubble != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(speechBubble);
            }
            
            // 使用反射修改 AttackCommand 的私有字段
            var combatStateField = typeof(AttackCommand).GetField("_combatState", BindingFlags.NonPublic | BindingFlags.Instance);
            var targetSideProperty = typeof(AttackCommand).GetProperty("TargetSide");
            var damagePropsField = typeof(AttackCommand).GetField("<DamageProps>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

            if (singleTargetField != null && combatStateField != null && targetSideProperty != null)
            {
#pragma warning disable CS8602
                // 设置为单一目标攻击，目标为攻击者自己
                singleTargetField.SetValue(__instance, attacker);
                targetSideProperty.SetValue(__instance, attacker.Side);

                // 添加 Unpowered 标记，防止 PersonalHivePower 等能力触发
                // 因为当敌人攻击自己时，dealer.Player 为 null，会导致空引用异常
                if (damagePropsField != null)
                {
                    var currentProps = (ValueProp)damagePropsField.GetValue(__instance)!;
                    damagePropsField.SetValue(__instance, currentProps | ValueProp.Unpowered);
                }
#pragma warning restore CS8602

                var monsterTitle = attacker.Monster.Title?.GetFormattedText() ?? attacker.Monster.GetType().Name;
                MainFile.Logger.Info($"LustfulPig: Enemy {monsterTitle} attacking itself for {playerName}");
            }
        }

        return true;
    }
}
