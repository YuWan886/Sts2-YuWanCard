using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Balatro;
using YuWanCard.Core.Extensions;
using TooltipSource = YuWanCard.Core.Utils.TooltipSource;

namespace YuWanCard.Core.Abstracts;

public abstract partial class YuWanCardModel : CardModel, IYuWanContent
{
    private static readonly Regex CamelCaseRegex = MyRegex();
    private static readonly string DefaultPortraitPath = "res://YuWanCard/images/card_portraits/you_are_pig.png";

    private readonly List<CardKeyword> _cardKeywords = [];
    private readonly List<(CardKeyword, UpgradeType)> _upgradeKeywords = [];
    private readonly List<DynamicVar> _constructedDynamicVars = [];
    private readonly List<Func<CardModel, IHoverTip>> _hoverTips = [];
    private readonly List<Func<CardModel, IEnumerable<IHoverTip>>> _multiHoverTips = [];
    private readonly HashSet<CardTag> _constructedTags = [];
    private CardHandGlowRules _constructedHandGlowRules;
    private CardPoolModel? _resolvedPool;

    protected enum UpgradeType
    {
        None,
        Add,
        Remove
    }

    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    
    protected string ModResPath => AssetPathHelper.GetModResPathFromType(GetType());
    
    protected virtual string PortraitBasePath => $"{ModResPath}/images/card_portraits/{CardId}";
    protected virtual string FrameBasePath => $"{ModResPath}/images/card_frames/{CardId}";
    protected virtual string AncientFramePath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/beta.tres");
    protected virtual string AncientBannerTexturePath => ImageHelper.GetImagePath("atlases/ui_atlas.sprites/card/card_banner_ancient_s.tres");
    protected virtual string AncientBannerMaterialPath => "res://materials/cards/banners/card_banner_ancient_mat.tres";
    protected virtual string AncientVisualTextBgPath
    {
        get
        {
            var cardType = Type switch
            {
                CardType.None or CardType.Status or CardType.Curse => CardType.Skill,
                CardType.Attack or CardType.Skill or CardType.Power or CardType.Quest => Type,
                _ => throw new ArgumentOutOfRangeException()
            };

            return ImageHelper.GetImagePath(
                "atlases/compressed.sprites/card_template/ancient_card_text_bg_" +
                cardType.ToString().ToLowerInvariant() +
                ".tres");
        }
    }

    public override string PortraitPath => GetPortraitPath();
    public virtual string? CustomPortraitPath => null;
    public virtual bool UseAncientVisualStyle => false;

    private string GetPortraitPath()
    {
        if (CustomPortraitPath != null)
            return CustomPortraitPath;
        
        string portraitPath = $"{PortraitBasePath}.png";
        return ResourceLoader.Exists(portraitPath) ? portraitPath : DefaultPortraitPath;
    }

    public virtual Texture2D? CustomFrame
    {
        get
        {
            if (UseAncientVisualStyle)
                return ResourceLoader.Load<Texture2D>(AncientFramePath, null, ResourceLoader.CacheMode.Reuse);

            string framePath = $"{FrameBasePath}.png";
            if (ResourceLoader.Exists(framePath))
                return ResourceLoader.Load<Texture2D>(framePath);
            return null;
        }
    }

    public virtual Texture2D? CustomAncientTextBg =>
        UseAncientVisualStyle
            ? ResourceLoader.Load<Texture2D>(AncientVisualTextBgPath, null, ResourceLoader.CacheMode.Reuse)
            : null;

    public virtual Texture2D? CustomBannerTexture =>
        UseAncientVisualStyle
            ? ResourceLoader.Load<Texture2D>(AncientBannerTexturePath, null, ResourceLoader.CacheMode.Reuse)
            : null;

    public virtual Material? CustomBannerMaterial =>
        UseAncientVisualStyle
            ? PreloadManager.Cache.GetMaterial(AncientBannerMaterialPath)
            : null;

