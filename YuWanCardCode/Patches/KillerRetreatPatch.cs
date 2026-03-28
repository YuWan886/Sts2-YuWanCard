using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
        var hasKiller = state.Enemies.Any(e => e.Monster is Killer);
        return hasKiller;
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
    // 位置设置为结束回合按钮正上方
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
    private HashSet<Player> _votedPlayers = [];

    public NRetreatButton(CombatState state)
    {
        _combatState = state;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        _viewport = GetViewport();
        
        Theme = null;
        FocusMode = FocusModeEnum.None;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        ZIndex = 100;

        // 创建 Visuals 容器
        _visuals = new Control
        {
            Name = "Visuals",
            Size = new Vector2(160, 55),
            Position = new Vector2(-80, -27.5f),
            Theme = null,
            ZIndex = 101
        };
        AddChild(_visuals);

        // 创建背景贴图
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
        // 加载按钮贴图
        var buttonTexture = PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/common_ui/event_button.png");
        if (buttonTexture != null)
        {
            _image.Texture = buttonTexture;
        }
        _visuals.AddChild(_image);

        // 创建标签
        _label = new MegaLabel
        {
            Name = "Label",
            Theme = null,
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
        _label.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
        _label.OffsetLeft = 0;
        _label.OffsetTop = 0;
        _label.OffsetRight = 0;
        _label.OffsetBottom = 0;
        _visuals.AddChild(_label);

        // 创建投票容器
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
        return _votedPlayers.Contains(player);
    }

    private Vector2 ShowPos => ShowPosRatio * _viewport.GetVisibleRect().Size;
    private Vector2 HidePos => HidePosRatio * _viewport.GetVisibleRect().Size;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            var rect = GetRect();
            var globalRect = new Rect2(GlobalPosition, rect.Size);
            bool inButton = globalRect.HasPoint(GetGlobalMousePosition());
            
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
            var rect = GetRect();
            var globalRect = new Rect2(GlobalPosition, rect.Size);
            bool inButton = globalRect.HasPoint(GetGlobalMousePosition());
            
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
        if (me != null)
        {
            // 确保每个玩家只能投票一次
            if (!_votedPlayers.Contains(me))
            {
                _votedPlayers.Add(me);
                _voteContainer.RefreshPlayerVotes();
                
                if (AllPlayersVoted())
                {
                    CallDeferred(nameof(ExecuteRetreat));
                }
                else
                {
                }
            }
            else
            {
            }
        }
    }

    private bool AllPlayersVoted()
    {
        if (_combatState == null) return false;
        
        foreach (var player in _combatState.Players)
        {
            if (!_votedPlayers.Contains(player))
            {
                return false;
            }
        }
        return true;
    }

    private async void ExecuteRetreat()
    {
        _isEnabled = false;
        Modulate = StsColors.gray;
        _label.Modulate = StsColors.gray;
        
        AnimOut();
        
        await Cmd.Wait(0.5f);

        if (CombatManager.Instance != null && CombatManager.Instance.IsInProgress && _combatState != null)
        {
            try
            {
                if (_combatState.Encounter is KillerElite killerElite)
                {
                    KillerElite.Retreated.Set(killerElite, true);
                }
                
                foreach (var enemy in _combatState.Enemies.ToList())
                {
                    if (enemy.IsAlive)
                    {
                        await CreatureCmd.Escape(enemy);
                    }
                }
                
                await CombatManager.Instance.CheckWinCondition();
            }
            catch (System.Exception e)
            {
                MainFile.Logger.Error($"KillerRetreatPatch: Error ending combat: {e.Message}");
            }
        }
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
        var retreatButton = __instance.GetNodeOrNull<NRetreatButton>("YuWanRetreatButton");
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
        var retreatButton = __instance.GetNodeOrNull<NRetreatButton>("YuWanRetreatButton");
        retreatButton?.Disable();
    }
}

[HarmonyPatch(typeof(NCombatUi), "AnimOut")]
public static class CombatUiAnimOutPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatUi __instance)
    {
        var retreatButton = __instance.GetNodeOrNull<NRetreatButton>("YuWanRetreatButton");
        retreatButton?.AnimOut();
    }
}
