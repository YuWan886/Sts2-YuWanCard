using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Cards;

namespace YuWanCard.RestSite;

public sealed class RoastPorkRestSiteOption(Player owner) : RestSiteOption(owner)
{
    private static readonly string CustomIconPath = "res://YuWanCard/images/ui/rest_site/option_roast_pork.png";

    public static bool WasRoastPorkSelected { get; private set; }

    public static void ResetState()
    {
        WasRoastPorkSelected = false;
    }

    public override string OptionId => "ROAST_PORK";

    public override IEnumerable<string> AssetPaths => [CustomIconPath];

    public override LocString Description
    {
        get
        {
            LocString locString = new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");
            locString.Add("HpLoss", 3m);
            locString.Add("CardCount", 1m);
            return locString;
        }
    }

    public override async Task<bool> OnSelect()
    {
        WasRoastPorkSelected = true;

        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, 3m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);

        var allPlayers = Owner.RunState.Players;
        foreach (var player in allPlayers)
        {
            if (player != Owner && player.Creature.CurrentHp > 0)
            {
                var pigChopCard = Owner.RunState.CreateCard(ModelDb.Card<PigChop>(), player);
                await CardPileCmd.Add(pigChopCard, PileType.Deck);
            }
        }

        return true;
    }
}
