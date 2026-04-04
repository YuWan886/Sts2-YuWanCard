using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using YuWanCard.Config;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(JoinFlow), "Begin")]
public static class JoinFlowHashPatch
{
    private static readonly Logger Logger = new Logger("YuWanCard.HashPatch", LogType.Generic);

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var hashProperty = AccessTools.PropertyGetter(typeof(ModelIdSerializationCache), "Hash");
        var bypassProperty = AccessTools.PropertyGetter(typeof(YuWanCardConfig), "BypassModelDbHashCheck");
        
        int hashCallIndex = -1;
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(hashProperty))
            {
                hashCallIndex = i;
                break;
            }
        }
        
        if (hashCallIndex == -1)
        {
            Logger.Warn("Could not find ModelIdSerializationCache.Hash call");
            return codes;
        }
        
        int comparisonStart = -1;
        for (int i = hashCallIndex - 1; i >= 0; i--)
        {
            var code = codes[i];
            if (code.opcode == OpCodes.Ldloc_S || code.opcode == OpCodes.Ldloc)
            {
                comparisonStart = i;
                break;
            }
        }
        
        if (comparisonStart == -1)
        {
            Logger.Warn("Could not find comparison start");
            return codes;
        }
        
        int branchIndex = -1;
        for (int i = hashCallIndex; i < Math.Min(hashCallIndex + 30, codes.Count); i++)
        {
            if (codes[i].opcode == OpCodes.Brtrue || codes[i].opcode == OpCodes.Brtrue_S)
            {
                branchIndex = i;
                break;
            }
        }
        
        if (branchIndex == -1)
        {
            Logger.Warn("Could not find branch instruction");
            return codes;
        }
        
        var branchTarget = codes[branchIndex].operand;
        
        var newCodes = new List<CodeInstruction>();
        
        for (int i = 0; i < codes.Count; i++)
        {
            if (i == comparisonStart)
            {
                newCodes.Add(new CodeInstruction(OpCodes.Call, bypassProperty));
                newCodes.Add(new CodeInstruction(OpCodes.Brtrue_S, branchTarget));
            }
            
            if (i >= comparisonStart && i <= branchIndex)
            {
                continue;
            }
            
            newCodes.Add(codes[i]);
        }
        
        Logger.Info("ModelDb hash check patch applied - will bypass when BypassModelDbHashCheck is true");
        
        return newCodes;
    }
}
