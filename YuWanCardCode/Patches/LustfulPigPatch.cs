using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(AttackCommand))]
public class LustfulPigPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Execute")]
    public static bool ModifyAttackTarget(AttackCommand __instance, ref PlayerChoiceContext? choiceContext)
    {
        var attacker = __instance.Attacker;
        if (attacker == null || attacker.Monster == null)
        {
            return true;
        }

        var combatState = attacker.CombatState;
        if (combatState == null)
        {
            return true;
        }

        if (!attacker.IsEnemy)
        {
            return true;
        }

        var hasLustfulPig = combatState.Players.Any(p => p.GetRelic<LustfulPig>() != null);
        if (!hasLustfulPig)
        {
            return true;
        }

        var rng = combatState.RunState.Rng.CombatTargets;
        if (rng.NextFloat() <= 0.4f)
        {
            string playerName = "Player";
            
            var platformType = RunManager.Instance.NetService.Platform;
            
            var target = YuWanReflectionHelper.GetPrivateField<Creature>(__instance, "_singleTarget");
            if (target != null && target.IsPlayer && target.Player != null)
            {
                playerName = PlatformUtil.GetPlayerName(platformType, target.Player.NetId);
            }
            
            if (playerName == "Player" && combatState.Players.Count > 0)
            {
                var firstPlayer = combatState.Players[0];
                playerName = PlatformUtil.GetPlayerName(platformType, firstPlayer.NetId);
            }

            var dialogueLocString = new LocString("monsters", "LUSTFUL_PIG_FOR.player");
            dialogueLocString.Add("playerName", playerName);
            string dialogueText = dialogueLocString.GetFormattedText();
            
            var speechBubble = NSpeechBubbleVfx.Create(dialogueText, attacker, 3.0);
            if (speechBubble != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(speechBubble);
            }
            
            var targetSideProperty = YuWanReflectionHelper.GetPrivateProperty(typeof(AttackCommand), "TargetSide");
            var damagePropsField = YuWanReflectionHelper.GetPrivateField(typeof(AttackCommand), "<DamageProps>k__BackingField");

            if (targetSideProperty != null && damagePropsField != null)
            {
#pragma warning disable CS8602
                YuWanReflectionHelper.SetPrivateField(__instance, "_singleTarget", attacker);
                targetSideProperty.SetValue(__instance, attacker.Side);

                var currentProps = (ValueProp)damagePropsField.GetValue(__instance)!;
                damagePropsField.SetValue(__instance, currentProps | ValueProp.Unpowered);
#pragma warning restore CS8602

                var monsterTitle = attacker.Monster.Title?.GetFormattedText() ?? attacker.Monster.GetType().Name;
                MainFile.Logger.Info($"LustfulPig: Enemy {monsterTitle} attacking itself for {playerName}");
            }
        }

        return true;
    }
}
