using System.Collections.Generic;
using System.Linq;
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

    private static readonly HashSet<string> PigCardIds =
    [
        "YUWANCARD-PIG_HURT",
        "YUWANCARD-PIG_THINK",
        "YUWANCARD-PIG_ANGRY",
        "YUWANCARD-PIG_SLEEP",
        "YUWANCARD-PIG_SACRIFICE",
        "YUWANCARD-PIG_DOUBT"
    ];

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
    public override Texture2D? CustomRunHistoryIcon => GD.Load<Texture2D>(RunHistoryIconPath);
    public override Texture2D? CustomRunHistoryIconOutline => GD.Load<Texture2D>(RunHistoryIconOutlinePathStr);

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
            .Where(c => PigCardIds.Contains(c.Id.Entry))
            .Select(c => Owner.RunState.CreateCard(c, Owner))];
    }

    private void FinishEvent()
    {
        var doneMethod = typeof(AncientEventModel).GetMethod("Done", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        doneMethod?.Invoke(this, null);
    }
}