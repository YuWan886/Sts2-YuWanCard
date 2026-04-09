using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public partial class BloodWheelEye : YuWanCardModel
{
    private const string FramePathPrefix = "res://YuWanCard/images/vfx/blood_wheel_eye/blood_wheel_eye";
    private const float Fps = 16.0f;
    private const float TotalDuration = 3.0f;

    public BloodWheelEye() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PlayBloodWheelEyeVfx();
        
        var allCards = PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(c => c != this)
            .ToList();

        foreach (var card in allCards)
        {
            card.BaseReplayCount += 1;
        }
        
        MainFile.Logger.Info($"BloodWheelEye: Added Replay 1 to {allCards.Count} cards");
        
        return Task.CompletedTask;
    }

    private void PlayBloodWheelEyeVfx()
    {
        try
        {
            var vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
            if (vfxContainer == null)
            {
                MainFile.Logger.Warn("BloodWheelEye: CombatVfxContainer not found");
                return;
            }

            var frames = VfxUtils.GetCachedFrames(FramePathPrefix);
            if (frames == null || frames.Count == 0)
            {
                MainFile.Logger.Warn("BloodWheelEye: No frames loaded");
                return;
            }

            var effect = new BloodWheelEyeVfxContainer(frames);
            vfxContainer.AddChildSafely(effect);
            
            MainFile.Logger.Debug("BloodWheelEye: VFX spawned");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"BloodWheelEye: VFX error: {ex.Message}");
        }
    }

    private partial class BloodWheelEyeVfxContainer : ColorRect
    {
        private readonly IReadOnlyList<Texture2D> _frames;
        private Sprite2D? _sprite;
        private float _elapsedTime;
        private int _currentFrame;

        public BloodWheelEyeVfxContainer(IReadOnlyList<Texture2D> frames)
        {
            _frames = frames;
            
            MouseFilter = MouseFilterEnum.Ignore;
            AnchorsPreset = (int)LayoutPreset.FullRect;
            AnchorRight = 1.0f;
            AnchorBottom = 1.0f;
            Color = new Color(0.0f, 0.0f, 0.0f, 0.5f);
        }

        public override void _Ready()
        {
            _sprite = new Sprite2D
            {
                Centered = true,
                ZIndex = 100
            };
            AddChild(_sprite);

            var viewport = GetViewport();
            if (viewport == null)
            {
                MainFile.Logger.Warn("BloodWheelEyeVfxContainer: Viewport is null");
                QueueFree();
                return;
            }

            var viewportSize = viewport.GetVisibleRect().Size;
            _sprite.Position = viewportSize * 0.5f;

            if (_frames.Count > 0 && _frames[0] != null)
            {
                var firstFrame = _frames[0];
                var frameSize = firstFrame.GetSize();
                var scaleX = viewportSize.X / frameSize.X;
                var scaleY = viewportSize.Y / frameSize.Y;
                var spriteScale = Mathf.Max(scaleX, scaleY) * 0.8f;
                _sprite.Scale = new Vector2(spriteScale, spriteScale);
            }
        }

        public override void _Process(double delta)
        {
            _elapsedTime += (float)delta;

            if (_elapsedTime >= TotalDuration)
            {
                QueueFree();
                return;
            }

            UpdateFrame();
        }

        private void UpdateFrame()
        {
            if (_frames.Count == 0 || _sprite == null)
                return;

            var frameIndex = (int)(_elapsedTime * Fps) % _frames.Count;
            if (frameIndex != _currentFrame)
            {
                _currentFrame = frameIndex;
                _sprite.Texture = _frames[_currentFrame];
            }
        }
    }
}
