using System.Reflection;
using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Config;
using YuWanCard.Relics;

namespace YuWanCard.Ancients;

// [RegisterAncient] ensures ContentRegistry.AncientTypes includes this type for canonical instance creation.
// The base constructor also calls CustomAncientRegistry.Register(this), which serves a different purpose
// (registering the runtime instance). Both are needed and are deduplicated internally.
[RegisterAncient]
public class PigPig : YuWanAncientModel
{
    private const string IconBasePath = "res://YuWanCard/images/ancients/pig_pig";

    private static readonly Lazy<RelicModel[]> _validRelics = new(() =>
    [
        ModelDb.Relic<ArrogantPig>(),
        ModelDb.Relic<JealousPig>(),
        ModelDb.Relic<FuriousPig>(),
        ModelDb.Relic<LazyPig>(),
        ModelDb.Relic<GreedyPig>(),
        ModelDb.Relic<GluttonousPig>(),
        ModelDb.Relic<LustfulPig>(),
        ModelDb.Relic<PigStandChicken>(),
        ModelDb.Relic<HongMengPig>(),
        ModelDb.Relic<TankPig>(),
        ModelDb.Relic<TrapPig>(),
        ModelDb.Relic<CrystalPig>()
    ]);

    public PigPig()
    {
    }

    public override bool IsValidForAct(ActModel act) =>
        act.Id == ModelDb.Act<Hive>().Id
        && YuWanContentAvailability.IsAncientTypeEnabled<PigPig>();

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;

    private const string RunHistoryIconPath = "res://YuWanCard/images/ancients/pig_pig.png";
    private const string RunHistoryIconOutlinePathStr = "res://YuWanCard/images/ancients/pig_pig.png";

    public override string? CustomScenePath => "res://YuWanCard/scenes/ancients/pig_pig.tscn";
    public override string? CustomMapIconPath => $"{IconBasePath}.png";
    public override string? CustomMapIconOutlinePath => $"{IconBasePath}.png";
    
    public override string? CustomRunHistoryIconPath => RunHistoryIconPath;
    public override string? CustomRunHistoryIconOutlinePath => RunHistoryIconOutlinePathStr;

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        yield return "res://scenes/events/ancient_event_layout.tscn";
        yield return CustomScenePath!;
        yield return RunHistoryIconPath;
        yield return RunHistoryIconOutlinePathStr;
        yield return CustomMapIconPath!;
    }

    private string FirstVisit => $"{Id.Entry}.talk.firstvisitEver.0-0.ancient";
    
    // Use reflection to set init-only properties — avoids modreq(IsExternalInit) JIT crash on Android/Mono
    private static readonly PropertyInfo? _adsFirstVisitProp = typeof(AncientDialogueSet).GetProperty("FirstVisitEverDialogue");
    private static readonly PropertyInfo? _adsCharDialoguesProp = typeof(AncientDialogueSet).GetProperty("CharacterDialogues");
    private static readonly PropertyInfo? _adsAgnosticProp = typeof(AncientDialogueSet).GetProperty("AgnosticDialogues");

    protected override AncientDialogueSet DefineDialogues()
    {
        var sfxPath = AncientDialogueUtil.SfxPath(FirstVisit);
        var firstVisit = new AncientDialogue(sfxPath);

        var characterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>();

        foreach (var character in ModelDb.AllCharacters)
        {
            var baseKey = AncientDialogueUtil.BaseLocKey(Id.Entry, character.Id.Entry);
            characterDialogues[character.Id.Entry] = AncientDialogueUtil.GetDialoguesForKey("ancients", baseKey);
        }

        var set = (AncientDialogueSet)Activator.CreateInstance(typeof(AncientDialogueSet))!;
        _adsFirstVisitProp?.SetValue(set, firstVisit);
        _adsCharDialoguesProp?.SetValue(set, characterDialogues);
        _adsAgnosticProp?.SetValue(set, AncientDialogueUtil.GetDialoguesForKey("ancients", AncientDialogueUtil.BaseLocKey(Id.Entry, "ANY")));
        return set;
    }

    protected override OptionPools MakeOptionPools => new(
        MakePool(Array.Empty<RelicModel>()),
        MakePool(Array.Empty<RelicModel>()),
        MakePool(Array.Empty<RelicModel>())
    );

    public override IEnumerable<EventOption> AllPossibleOptions =>
        _validRelics.Value.Select(relic => RelicOption(relic.ToMutable(), "INITIAL"));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return _validRelics.Value
            .Select(relic => RelicOption(relic.ToMutable(), "INITIAL"))
            .ToList()
            .UnstableShuffle(Rng)
            .Take(3)
            .ToList();
    }
}
