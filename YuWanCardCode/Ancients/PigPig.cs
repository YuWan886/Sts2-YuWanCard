using System.Reflection;
using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics;

namespace YuWanCard.Ancients;

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
        ModelDb.Relic<LustfulPig>()
    ]);

    public PigPig()
    {
    }

    public override bool IsValidForAct(ActModel act) =>
        act.Id == ModelDb.Act<Hive>().Id || act.Id == ModelDb.Act<Glory>().Id;

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

    public override IEnumerable<EventOption> AllPossibleOptions => _validRelics.Value.Select(r => RelicOption(r.ToMutable()));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var randomSevenSinsIndex = Rng.NextInt(_validRelics.Value.Length);
        var selectedRelic = _validRelics.Value[randomSevenSinsIndex].ToMutable();
        
        var eventOptions = new List<EventOption>
        {
            RelicOption(selectedRelic),
            new(this, ChooseRandomRelic, "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_RELIC"),
            new(this, UpgradeCards, "YUWANCARD-PIG_PIG.pages.INITIAL.options.UPGRADE_CARDS")
        };
        
        return eventOptions;
    }

    private EventOption RelicOption(RelicModel relic)
    {
        var optionKey = $"YUWANCARD-PIG_PIG.pages.INITIAL.options.{relic.Id.Entry.Replace("YUWANCARD-", "").ToUpperInvariant()}";
        return EventOption.FromRelic(relic, this, () => ObtainRelic(relic), optionKey);
    }

    private async Task ObtainRelic(RelicModel relic)
    {
        await RelicCmd.Obtain(relic, Owner!);
        FinishEvent();
    }

    private async Task ChooseRandomRelic()
    {
        var sharedPool = ModelDb.RelicPool<SharedRelicPool>();
        var commonRelics = sharedPool.AllRelics.Where(r => r.Rarity == RelicRarity.Common).Select(r => r.ToMutable()).ToList().UnstableShuffle(Rng);
        var uncommonRelics = sharedPool.AllRelics.Where(r => r.Rarity == RelicRarity.Uncommon).Select(r => r.ToMutable()).ToList().UnstableShuffle(Rng);
        var shopRelics = sharedPool.AllRelics.Where(r => r.Rarity == RelicRarity.Shop).Select(r => r.ToMutable()).ToList().UnstableShuffle(Rng);
        var rareRelics = sharedPool.AllRelics.Where(r => r.Rarity == RelicRarity.Rare).Select(r => r.ToMutable()).ToList().UnstableShuffle(Rng);
        
        var relicsToOffer = new List<RelicModel>();
        relicsToOffer.AddRange(commonRelics.Take(1));
        relicsToOffer.AddRange(uncommonRelics.Take(2));
        relicsToOffer.AddRange(shopRelics.Take(1));
        relicsToOffer.AddRange(rareRelics.Take(1));
        
        if (relicsToOffer.Count == 0)
        {
            FinishEvent();
            return;
        }
        
        var firstRelic = await RelicSelectCmd.FromChooseARelicScreen(Owner!, relicsToOffer);
        if (firstRelic != null)
        {
            await RelicCmd.Obtain(firstRelic, Owner!);
            relicsToOffer.Remove(firstRelic);
        }
        
        if (relicsToOffer.Count > 0)
        {
            var secondRelic = await RelicSelectCmd.FromChooseARelicScreen(Owner!, relicsToOffer);
            if (secondRelic != null)
            {
                await RelicCmd.Obtain(secondRelic, Owner!);
            }
        }
        
        FinishEvent();
    }

    private async Task UpgradeCards()
    {
        var upgradeableCards = PileType.Deck.GetPile(Owner!).Cards
            .Where(c => c.IsUpgradable)
            .ToList();

        if (upgradeableCards.Count == 0)
        {
            await ChooseRandomRelic();
            return;
        }

        var cardsToUpgrade = await CardSelectCmd.FromDeckForUpgrade(
            Owner!,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, Math.Min(3, upgradeableCards.Count))
        );

        foreach (var card in cardsToUpgrade)
        {
            CardCmd.Upgrade(card);
        }
        FinishEvent();
    }

    private void FinishEvent()
    {
        Done();
    }
}