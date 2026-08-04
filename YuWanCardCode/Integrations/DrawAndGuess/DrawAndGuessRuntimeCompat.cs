using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Characters;
using YuWanCard.Core.Interop;

namespace YuWanCard.DrawAndGuess;

/// <summary>
/// Runtime integration with the "你画瓦猜 / Draw &amp; Guess" mod (DrawAndGuessMod).
///
/// Currently injects the Pig character as a sixth drawing stamp so players can stamp
/// little pig icons onto their card artwork. The stamp texture is always the fixed
/// <c>character_icon_pig.png</c> and does NOT follow the selected Pig skin. All other
/// surfaces (card recognition candidates, relic appraisal, card-pool detection in the
/// settings screen) iterate every loaded card/relic pool automatically, so YuWanCard
/// content already participates without any code on our side.
///
/// The integration is a pure optional dependency: when DrawAndGuessMod is absent
/// every patch is a no-op and this mod behaves exactly as before.
/// </summary>
public static class DrawAndGuessRuntimeCompat
{
    private const string DrawAndGuessModId = "DrawAndGuessMod";
    private const string DrawingScreenTypeName = "DrawAndGuessMod.Scripts.Ui.DrawingScreen";
    private const string DrawingCanvasTypeName = "DrawAndGuessMod.Scripts.Ui.DrawingCanvas";
    private const string AddStampButtonMethodName = "AddStampButton";
    private const string RegisterStampMethodName = "RegisterStamp";
    private const string CanvasFieldName = "_canvas";

    // Fixed stamp artwork — intentionally not skin-aware.
    private const string PigStampTexturePath = "res://YuWanCard/images/characters/character_icon_pig.png";

    // DrawAndGuess registers five vanilla character stamps (Ironclad..Regent) with
    // indices 0-4. The Pig stamp takes index 5. DrawingCommand serializes StampIndex
    // over the network as 3 bits (0-7), so this fits without colliding; clients that
    // don't have YuWanCard simply have no registered stamp for index 5 and the stroke
    // is silently skipped, which degrades gracefully.
    private const byte LastVanillaStampIndex = 4;
    private const byte PigStampIndex = 5;

    private static bool _installed;
    private static MethodInfo? _addStampButtonMethod;
    private static MethodInfo? _registerStampMethod;
    private static FieldInfo? _canvasField;
    private static Texture2D? _fixedStampTexture;

    public static void TryInstall(Harmony harmony)
    {
        if (_installed)
        {
            return;
        }

        ModCompatContext? context = ModCompat.TryCreate(DrawAndGuessModId, "DrawAndGuessRuntimeCompat");
        if (context == null)
        {
            return;
        }

        Type? drawingScreenType = context.ResolveType(DrawingScreenTypeName);
        if (drawingScreenType == null)
        {
            MainFile.Logger.Warn("DrawAndGuessRuntimeCompat: DrawingScreen type not found");
            return;
        }

        _addStampButtonMethod = AccessTools.Method(
            drawingScreenType,
            AddStampButtonMethodName,
            [typeof(HBoxContainer), typeof(CharacterModel), typeof(byte)]);
        if (_addStampButtonMethod == null)
        {
            MainFile.Logger.Warn("DrawAndGuessRuntimeCompat: AddStampButton method not found");
            return;
        }

        // Resolve the private canvas field and public RegisterStamp so the fixed stamp
        // texture can be swapped in after the vanilla button is created.
        _canvasField = AccessTools.Field(drawingScreenType, CanvasFieldName);
        Type? drawingCanvasType = context.ResolveType(DrawingCanvasTypeName);
        _registerStampMethod = drawingCanvasType == null
            ? null
            : AccessTools.Method(drawingCanvasType, RegisterStampMethodName, [typeof(byte), typeof(Texture2D)]);

        _installed = true;
        context.PatchMethod(
            harmony,
            drawingScreenType,
            AddStampButtonMethodName,
            typeof(DrawAndGuessRuntimeCompat),
            postfixName: nameof(AddStampButtonPostfix));
        MainFile.Logger.Info("DrawAndGuessRuntimeCompat: DrawAndGuessMod detected, injecting Pig stamp");
    }

    public static void TryInstallIfAvailable()
    {
        TryInstall(new Harmony(MainFile.ModId));
    }

    /// <summary>
    /// Appends the Pig stamp to the character-stamp toolbar right after the last
    /// vanilla stamp. Reuses DrawAndGuess' own AddStampButton (resolved via
    /// reflection) so the button, tooltip, tool switching and size control all behave
    /// exactly like the vanilla stamps, then overrides the stamp texture and button
    /// icon with the fixed <c>character_icon_pig.png</c> so it never follows the
    /// selected Pig skin.
    /// </summary>
    public static void AddStampButtonPostfix(object __instance, HBoxContainer tools, byte stampIndex)
    {
        if (stampIndex != LastVanillaStampIndex || tools == null || _addStampButtonMethod == null)
        {
            return;
        }

        try
        {
            CharacterModel pig = ModelDb.Character<Pig>();
            _addStampButtonMethod.Invoke(__instance, [tools, pig, (byte)PigStampIndex]);

            Texture2D? fixedTexture = GetFixedStampTexture();
            if (fixedTexture == null)
            {
                return;
            }

            // Swap the canvas stamp image to the fixed texture (skin-independent).
            if (_canvasField != null && _registerStampMethod != null)
            {
                object? canvas = _canvasField.GetValue(__instance);
                if (canvas != null)
                {
                    _registerStampMethod.Invoke(canvas, [(byte)PigStampIndex, fixedTexture]);
                }
            }

            // Swap the button icon to the same fixed texture (the just-added button is
            // the last child of the stamp toolbar).
            if (tools.GetChildCount() > 0 && tools.GetChild(tools.GetChildCount() - 1) is Button button)
            {
                button.Icon = fixedTexture;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"DrawAndGuessRuntimeCompat: failed to add Pig stamp: {ex.Message}");
        }
    }

    private static Texture2D? GetFixedStampTexture()
    {
        if (_fixedStampTexture != null)
        {
            return _fixedStampTexture;
        }

        try
        {
            _fixedStampTexture = GD.Load<Texture2D>(PigStampTexturePath);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"DrawAndGuessRuntimeCompat: failed to load Pig stamp texture: {ex.Message}");
        }

        return _fixedStampTexture;
    }
}
