using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Characters;

namespace YuWanCard.Relics;

[Pool(typeof(PigRelicPool))]
public class PigCarrot : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PlatingPower>(6m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromPowerWithPowerHoverTips<PlatingPower>();

    public PigCarrot() : base(true)
    {
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            Flash();
            await PowerCmd.Apply<PlatingPower>(Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, null);
        }
    }

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
        {
            return options;
        }

        if (player.Character is not Pig)
        {
            return options;
        }

        var allCards = new HashSet<CardModel>(options.GetPossibleCards(player));
        allCards.UnionWith(GetAllUnlockedCards(player));

        return options.WithCustomPool(allCards);
    }

    public override IEnumerable<CardModel> ModifyMerchantCardPool(Player player, IEnumerable<CardModel> options)
    {
        if (player.Character is not Pig)
        {
            return options;
        }

        var allCards = new HashSet<CardModel>(options);
        allCards.UnionWith(GetAllUnlockedCards(player));

        return allCards;
    }

    private static HashSet<CardModel> GetAllUnlockedCards(Player player)
    {
        var allCards = new HashSet<CardModel>();
        
        foreach (var pool in ModelDb.AllCardPools)
        {
            foreach (var card in pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            {
                allCards.Add(card);
            }
        }

        return allCards;
    }
}
