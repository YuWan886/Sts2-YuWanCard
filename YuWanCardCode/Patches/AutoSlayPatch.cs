using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Random;
using YuWanCard.Characters;
using YuWanCard.Config;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NGame))]
[HarmonyPatch(nameof(NGame.IsReleaseGame))]
public static class AutoSlayPatch
{
    public static void Postfix(ref bool __result)
    {
        if (YuWanCardConfig.EnableAutoSlay)
        {
            __result = false;
        }
    }
}

public static class AutoSlayCharacterPatch
{
    private static readonly MethodInfo SelectPigMethod = AccessTools.Method(
        typeof(AutoSlayCharacterPatch), 
        nameof(SelectPigCharacter));

    public static void ApplyPatch(Harmony harmony)
    {
        var autoSlayerType = typeof(AutoSlayer);
        
        var asyncMethod = AccessTools.Method(autoSlayerType, "PlayMainMenuAsync");
        if (asyncMethod == null)
        {
            MainFile.Logger.Warn("[AutoSlay] Could not find PlayMainMenuAsync method");
            return;
        }
        
        var stateMachineType = asyncMethod
            .GetCustomAttribute<AsyncStateMachineAttribute>()?
            .StateMachineType;
        
        if (stateMachineType == null)
        {
            MainFile.Logger.Warn("[AutoSlay] Could not find state machine type for PlayMainMenuAsync");
            return;
        }
        
        var moveNextMethod = AccessTools.Method(stateMachineType, "MoveNext");
        if (moveNextMethod == null)
        {
            MainFile.Logger.Warn("[AutoSlay] Could not find MoveNext method in state machine");
            return;
        }
        
        var transpilerMethod = AccessTools.Method(typeof(AutoSlayCharacterPatch), nameof(Transpiler));
        
        harmony.Patch(moveNextMethod, transpiler: new HarmonyMethod(transpilerMethod));
        MainFile.Logger.Info($"[AutoSlay] Applied transpiler to {stateMachineType.Name}.MoveNext");
    }
    
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var found = false;
        
        foreach (var instruction in instructions)
        {
            if (!found && 
                (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                instruction.operand is MethodInfo methodInfo &&
                methodInfo.Name == "NextItem" &&
                methodInfo.DeclaringType == typeof(Rng))
            {
                found = true;
                MainFile.Logger.Info("[AutoSlay] Found NextItem call, replacing with SelectPigCharacter");
                yield return new CodeInstruction(OpCodes.Call, SelectPigMethod);
            }
            else
            {
                yield return instruction;
            }
        }
        
        if (!found)
        {
            MainFile.Logger.Warn("[AutoSlay] Could not find NextItem call in MoveNext");
        }
    }

    private static NCharacterSelectButton? SelectPigCharacter(Rng rng, List<NCharacterSelectButton> items)
    {
        if (!YuWanCardConfig.EnableAutoSlay || !AutoSlayer.IsActive)
        {
            return rng.NextItem(items);
        }
        
        if (items == null || items.Count == 0)
        {
            return null;
        }
        
        foreach (var b in items)
        {
            var character = b.Character;
            if (character == null) continue;
            
            var entry = character.Id?.Entry;
            if (entry == null) continue;
            
            if (entry == "YUWANCARD-pig" || entry == "pig" || character is Pig)
            {
                MainFile.Logger.Info($"[AutoSlay] Auto-selecting Pig character from {items.Count} buttons");
                return b;
            }
        }
        
        MainFile.Logger.Warn($"[AutoSlay] Pig character not found in {items.Count} buttons, using random");
        return rng.NextItem(items);
    }
}
