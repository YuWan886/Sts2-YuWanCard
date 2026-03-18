using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Ancients;

public class PigPig : CustomAncientModel
{
    private const string IconBasePath = "res://YuWanCard/images/ancients/pig_pig";

    private static readonly Regex PigCardPattern = new(@"^YUWANCARD-PIG_", RegexOptions.Compiled);

    public PigPig() : base(autoAdd: true)
    {
    }

    public override bool IsValidForAct(ActModel act) =>
        act.Id == ModelDb.Act<Hive>().Id || act.Id == ModelDb.Act<Glory>().Id;

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;

    private const string RunHistoryIconPath = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig.png";
    private const string RunHistoryIconOutlinePathStr = "res://YuWanCard/images/ui/run_history/yuwancard-pig_pig_outline.png";
    
    private static Texture2D? _cachedRunHistoryIcon;
    private static Texture2D? _cachedRunHistoryIconOutline;
    
    public override string? CustomScenePath => "res://YuWanCard/scenes/ancients/pig_pig.tscn";
    public override string? CustomMapIconPath => $"{IconBasePath}.png";
    public override string? CustomMapIconOutlinePath => $"{IconBasePath}.png";
    
    public override Texture2D? CustomRunHistoryIcon
    {
        get
        {
            if (_cachedRunHistoryIcon == null)
            {
                _cachedRunHistoryIcon = GD.Load<Texture2D>(RunHistoryIconPath);
                if (_cachedRunHistoryIcon == null)
                {
                    MainFile.Logger.Warn($"Failed to load PigPig run history icon from {RunHistoryIconPath}");
                }
            }
            return _cachedRunHistoryIcon;
        }
    }
    
    public override Texture2D? CustomRunHistoryIconOutline
    {
        get
        {
            if (_cachedRunHistoryIconOutline == null)
            {
                _cachedRunHistoryIconOutline = GD.Load<Texture2D>(RunHistoryIconOutlinePathStr);
                if (_cachedRunHistoryIconOutline == null)
                {
                    MainFile.Logger.Warn($"Failed to load PigPig run history outline icon from {RunHistoryIconOutlinePathStr}");
                }
            }
            return _cachedRunHistoryIconOutline;
        }
    }

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
        MakePool(ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Circlet>()),
        MakePool(ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Circlet>()),
        MakePool(ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Circlet>())
    );

    public override IEnumerable<EventOption> AllPossibleOptions => [];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            new(this, ChoosePigCard, "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_CARD"),
            new(this, ChooseRelic, "YUWANCARD-PIG_PIG.pages.INITIAL.options.CHOOSE_RELIC"),
            new(this, UpgradeCards, "YUWANCARD-PIG_PIG.pages.INITIAL.options.UPGRADE_CARDS")
        };
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
        var cardsToOffer = shuffled.Take(Math.Min(5, shuffled.Count)).ToList();
        var cardReward = new CardReward(cardsToOffer, CardCreationSource.Other, Owner!);
        await RewardsCmd.OfferCustom(Owner!, [cardReward]);
        FinishEvent();
    }

    private async Task ChooseRelic()
    {
        List<RelicModel> relics = [];
        for (int i = 0; i < 3; i++)
        {
            var relic = RelicFactory.PullNextRelicFromFront(Owner!).ToMutable();
            relics.Add(relic);
        }
        var selectedRelic = await RelicSelectCmd.FromChooseARelicScreen(Owner!, relics);
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
            FinishEvent();
            return;
        }

        var cardsToUpgrade = await CardSelectCmd.FromDeckForUpgrade(
            Owner!,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, Math.Min(5, upgradeableCards.Count))
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
            .Where(c => PigCardPattern.IsMatch(c.Id.Entry))
            .Select(c => Owner.RunState.CreateCard(c, Owner))];
    }

    private void FinishEvent()
    {
        var doneMethod = typeof(AncientEventModel).GetMethod("Done", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        doneMethod?.Invoke(this, null);
    }
}