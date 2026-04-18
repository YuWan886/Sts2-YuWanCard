using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.addons.mega_text;
using YuWanCard.Monsters;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Context;
using YuWanCard.Encounters;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.GameActions;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
public static class CombatUiActivatePatch
{
    private const string RetreatButtonName = "YuWanRetreatButton";

    [HarmonyPostfix]
    public static void Postfix(NCombatUi __instance, CombatState state)
    {
        
        if (!HasKillerEnemy(state))
        {
            return;
        }

        var existingButton = __instance.GetNodeOrNull<Control>(RetreatButtonName);
        if (existingButton != null)
        {
            return;
        }
        var retreatButton = CreateRetreatButton(state);
        __instance.AddChild(retreatButton);
    }

    private static bool HasKillerEnemy(CombatState state)
    {
        return state.Enemies.Any(e => e.Monster is Killer && e.IsAlive);
    }

    private static NRetreatButton CreateRetreatButton(CombatState state)
    {
        var button = new NRetreatButton(state)
        {
            Name = RetreatButtonName
        };
        return button;
    }
}

public partial class NRetreatButton : Control
{
    private const float FlyInOutDuration = 0.5f;
    private static readonly Vector2 ShowPosRatio = new Vector2(1634f, 786f) / NGame.devResolution;
    private static readonly Vector2 HidePosRatio = ShowPosRatio + new Vector2(0f, 400f) / NGame.devResolution;
    
    private CombatState? _combatState;
    private Control _visuals = null!;
    private TextureRect _image = null!;
    private MegaLabel _label = null!;
    private NMultiplayerVoteContainer _voteContainer = null!;
    
    private Viewport _viewport = null!;
    private Tween? _positionTween;
    private Tween? _hoverTween;
    
    private bool _isEnabled;
    private ulong? _localPlayerNetId;

    public NRetreatButton(CombatState state)
    {
        _combatState = state;
        _localPlayerNetId = LocalContext.NetId;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        _viewport = GetViewport();
        
        Theme = null;
        FocusMode = FocusModeEnum.None;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        ZIndex = 100;

        _visuals = new Control
        {
            Name = "Visuals",
            Size = new Vector2(160, 55),
            Position = new Vector2(-80, -27.5f),
            Theme = null,
            ZIndex = 101
        };
        AddChild(_visuals);

        _image = new TextureRect
        {
            Name = "Image",
            Size = new Vector2(160, 55),
            Position = Vector2.Zero,
            Scale = new Vector2(0.5f, 0.5f),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Theme = null,
            ZIndex = 102
        };
        var buttonTexture = PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/common_ui/event_button.png");
        if (buttonTexture != null)
        {
            _image.Texture = buttonTexture;
        }
        _visuals.AddChild(_image);

        _label = new MegaLabel
        {
            Name = "Label",
            ZIndex = 103,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = StsColors.cream,
            AutoSizeEnabled = true,
            MinFontSize = 30,
            MaxFontSize = 38,
            AutowrapMode = TextServer.AutowrapMode.Off
        };
        var font = PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_shared.tres");
        if (font != null)
        {
            _label.AddThemeFontOverride("font", font);
        }
        var buttonText = new LocString("encounters", "YUWANCARD-RETREAT_BUTTON.title");
        _label.SetTextAutoSize(buttonText.GetFormattedText());
        _label.Size = new Vector2(160, 55);
        _label.Position = Vector2.Zero;
        _label.AnchorsPreset = (int)LayoutPreset.FullRect;
        _label.OffsetLeft = 0;
        _label.OffsetTop = 0;
        _label.OffsetRight = 0;
        _label.OffsetBottom = 0;
        _visuals.AddChild(_label);

        _voteContainer = new NMultiplayerVoteContainer
        {
            Name = "VoteContainer",
            Theme = null,
            ZIndex = 104,
            Size = new Vector2(160, 30),
            Position = new Vector2(0, -40)
        };
        if (_combatState != null)
        {
            _voteContainer.Initialize(HasPlayerVoted, _combatState.Players);
        }
        AddChild(_voteContainer);
        
        CustomMinimumSize = new Vector2(160, 55);
        Size = new Vector2(160, 55);
        
        Position = HidePos;
        
        _isEnabled = false;
        _image.Modulate = StsColors.gray;
        _label.Modulate = StsColors.gray;
        
        CallDeferred(nameof(AnimIn));
    }

    private bool HasPlayerVoted(Player player)
    {
        if (_combatState?.Encounter is not KillerElite killerElite)
            return false;
        return killerElite.HasPlayerVoted(player.NetId);
    }

