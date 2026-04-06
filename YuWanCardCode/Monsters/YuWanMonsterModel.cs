using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YuWanCard.Monsters;

public abstract partial class YuWanMonsterModel : CustomMonsterModel
{
    private static readonly Regex CamelCaseRegex = new(@"([a-z])([A-Z])", RegexOptions.Compiled);

    protected virtual string MonsterId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string VisualsBasePath => $"res://YuWanCard/scenes/monsters/{MonsterId}_visuals";

    public override string? CustomVisualPath => $"{VisualsBasePath}.tscn";

    public override NCreatureVisuals? CreateCustomVisuals()
    {
        if (CustomVisualPath == null) return null;
        return NodeFactory<NCreatureVisuals>.CreateFromScene(CustomVisualPath);
    }

    public static string GenerateMonsterId<T>() where T : YuWanMonsterModel
    {
        return CamelCaseRegex.Replace(typeof(T).Name, "$1_$2").ToLowerInvariant();
    }

    public static string GenerateVisualsPath<T>() where T : YuWanMonsterModel
    {
        var monsterId = GenerateMonsterId<T>();
        return $"res://YuWanCard/scenes/monsters/{monsterId}_visuals.tscn";
    }
}
