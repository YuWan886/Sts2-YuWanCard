using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
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

    public override string PortraitPath => GetPortraitPath();
    public virtual string? CustomPortraitPath => null;

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
            string framePath = $"{FrameBasePath}.png";
            if (ResourceLoader.Exists(framePath))
                return ResourceLoader.Load<Texture2D>(framePath);
            return null;
        }
    }

    protected sealed override IEnumerable<DynamicVar> CanonicalVars => _constructedDynamicVars;
    public sealed override IEnumerable<CardKeyword> CanonicalKeywords => _cardKeywords;
    protected sealed override HashSet<CardTag> CanonicalTags => _constructedTags;

    protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
        _hoverTips.Select(t => t(this))
            .Concat(_multiHoverTips.SelectMany(mt => mt(this)));

    protected YuWanCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true)
        : base(baseCost, type, rarity, target, showInCardLibrary)
    {
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

    internal int? CostUpgrade;

    protected YuWanCardModel WithCostUpgradeBy(int amount)
    {
        CostUpgrade = amount;
        return this;
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