    private Vector2 ShowPos => ShowPosRatio * _viewport.GetVisibleRect().Size;
    private Vector2 HidePos => HidePosRatio * _viewport.GetVisibleRect().Size;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            var visualGlobalRect = GetVisualGlobalRect();
            bool inButton = visualGlobalRect.HasPoint(GetGlobalMousePosition());
            
            if (inButton && _isEnabled)
            {
                OnHoverEnter();
            }
            else
            {
                OnHoverExit();
            }
        }
        
        if (!CanTurnBeEnded) return;
        
        if (@event is InputEventMouseButton mouseButton)
        {
            var visualGlobalRect = GetVisualGlobalRect();
            bool inButton = visualGlobalRect.HasPoint(GetGlobalMousePosition());
            
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed && inButton && _isEnabled)
                {
                    OnPress();
                }
                else if (!mouseButton.Pressed && inButton && _isEnabled)
                {
                    OnRelease();
                }
                else
                {
                    OnHoverExit();
                }
            }
        }
    }

    private Rect2 GetVisualGlobalRect()
    {
        return new Rect2(
            GlobalPosition + _visuals.Position,
            _visuals.Size
        );
    }

    private void OnHoverEnter()
    {
        if (!_isEnabled) return;
        
        _hoverTween?.Kill();
        _hoverTween = CreateTween().SetParallel();
        _hoverTween.TweenProperty(_visuals, "position", new Vector2(0f, -2f), 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
    }

    private void OnHoverExit()
    {
        _hoverTween?.Kill();
        _hoverTween = CreateTween().SetParallel();
        _hoverTween.TweenProperty(_visuals, "position", Vector2.Zero, 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
    }

    private void OnPress()
    {
        if (!_isEnabled) return;
        
        _hoverTween?.Kill();
        _hoverTween = CreateTween().SetParallel();
        _hoverTween.TweenProperty(_visuals, "position", new Vector2(0f, 4f), 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
    }
    
    private bool CanTurnBeEnded
    {
        get
        {
            if (NCombatRoom.Instance == null) return false;
            return !NCombatRoom.Instance.Ui.Hand.InCardPlay && 
                   NCombatRoom.Instance.Ui.Hand.CurrentMode == NPlayerHand.Mode.Play;
        }
    }

    private void OnRelease()
    {
        if (_combatState == null) return;
        
        var me = LocalContext.GetMe(_combatState);
        if (me == null) return;
        
        if (_combatState.Encounter is not KillerElite killerElite) return;
        
        if (killerElite.HasPlayerVoted(me.NetId)) return;
        
        RequestRetreatVote(me);
    }

    private void RequestRetreatVote(Player player)
    {
        var action = new RetreatVoteAction(player);
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(action);
    }

    public void RefreshVotes()
    {
        _voteContainer.RefreshPlayerVotes();
    }

    private void AnimIn()
    {
        _positionTween?.Kill();
        _positionTween = CreateTween();
        _positionTween.TweenProperty(this, "position", ShowPos, FlyInOutDuration)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
    }

    public void AnimOut()
    {
        _positionTween?.Kill();
        _positionTween = CreateTween();
        _positionTween.TweenProperty(this, "position", HidePos, FlyInOutDuration)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
    }

    public void Enable()
    {
        _isEnabled = true;
        _image.Modulate = Colors.White;
        _label.Modulate = StsColors.cream;
    }

    public void Disable()
    {
        _isEnabled = false;
        _image.Modulate = StsColors.gray;
        _label.Modulate = StsColors.gray;
    }
}

[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Enable))]
public static class CombatUiEnablePatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatUi __instance)
    {
        var retreatButton = __instance.GetNodeSafe<NRetreatButton>("YuWanRetreatButton", logWarning: false);
        if (retreatButton == null) return;

        var combatState = CombatManager.Instance.DebugOnlyGetState();
        if (combatState == null) return;

        if (combatState.CurrentSide == CombatSide.Player && HasKillerEnemy(combatState))
        {
            retreatButton.Enable();
        }
        else
        {
            retreatButton.Disable();
        }
    }

    private static bool HasKillerEnemy(CombatState state)
    {
        return state.Enemies.Any(e => e.Monster is Killer && e.IsAlive);
    }
}

[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Disable))]
public static class CombatUiDisablePatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatUi __instance)
    {
        var retreatButton = __instance.GetNodeSafe<NRetreatButton>("YuWanRetreatButton", logWarning: false);
        retreatButton?.Disable();
    }
}

[HarmonyPatch(typeof(NCombatUi), "AnimOut")]
public static class CombatUiAnimOutPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatUi __instance)
    {
        var retreatButton = __instance.GetNodeSafe<NRetreatButton>("YuWanRetreatButton", logWarning: false);
        retreatButton?.AnimOut();
    }
}
