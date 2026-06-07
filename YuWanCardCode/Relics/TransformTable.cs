using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Persistence;
using YuWanCard.Core.RightClick;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class TransformTable : YuWanRelicModel, IYuWanRightClickableRelic
{
    private const int MaxTransformsPerTurn = 2;
    private static readonly LocString SelectionPrompt = new("relics", "YUWANCARD-TRANSFORM_TABLE.selectionPrompt");
    private static readonly SavedAttachedState<TransformTable, int> RemainingTransformsState =
        new(nameof(YUWANCARD_RemainingTransforms), () => 0);

    private int YUWANCARD_RemainingTransforms
    {
        get => RemainingTransformsState.GetValueOrDefault(this, 0);
        set => RemainingTransformsState[this] = value;
    }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress && Owner != null;

    public override int DisplayAmount => YUWANCARD_RemainingTransforms;

    public TransformTable() : base(true)
    {
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            SetRemainingTransforms(0);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        SetRemainingTransforms(0);
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            SetRemainingTransforms(MaxTransformsPerTurn);
        }

        return Task.CompletedTask;
    }

    public bool CanHandleRightClickLocal(YuWanRightClickContext context)
    {
        return Owner != null
               && context.Player == Owner
               && LocalContext.IsMe(Owner)
               && CombatManager.Instance.IsPlayPhase
               && !CombatManager.Instance.PlayerActionsDisabled
               && YUWANCARD_RemainingTransforms > 0
               && GetConvertibleHandCards().Count > 0;
    }

    public bool CanExecuteRightClick(YuWanRightClickExecutionContext context)
    {
        return Owner != null
               && context.Player == Owner
               && CombatManager.Instance.IsPlayPhase
               && !CombatManager.Instance.PlayerActionsDisabled
               && YUWANCARD_RemainingTransforms > 0;
    }

    public async Task OnRightClick(YuWanRightClickExecutionContext context)
    {
        if (Owner == null)
        {
            return;
        }

        if (context.PlayerChoiceContext != null)
        {
            await ExecuteTransform(context.PlayerChoiceContext);
            return;
        }

        if (!LocalContext.NetId.HasValue)
        {
            return;
        }

        var hookContext = new HookPlayerChoiceContext(Owner, LocalContext.NetId.Value, GameActionType.CombatPlayPhaseOnly);
        Task task = ExecuteTransform(hookContext);
        await hookContext.AssignTaskAndWaitForPauseOrCompletion(task);
    }

    private async Task ExecuteTransform(PlayerChoiceContext choiceContext)
    {
        if (Owner == null || YUWANCARD_RemainingTransforms <= 0)
        {
            return;
        }

        List<CardModel> convertibleCards = GetConvertibleHandCards();
        if (convertibleCards.Count == 0)
        {
            return;
        }

        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionPrompt, 1, 1),
            IsConvertibleCard,
            this)).FirstOrDefault();

        if (selectedCard == null)
        {
            return;
        }

        int convertedEnergy = GetConvertibleEnergy(selectedCard);
        if (convertedEnergy <= 0)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(convertedEnergy, Owner);
        await CardPileCmd.RemoveFromCombat(selectedCard);
        SetRemainingTransforms(YUWANCARD_RemainingTransforms - 1);
    }

    private List<CardModel> GetConvertibleHandCards()
    {
        if (Owner == null)
        {
            return [];
        }

        return PileType.Hand.GetPile(Owner).Cards
            .Where(IsConvertibleCard)
            .ToList();
    }

    private static bool IsConvertibleCard(CardModel card)
    {
        return GetConvertibleEnergy(card) > 0;
    }

    private static int GetConvertibleEnergy(CardModel card)
    {
        if (card.EnergyCost == null || card.EnergyCost.CostsX)
        {
            return 0;
        }

        return card.EnergyCost.GetResolved();
    }

    private void SetRemainingTransforms(int value)
    {
        int clamped = Math.Max(0, value);
        if (YUWANCARD_RemainingTransforms == clamped)
        {
            return;
        }

        YUWANCARD_RemainingTransforms = clamped;
        InvokeDisplayAmountChanged();
    }
}