    public override CardPoolModel Pool
    {
        get
        {
            if (_resolvedPool != null)
            {
                return _resolvedPool;
            }

            Type? poolType = GetType().GetCustomAttribute<PoolAttribute>()?.PoolType;
            if (poolType != null)
            {
                CardPoolModel? mappedPool = ModelDb.AllCardPools.FirstOrDefault(pool => pool.GetType() == poolType);
                if (mappedPool != null)
                {
                    _resolvedPool = mappedPool;
                    return mappedPool;
                }
            }

            CardPoolModel? discoveredPool = ModelDb.AllCardPools
                .FirstOrDefault(pool => pool is not MockCardPool && pool.AllCardIds.Contains(Id));
            if (discoveredPool != null)
            {
                _resolvedPool = discoveredPool;
                return discoveredPool;
            }

            _resolvedPool = Rarity == CardRarity.Token
                ? ModelDb.CardPool<TokenCardPool>()
                : ModelDb.CardPool<ColorlessCardPool>();
            return _resolvedPool;
        }
    }

    /// <summary>
    /// Override for card-specific gold glow logic when the stronger bonus line is active.
    /// Prefer this over overriding the vanilla <c>ShouldGlowGoldInternal</c>.
    /// </summary>
    protected virtual bool ShouldGlowGoldInHand => false;

    /// <summary>
    /// Override for card-specific red glow logic when the hand should show a warning state.
    /// Prefer this over overriding the vanilla <c>ShouldGlowRedInternal</c>.
    /// </summary>
    protected virtual bool ShouldGlowRedInHand => false;

    protected override bool ShouldGlowGoldInternal =>
        ShouldGlowGoldInHand || _constructedHandGlowRules.MatchesGold(this);

    protected override bool ShouldGlowRedInternal =>
        ShouldGlowRedInHand || _constructedHandGlowRules.MatchesRed(this);

    protected override IEnumerable<DynamicVar> CanonicalVars => _constructedDynamicVars;
    public override IEnumerable<CardKeyword> CanonicalKeywords => _cardKeywords;
    protected override HashSet<CardTag> CanonicalTags => _constructedTags;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        _hoverTips.Select(t => t(this))
            .Concat(_multiHoverTips.SelectMany(mt => mt(this)));

