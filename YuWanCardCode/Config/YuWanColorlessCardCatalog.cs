using MegaCrit.Sts2.Core.Models;
using YuWanCard.Cards;
using YuWanCard.Cards.Colorless;

namespace YuWanCard.Config;

internal readonly record struct YuWanColorlessCardDefinition(
    string Key,
    Type CardType);

internal static class YuWanColorlessCardCatalog
{
    public const string SectionId = "colorless_cards";
    public const int ButtonsPerRow = 5;

    public static readonly IReadOnlyList<YuWanColorlessCardDefinition> Cards =
    [
        Create<AllIn>("all_in"),
        Create<BloodWheelEye>("blood_wheel_eye"),
        Create<BorrowKnifeToKill>("borrow_knife_to_kill"),
        Create<BraveRib>("brave_rib"),
        Create<BullyLittlePig>("bully_little_pig"),
        Create<CallCompanions>("call_companions"),
        Create<CleanPig>("clean_pig"),
        Create<DefeatBringsSorrow>("defeat_brings_sorrow"),
        Create<DoNotDie>("do_not_die"),
        Create<FitnessMouse>("fitness_mouse"),
        Create<Fps>("fps"),
        Create<Gambler>("gambler"),
        Create<GiveYou>("give_you"),
        Create<JusticeIronFist>("justice_iron_fist"),
        Create<KouKouSpace>("kou_kou_space"),
        Create<LightBoatPastMountains>("light_boat_past_mountains"),
        Create<LittleRegent>("little_regent"),
        Create<LittleSnakeBite>("little_snake_bite"),
        Create<Lolicon>("lolicon"),
        Create<LotsOfMods>("lots_of_mods"),
        Create<MelancholyRabbit>("melancholy_rabbit"),
        Create<Nyjk>("nyjk"),
        Create<OldPigCalendar>("old_pig_calendar"),
        Create<PigAngry>("pig_angry"),
        Create<PigBankruptcy>("pig_bankruptcy"),
        Create<PigBite>("pig_bite"),
        Create<PigBrainOverload>("pig_brain_overload"),
        Create<PigBully>("pig_bully"),
        Create<PigBusyCome>("pig_busy_come"),
        Create<PigCrash>("pig_crash"),
        Create<PigDefection>("pig_defection"),
        Create<PigDoubt>("pig_doubt"),
        Create<PigDragonRide>("pig_dragon_ride"),
        Create<PigGaze>("pig_gaze"),
        Create<PigHurt>("pig_hurt"),
        Create<PigKing>("pig_king"),
        Create<PigMelt>("pig_melt"),
        Create<PigOffWork>("pig_off_work"),
        Create<PigSacrifice>("pig_sacrifice"),
        Create<PigSleep>("pig_sleep"),
        Create<PigTeammate>("pig_teammate"),
        Create<PigThink>("pig_think"),
        Create<PigTouchFish>("pig_touch_fish"),
        Create<PressureYou>("pressure_you"),
        Create<PrideComesBeforeFall>("pride_comes_before_fall"),
        Create<PullNetCable>("pull_net_cable"),
        Create<RainDark>("rain_dark"),
        Create<ReviveKai>("revive_kai"),
        Create<SadArmyWin>("sad_army_win"),
        Create<Sha>("sha"),
        Create<Shan>("shan"),
        Create<ShieldToFront>("shield_to_front"),
        Create<StealCard>("steal_card"),
        Create<StoneCarryingKing>("stone_carrying_king"),
        Create<TenDayElbow>("ten_day_elbow"),
        Create<TiaoJiao>("tiao_jiao"),
        Create<TurnToSpecimen>("turn_to_specimen"),
        Create<UserGotAngry>("user_got_angry"),
        Create<VictoryBreedsArrogance>("victory_breeds_arrogance"),
        Create<Wyjk>("wyjk"),
        Create<YouAreDumbCry>("you_are_dumb_cry"),
        Create<YouArePig>("you_are_pig"),
        Create<YuWanForgot>("yuwan_forgot"),
    ];

    private static readonly Dictionary<Type, YuWanColorlessCardDefinition> DefinitionsByType =
        Cards.ToDictionary(static definition => definition.CardType);

    private static readonly Dictionary<string, YuWanColorlessCardDefinition> DefinitionsByKey =
        Cards.ToDictionary(static definition => definition.Key, StringComparer.Ordinal);

    public static bool TryGetDefinition(Type cardType, out YuWanColorlessCardDefinition definition)
        => DefinitionsByType.TryGetValue(cardType, out definition);

    public static bool TryGetDefinition(string key, out YuWanColorlessCardDefinition definition)
        => DefinitionsByKey.TryGetValue(key, out definition);

    public static CardModel CreateCanonicalCard(YuWanColorlessCardDefinition definition)
        => ModelDb.GetById<CardModel>(ModelDb.GetId(definition.CardType));

    private static YuWanColorlessCardDefinition Create<TCard>(string key)
        where TCard : CardModel
    {
        return new(
            key,
            typeof(TCard));
    }
}
