using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class FoodPigTrackerPower : YuWanPowerModel
{
    private sealed class Data
    {
        public bool PlayedFoodThisTurn;
        public HashSet<string> DistinctFoodIds = [];
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    protected override bool IsVisibleInternal => false;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public bool HasPlayedFoodThisTurn => GetInternalData<Data>().PlayedFoodThisTurn;

    public int DistinctFoodPlayedCount => GetInternalData<Data>().DistinctFoodIds.Count;

    public void RecordPlayedFood(CardModel card)
    {
        Data data = GetInternalData<Data>();
        data.PlayedFoodThisTurn = true;
        data.DistinctFoodIds.Add(CardUtils.GetFoodPigIdentity(card));
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
        {
            GetInternalData<Data>().PlayedFoodThisTurn = false;
        }

        return Task.CompletedTask;
    }
}
