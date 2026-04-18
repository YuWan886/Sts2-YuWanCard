using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics;

namespace YuWanCard.Ancients;

public class PigPig : CustomAncientModel
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

    public PigPig() : base(autoAdd: true)
    {
    }

    public override bool IsValidForAct(ActModel act) =>
        act.Id == ModelDb.Act<Hive>().Id || act.Id == ModelDb.Act<Glory>().Id;

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;

    private const string RunHistoryIconPath = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig.png";
    private const string RunHistoryIconOutlinePathStr = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig_outline.png";
    
    public override string? CustomScenePath => "res://YuWanCard/scenes/ancients/pig_pig.tscn";
    public override string? CustomMapIconPath => $"{IconBasePath}.png";
    public override string? CustomMapIconOutlinePath => $"{IconBasePath}.png";
    
    public override string? CustomRunHistoryIconPath => RunHistoryIconPath;
    public override string? CustomRunHistoryIconOutlinePath => RunHistoryIconOutlinePathStr;

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        foreach (var path in base.GetAssetPaths(runState))
        {
            yield return path;
        }
        
        yield return RunHistoryIconPath;
        yield return RunHistoryIconOutlinePathStr;
        yield return CustomMapIconPath!;
    }

    private string FirstVisit => $"{Id.Entry}.talk.firstvisitEver.0-0.ancient";
    
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
        
        return new AncientDialogueSet
        {
            FirstVisitEverDialogue = firstVisit,
            CharacterDialogues = characterDialogues,
            AgnosticDialogues = AncientDialogueUtil.GetDialoguesForKey("ancients", AncientDialogueUtil.BaseLocKey(Id.Entry, "ANY"))
        };
    }

    protected override OptionPools MakeOptionPools => new(
        MakePool(Array.Empty<RelicModel>()),
        MakePool(Array.Empty<RelicModel>()),
        MakePool(Array.Empty<RelicModel>())
    );

    public override IEnumerable<EventOption> AllPossibleOptions => _validRelics.Value.Select(r => RelicOption(r.ToMutable()));

    private bool _isRelicReward;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var randomSevenSinsIndex = Rng.NextInt(_validRelics.Value.Length);
        var selectedRelic = _validRelics.Value[randomSevenSinsIndex].ToMutable();
        
        _isRelicReward = Rng.NextInt(2) == 0;
        var thirdOptionKey = _isRelicReward 
            ? "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_RELIC" 
            : "YUWANCARD-PIG_PIG.pages.INITIAL.options.UPGRADE_CARDS";
        
        var eventOptions = new List<EventOption>
        {
            RelicOption(selectedRelic),
            new(this, ChoosePigCard, "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_CARD"),
            new(this, ChooseRelicOrUpgrade, thirdOptionKey)
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

    private async Task ChoosePigCard()
    {
        var pigCards = GetPigCards();
        if (pigCards.Count == 0)
        {
            FinishEvent();
            return;
        }

        var shuffled = pigCards.OrderBy(_ => Rng.NextInt()).ToList();
        var cardsToOffer = shuffled.Take(Math.Min(6, shuffled.Count)).ToList();
        var cardCreationResults = cardsToOffer.Select(c => new CardCreationResult(c)).ToList();
        
        var prefs = new CardSelectorPrefs(
            new LocString("ancients", "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_CARD.selectionScreenPrompt"),
            0,
            3
        );
        
        var selectedCards = await CardSelectCmd.FromSimpleGridForRewards(
            prefs: prefs,
            context: new BlockingPlayerChoiceContext(),
            cards: cardCreationResults,
            player: Owner!
        );
        
        foreach (var card in selectedCards)
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
        }
        
        FinishEvent();
    }

    private async Task ChooseRelicOrUpgrade()
    {
        if (_isRelicReward)
        {
            await ChooseRandomRelic();
        }
        else
        {
            await UpgradeCards();
        }
    }

    private async Task ChooseRandomRelic()
    {
        var sharedPool = ModelDb.RelicPool<SharedRelicPool>();
        var uncommonRelics = sharedPool.AllRelics.Where(r => r.Rarity == RelicRarity.Uncommon).ToList();
        var rareRelics = sharedPool.AllRelics.Where(r => r.Rarity == RelicRarity.Rare).ToList();
        
        var shuffledUncommon = uncommonRelics.Select(r => r.ToMutable()).ToList().UnstableShuffle(Rng);
        var shuffledRare = rareRelics.Select(r => r.ToMutable()).ToList().UnstableShuffle(Rng);
        
        var relicsToOffer = new List<RelicModel>();
        relicsToOffer.AddRange(shuffledUncommon.Take(1));
        relicsToOffer.AddRange(shuffledRare.Take(2));
        
        var selectedRelic = await RelicSelectCmd.FromChooseARelicScreen(Owner!, relicsToOffer);
        if (selectedRelic != null)
        {
            await RelicCmd.Obtain(selectedRelic, Owner!);
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

    private List<CardModel> GetPigCards()
    {
        var colorlessPool = ModelDb.CardPool<ColorlessCardPool>();
        var allCards = colorlessPool.GetUnlockedCards(Owner!.UnlockState, Owner.RunState.CardMultiplayerConstraint);
        
        return [.. allCards
            .Where(c => (c.Id.Entry.Contains("PIG_") || c.Id.Entry.Contains("YOU_ARE_PIG")) && !c.Id.Entry.Contains("POWER"))
            .Select(c => Owner.RunState.CreateCard(c, Owner))];
    }



    private void FinishEvent()
    {
        Done();
    }
}