    protected YuWanCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true)
        : base(baseCost, type, rarity, target, showInCardLibrary)
    {
        if (autoAdd) ContentRegistry.AddModel(GetType());
    }

    protected YuWanCardModel WithVars(params DynamicVar[] vars)
    {
        foreach (var dynVar in vars)
        {
            _constructedDynamicVars.Add(dynVar);
            var t = dynVar.GetType();
            if (!t.IsGenericType) continue;
            foreach (var arg in t.GetGenericArguments())
            {
                if (arg.IsAssignableTo(typeof(PowerModel)))
                    WithTip(arg);
            }
        }
        return this;
    }

    protected YuWanCardModel WithVar(string name, int baseVal, int upgrade = 0)
    {
        _constructedDynamicVars.Add(new DynamicVar(name, baseVal).WithUpgrade(upgrade));
        return this;
    }

    protected YuWanCardModel WithVar(DynamicVar var)
    {
        return WithVars(var);
    }

    protected YuWanCardModel WithDamage(int baseVal, int upgrade = 0)
    {
        _constructedDynamicVars.Add(new DamageVar(baseVal, ValueProp.Move).WithUpgrade(upgrade));
        return this;
    }

    protected YuWanCardModel WithCalculatedDamage(ValueProp props, Func<CardModel, Creature?, decimal> multiplierCalc, int baseVal = 0, int extraVal = 0, int baseUpgrade = 0, int extraUpgrade = 0)
    {
        _constructedDynamicVars.Add(new CalculationBaseVar(baseVal).WithUpgrade(baseUpgrade));
        _constructedDynamicVars.Add(new ExtraDamageVar(extraVal).WithUpgrade(extraUpgrade));
        var calculatedVar = new CalculatedDamageVar(props).WithMultiplier(multiplierCalc);
        _constructedDynamicVars.Add(calculatedVar);
        return this;
    }

    protected YuWanCardModel WithBlock(int baseVal, int upgrade = 0)
    {
        _constructedDynamicVars.Add(new BlockVar(baseVal, ValueProp.Move).WithUpgrade(upgrade));
        return this;
    }

    protected YuWanCardModel WithCards(int baseVal, int upgrade = 0)
    {
        _constructedDynamicVars.Add(new CardsVar(baseVal).WithUpgrade(upgrade));
        return this;
    }

    protected YuWanCardModel WithEnergy(int baseVal, int upgrade = 0)
    {
        _constructedDynamicVars.Add(new EnergyVar(baseVal).WithUpgrade(upgrade));
        _hoverTips.Add(new(card => HoverTipFactory.ForEnergy(card)));
        return this;
    }

    protected YuWanCardModel WithGold(int baseVal, int upgrade = 0)
    {
        _constructedDynamicVars.Add(new GoldVar(baseVal).WithUpgrade(upgrade));
        return this;
    }

    protected YuWanCardModel WithHeal(int baseVal, int upgrade = 0)
    {
        _constructedDynamicVars.Add(new HealVar(baseVal).WithUpgrade(upgrade));
        return this;
    }

    protected YuWanCardModel WithPower<T>(int baseVal, int upgrade = 0) where T : PowerModel
    {
        _constructedDynamicVars.Add(new PowerVar<T>(baseVal).WithUpgrade(upgrade));
        _hoverTips.Add(new(_ => HoverTipFactory.FromPower<T>()));
        return this;
    }

    protected YuWanCardModel WithPower<T>(string name, int baseVal, int upgrade = 0) where T : PowerModel
    {
        _constructedDynamicVars.Add(new PowerVar<T>(name, baseVal).WithUpgrade(upgrade));
        _hoverTips.Add(new(_ => HoverTipFactory.FromPower<T>()));
        return this;
    }

    protected YuWanCardModel WithTags(params CardTag[] tags)
    {
        foreach (var tag in tags) _constructedTags.Add(tag);
        return this;
    }

    protected YuWanCardModel WithKeywords(params CardKeyword[] keywords)
    {
        _cardKeywords.AddRange(keywords);
        return this;
    }

    protected YuWanCardModel WithKeyword(CardKeyword keyword, UpgradeType upgradeType = UpgradeType.None)
    {
        if (upgradeType != UpgradeType.Add) _cardKeywords.Add(keyword);
        if (upgradeType != UpgradeType.None) _upgradeKeywords.Add((keyword, upgradeType));
        return this;
    }

    protected YuWanCardModel WithTip(Func<CardModel, IHoverTip> tipSource)
    {
        _hoverTips.Add(tipSource);
        return this;
    }

    protected YuWanCardModel WithTip(TooltipSource tipSource)
    {
        _hoverTips.Add(card => tipSource.Tip(card));
        return this;
    }

    protected YuWanCardModel WithTip(Type t)
    {
        if (t.IsAssignableTo(typeof(PowerModel)))
            _hoverTips.Add(_ => HoverTipFactory.FromPower(ModelDb.GetById<PowerModel>(ModelDb.GetId(t))));
        else if (t.IsAssignableTo(typeof(CardModel)))
            _hoverTips.Add(_ => HoverTipFactory.FromCard(ModelDb.GetById<CardModel>(ModelDb.GetId(t))));
        else if (t.IsAssignableTo(typeof(PotionModel)))
            _hoverTips.Add(_ => HoverTipFactory.FromPotion(ModelDb.GetById<PotionModel>(ModelDb.GetId(t))));
        else if (t.IsAssignableTo(typeof(EnchantmentModel)))
            _hoverTips.Add(_ => ModelDb.GetById<EnchantmentModel>(ModelDb.GetId(t)).HoverTip);
        return this;
    }

    protected YuWanCardModel WithTip(CardKeyword keyword)
    {
        _hoverTips.Add(card => HoverTipFactory.FromKeyword(keyword));
        return this;
    }

    protected YuWanCardModel WithTips(Func<CardModel, IEnumerable<IHoverTip>> multiTipSource)
    {
        _multiHoverTips.Add(multiTipSource);
        return this;
    }

    protected YuWanCardModel WithEnergyTip()
    {
        _hoverTips.Add(new(card => HoverTipFactory.ForEnergy(card)));
        return this;
    }

    /// <summary>
    /// Adds declarative in-hand glow rules for this card instance. Multiple calls OR-merge both channels.
    /// </summary>
    protected YuWanCardModel WithHandGlow(CardHandGlowRules rules)
    {
        _constructedHandGlowRules = _constructedHandGlowRules.Or(rules);
        return this;
    }

    /// <summary>
    /// Adds a gold in-hand glow rule for this card instance.
    /// </summary>
    protected YuWanCardModel WithHandGlowGold(Func<CardModel, bool> whenBonusActive)
    {
        return WithHandGlow(CardHandGlowRules.Gold(whenBonusActive));
    }

    /// <summary>
    /// Adds a red in-hand glow rule for this card instance.
    /// </summary>
    protected YuWanCardModel WithHandGlowRed(Func<CardModel, bool> whenHandWarning)
    {
        return WithHandGlow(CardHandGlowRules.Red(whenHandWarning));
    }

    internal int? CostUpgrade;

    [SavedProperty]
    public int YUWANCARD_Edition { get; set; }

    [SavedProperty]
    public bool YUWANCARD_FoilApplied { get; set; }

    public BalatroCardEdition BalatroEdition =>
        YUWANCARD_Edition is >= (int)BalatroCardEdition.Foil and <= (int)BalatroCardEdition.Negative
            ? (BalatroCardEdition)YUWANCARD_Edition
            : BalatroCardEdition.None;

    public bool HasBalatroEdition => BalatroEdition != BalatroCardEdition.None;

    protected YuWanCardModel WithCostUpgradeBy(int amount)
    {
        CostUpgrade = amount;
        return this;
    }

    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();

        AddBalatroEditionKeywordIfNeeded();

        if (BalatroEdition == BalatroCardEdition.Foil && !YUWANCARD_FoilApplied)
        {
            ApplyFoilEdition();
        }
    }

    public bool CanApplyBalatroEdition(BalatroCardEdition edition)
    {
        if (edition == BalatroCardEdition.None || HasBalatroEdition)
        {
            return false;
        }

        return Type is not CardType.None and not CardType.Status and not CardType.Curse and not CardType.Quest;
    }

    public bool TryApplyBalatroEdition(BalatroCardEdition edition)
    {
        if (!CanApplyBalatroEdition(edition))
        {
            return false;
        }

        YUWANCARD_Edition = (int)edition;
        AddBalatroEditionKeywordIfNeeded();
        if (edition == BalatroCardEdition.Foil)
        {
            ApplyFoilEdition();
        }

        return true;
    }

    public int GetBalatroPlayCountBonus()
    {
        return BalatroEdition == BalatroCardEdition.Polychrome ? 1 : 0;
    }

    private void AddBalatroEditionKeywordIfNeeded()
    {
        if (!HasBalatroEdition)
        {
            return;
        }

        CardKeyword keyword = BalatroCardEditionHelper.GetEditionKeyword(BalatroEdition);
        if (keyword != CardKeyword.None && !Keywords.Contains(keyword))
        {
            AddKeyword(keyword);
        }
    }

    private void ApplyFoilEdition()
    {
        BalatroCardEditionHelper.ApplyFoilEdition(this);
        YUWANCARD_FoilApplied = true;
    }

    public void ConstructedUpgrade()
    {
        foreach (var (keyword, upgradeType) in _upgradeKeywords)
        {
            switch (upgradeType)
            {
                case UpgradeType.Add:
                    AddKeyword(keyword);
                    break;
                case UpgradeType.Remove:
                    RemoveKeyword(keyword);
                    break;
            }
        }
        if (CostUpgrade.HasValue)
            EnergyCost.UpgradeBy(CostUpgrade.Value);
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}

public interface ITranscendenceCard
{
    CardModel GetTranscendenceTransformedCard();
}

public interface IDustyTomeCard
{
    CharacterModel GetDustyTomeCharacter();
}
