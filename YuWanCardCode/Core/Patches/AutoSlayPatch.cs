using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Random;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Makes IsReleaseGame return false when --autoslay is present,
/// enabling the autoslay code path in release builds.
/// </summary>
[HarmonyPatch(typeof(NGame))]
[HarmonyPatch(nameof(NGame.IsReleaseGame))]
public static class AutoSlayReleasePatch
{
    public static void Postfix(ref bool __result)
    {
        if (CommandLineHelper.HasArg("autoslay"))
        {
            __result = false;
        }
    }
}

/// <summary>
/// Overrides the ascension getter to return the value from --autoslay-ascension.
/// This bypasses save-data caps that would otherwise limit custom characters to A0.
/// </summary>
[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.Ascension), MethodType.Getter)]
public static class AutoSlayAscensionPatch
{
    public static void Postfix(ref int __result)
    {
        if (!AutoSlayer.IsActive)
            return;

        var ascensionArg = CommandLineHelper.GetValue("autoslay-ascension");
        if (ascensionArg == null)
            return;

        if (int.TryParse(ascensionArg, out int level) && level >= 0 && level <= 20)
        {
            __result = level;
        }
    }
}

/// <summary>
/// Transpiler that replaces the random character selection in PlayMainMenuAsync
/// with a character chosen via --autoslay-character command line arg.
/// Falls back to random if the arg is missing or the character isn't found.
/// </summary>
public static class AutoSlayCharacterPatch
{
    private static readonly MethodInfo SelectAutoSlayCharacterMethod = AccessTools.Method(
        typeof(AutoSlayCharacterPatch),
        nameof(SelectAutoSlayCharacter));

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

        try
        {
            harmony.Patch(moveNextMethod, transpiler: new HarmonyMethod(transpilerMethod));
            MainFile.Logger.Debug($"[AutoSlay] Applied character select transpiler to {stateMachineType.Name}.MoveNext");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[AutoSlay] Failed to apply character select transpiler (may be mobile): {ex.Message}");
        }
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
                MainFile.Logger.Debug("[AutoSlay] Found NextItem call, replacing with SelectAutoSlayCharacter");
                yield return new CodeInstruction(OpCodes.Call, SelectAutoSlayCharacterMethod);
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

    private static NCharacterSelectButton? SelectAutoSlayCharacter(Rng rng, List<NCharacterSelectButton> items)
    {
        if (!AutoSlayer.IsActive || items == null || items.Count == 0)
            return items != null ? rng.NextItem(items) : null;

        var characterArg = CommandLineHelper.GetValue("autoslay-character");
        if (!string.IsNullOrEmpty(characterArg))
        {
            foreach (var b in items)
            {
                var entry = b.Character?.Id?.Entry;
                if (entry != null && entry.Equals(characterArg, StringComparison.OrdinalIgnoreCase))
                {
                    MainFile.Logger.Debug($"[AutoSlay] Auto-selecting character '{entry}' from {items.Count} buttons");
                    return b;
                }
            }

            MainFile.Logger.Warn($"[AutoSlay] Character '{characterArg}' not found in {items.Count} buttons, falling back to random");
        }

        return rng.NextItem(items);
    }
}

/// <summary>
/// Transpiler that replaces the "Options" node path with "PauseButton"
/// in AbandonRunAsync, for mobile compatibility.
/// </summary>
public static class AutoSlayOptionsPatch
{
    private const string OldPath = "/root/Game/RootSceneContainer/Run/GlobalUi/TopBar/RightAlignedStuff/Options";
    private const string NewPath = "/root/Game/RootSceneContainer/Run/GlobalUi/TopBar/RightAlignedStuff/PauseButton";

    public static void ApplyPatch(Harmony harmony)
    {
        var autoSlayerType = typeof(AutoSlayer);

        var asyncMethod = AccessTools.Method(autoSlayerType, "AbandonRunAsync");
        if (asyncMethod == null)
        {
            MainFile.Logger.Warn("[AutoSlay] Could not find AbandonRunAsync method");
            return;
        }

        var stateMachineType = asyncMethod
            .GetCustomAttribute<AsyncStateMachineAttribute>()?
            .StateMachineType;

        if (stateMachineType == null)
        {
            MainFile.Logger.Warn("[AutoSlay] Could not find state machine type for AbandonRunAsync");
            return;
        }

        var moveNextMethod = AccessTools.Method(stateMachineType, "MoveNext");
        if (moveNextMethod == null)
        {
            MainFile.Logger.Warn("[AutoSlay] Could not find MoveNext method in AbandonRunAsync state machine");
            return;
        }

        var transpilerMethod = AccessTools.Method(typeof(AutoSlayOptionsPatch), nameof(Transpiler));

        try
        {
            harmony.Patch(moveNextMethod, transpiler: new HarmonyMethod(transpilerMethod));
            MainFile.Logger.Debug($"[AutoSlay] Applied Options->PauseButton transpiler to {stateMachineType.Name}.MoveNext");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[AutoSlay] Failed to apply Options->PauseButton transpiler (may be mobile): {ex.Message}");
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var found = false;

        foreach (var instruction in instructions)
        {
            if (!found &&
                instruction.opcode == OpCodes.Ldstr &&
                instruction.operand is string str &&
                str == OldPath)
            {
                found = true;
                MainFile.Logger.Debug("[AutoSlay] Found Options path, replacing with PauseButton");
                yield return new CodeInstruction(OpCodes.Ldstr, NewPath);
            }
            else
            {
                yield return instruction;
            }
        }

        // 0.109+ already uses PauseButton, so no replacement is necessary.
    }
}
