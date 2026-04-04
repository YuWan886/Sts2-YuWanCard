using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Modifiers;

public abstract partial class YuWanModifierModel : ModifierModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();
    private static readonly List<YuWanModifierModel> _registeredModifiers = [];

    public static IReadOnlyList<YuWanModifierModel> RegisteredModifiers => _registeredModifiers.AsReadOnly();

    protected virtual string ModifierId
    {
        get
        {
            string name = GetType().Name;
            if (name.EndsWith("Modifier"))
            {
                name = name[..^"Modifier".Length];
            }
            return "YUWANCARD-" + CamelCaseRegex.Replace(name, "$1_$2").ToUpperInvariant();
        }
    }

    protected virtual string IconBasePath
    {
        get
        {
            string name = GetType().Name;
            if (name.EndsWith("Modifier"))
            {
                name = name[..^"Modifier".Length];
            }
            return $"res://YuWanCard/images/modifiers/{CamelCaseRegex.Replace(name, "$1_$2").ToLowerInvariant()}.png";
        }
    }

    protected override string IconPath => IconBasePath;

    public override LocString Title => new("modifiers", ModifierId + ".title");
    public override LocString Description => new("modifiers", ModifierId + ".description");
    public override LocString NeowOptionTitle => new("modifiers", ModifierId + ".neow_title");
    public override LocString NeowOptionDescription => new("modifiers", ModifierId + ".neow_description");

    protected YuWanModifierModel()
    {
        _registeredModifiers.Add(this);
    }

    public static T? GetModifier<T>() where T : YuWanModifierModel
    {
        return _registeredModifiers.OfType<T>().FirstOrDefault();
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
