using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YuWanCard.Patches;

/// <summary>
/// 修复七咒之戒的休息处回复量显示问题
/// 修改 HealRestSiteOption.Description，使其显示实际的回复量（15%）
/// </summary>
[HarmonyPatch(typeof(HealRestSiteOption), "Description", MethodType.Getter)]
class HealRestSiteOptionPatch
{
    [HarmonyPostfix]
    static void FixHealDisplay(ref LocString __result, HealRestSiteOption __instance)
    {
        // 使用反射获取 Owner 属性
        var ownerProperty = typeof(RestSiteOption).GetProperty("Owner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (ownerProperty == null)
        {
            return;
        }

        if (ownerProperty.GetValue(__instance) is not MegaCrit.Sts2.Core.Entities.Players.Player player)
        {
            return;
        }

        // 检查玩家是否有七咒之戒
        var hasRingOfSevenCurses = false;
        foreach (var relic in player.Relics)
        {
            if (relic.Id.Entry == "YUWANCARD-RING_OF_SEVEN_CURSES")
            {
                hasRingOfSevenCurses = true;
                break;
            }
        }

        if (!hasRingOfSevenCurses)
        {
            return;
        }

        // 计算实际回复量（15%）
        decimal baseHeal = (decimal)player.Creature.MaxHp * 0.3m;
        decimal actualHeal = baseHeal * 0.5m;
        int actualHealInt = (int)actualHeal;

        // 重新创建 LocString，使用实际的回复量
        // 获取原始的本地化键
        var originalKey = __result.LocEntryKey;
        var originalTable = __result.LocTable;
        
        // 创建一个新的 LocString，使用相同的本地化键
        var newLocString = new LocString(originalTable, originalKey);
        
        // 复制原始的变量
        foreach (var var in __result.Variables)
        {
            if (var.Key == "Heal")
            {
                // 替换 Heal 变量为实际值
                newLocString.Add("Heal", actualHealInt.ToString());
            }
            else if (var.Key == "Character" || var.Key == "ExtraText")
            {
                // 直接复制字符串类型的变量
                newLocString.Add(var.Key, var.Value.ToString()!);
            }
        }
        
        // 如果没有找到 Heal 变量，手动添加
        if (!newLocString.Variables.ContainsKey("Heal"))
        {
            newLocString.Add("Heal", actualHealInt.ToString());
        }
        
        __result = newLocString;
    }
}



