using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using YuWanCard.Enchantments;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(SelfHelpBook), "GenerateInitialOptions")]
public class SelfHelpBookPatch
{
    private const int _enchantAmount = 2;
    private const int _maxOptionsToSelect = 3;

    private record EnchantmentOptionDef(
        Type EnchantmentType,
        int Weight,
        string OptionKey,
        string LockedOptionKey,
        string DescriptionKey,
        Func<Player, bool> AvailabilityCheck,
        Func<CardModel, bool> CardFilter
    );

    private static readonly EnchantmentOptionDef[] _enchantmentPool =
    [
        new(
            typeof(Sharp),
            Weight: 1,
            OptionKey: "SELF_HELP_BOOK.pages.INITIAL.options.READ_THE_BACK",
            LockedOptionKey: "SELF_HELP_BOOK.pages.INITIAL.options.READ_THE_BACK_LOCKED",
            DescriptionKey: "SELF_HELP_BOOK.pages.READ_THE_BACK.description",
            AvailabilityCheck: p => PlayerHasCardsForType<Sharp>(p, CardType.Attack),
            CardFilter: c => c.Type == CardType.Attack
        ),
        new(
            typeof(Nimble),
            Weight: 1,
            OptionKey: "SELF_HELP_BOOK.pages.INITIAL.options.READ_PASSAGE",
            LockedOptionKey: "SELF_HELP_BOOK.pages.INITIAL.options.READ_PASSAGE_LOCKED",
            DescriptionKey: "SELF_HELP_BOOK.pages.READ_PASSAGE.description",
            AvailabilityCheck: p => PlayerHasCardsForType<Nimble>(p, CardType.Skill),
            CardFilter: c => c.Type == CardType.Skill
        ),
        new(
            typeof(Swift),
            Weight: 1,
            OptionKey: "SELF_HELP_BOOK.pages.INITIAL.options.READ_ENTIRE_BOOK",
            LockedOptionKey: "SELF_HELP_BOOK.pages.INITIAL.options.READ_ENTIRE_BOOK_LOCKED",
            DescriptionKey: "SELF_HELP_BOOK.pages.READ_ENTIRE_BOOK.description",
            AvailabilityCheck: p => PlayerHasCardsForType<Swift>(p, CardType.Power),
            CardFilter: c => c.Type == CardType.Power
        ),
        new(
            typeof(ArthropodKiller),
            Weight: 1,
            OptionKey: "YUWANCARD-SELF_HELP_BOOK.pages.INITIAL.options.ARTHROPOD_KILLER",
            LockedOptionKey: "YUWANCARD-SELF_HELP_BOOK.pages.INITIAL.options.ARTHROPOD_KILLER_LOCKED",
            DescriptionKey: "YUWANCARD-SELF_HELP_BOOK.pages.ARTHROPODKILLER.description",
            AvailabilityCheck: p => PlayerHasDamageCards<ArthropodKiller>(p),
            CardFilter: c => HasDamageVariable(c)
        ),
        new(
            typeof(SweepingBlade),
            Weight: 1,
            OptionKey: "YUWANCARD-SELF_HELP_BOOK.pages.INITIAL.options.SWEEPING_BLADE",
            LockedOptionKey: "YUWANCARD-SELF_HELP_BOOK.pages.INITIAL.options.SWEEPING_BLADE_LOCKED",
            DescriptionKey: "YUWANCARD-SELF_HELP_BOOK.pages.SWEEPINGBLADE.description",
            AvailabilityCheck: p => PlayerHasDamageCards<SweepingBlade>(p),
            CardFilter: c => HasDamageVariable(c)
        ),
        new(
            typeof(Venomous),
            Weight: 1,
            OptionKey: "YUWANCARD-SELF_HELP_BOOK.pages.INITIAL.options.VENOMOUS",
            LockedOptionKey: "YUWANCARD-SELF_HELP_BOOK.pages.INITIAL.options.VENOMOUS_LOCKED",
            DescriptionKey: "YUWANCARD-SELF_HELP_BOOK.pages.VENOMOUS.description",
            AvailabilityCheck: p => PlayerHasDamageCards<Venomous>(p),
            CardFilter: c => HasDamageVariable(c)
        )
    ];

    [HarmonyPostfix]
    public static void Postfix(SelfHelpBook __instance, ref IReadOnlyList<EventOption> __result)
    {
        var owner = __instance.Owner;

        if (owner == null)
        {
            return;
        }

        var availablePool = new WeightedList<EnchantmentOptionDef>();

        foreach (var optionDef in _enchantmentPool)
        {
            if (optionDef.AvailabilityCheck(owner))
            {
                availablePool.Add(optionDef, optionDef.Weight);
            }
        }

        if (availablePool.Count == 0)
        {
            return;
        }

        var options = new List<EventOption>();

        int optionsToSelect = Math.Min(_maxOptionsToSelect, availablePool.Count);

        for (int i = 0; i < optionsToSelect; i++)
        {
            var selected = availablePool.GetRandom(__instance.Rng, remove: true);
            options.Add(CreateEventOption(__instance, owner, selected, isAvailable: true));
        }

        __result = options;
    }

    private static EventOption CreateEventOption(
        SelfHelpBook eventModel,
        Player owner,
        EnchantmentOptionDef optionDef,
        bool isAvailable)
    {
        if (isAvailable)
        {
            return new EventOption(
                eventModel,
                () => SelectAndEnchant(eventModel, owner, optionDef),
                optionDef.OptionKey,
                CreateHoverTip(optionDef.EnchantmentType) ?? Array.Empty<IHoverTip>()
            );
        }
        else
        {
            return new EventOption(
                eventModel,
                null,
                optionDef.LockedOptionKey
            );
        }
    }

    private static IEnumerable<IHoverTip>? CreateHoverTip(Type enchantmentType)
    {
        var method = typeof(HoverTipFactory).GetMethod("FromEnchantment")?.MakeGenericMethod(enchantmentType);
        return method?.Invoke(null, [_enchantAmount]) as IEnumerable<IHoverTip>;
    }

    private static bool PlayerHasCardsForType<T>(Player player, CardType cardType) where T : EnchantmentModel
    {
        var enchantment = ModelDb.Enchantment<T>();
        return PileType.Deck.GetPile(player).Cards.Any(c => 
            c.Pile?.Type == PileType.Deck && 
            c.Type == cardType && 
            enchantment.CanEnchant(c));
    }

    private static bool PlayerHasDamageCards<T>(Player player) where T : EnchantmentModel
    {
        var enchantment = ModelDb.Enchantment<T>();
        return PileType.Deck.GetPile(player).Cards.Any(c => 
            c.Pile?.Type == PileType.Deck && 
            enchantment.CanEnchant(c) && 
            HasDamageVariable(c));
    }

    private static bool HasDamageVariable(CardModel? card)
    {
        if (card == null)
        {
            return false;
        }
        var vars = card.DynamicVars;
        return vars.ContainsKey("Damage") ||
               vars.ContainsKey("CalculatedDamage") ||
               vars.ContainsKey("OstyDamage") ||
               vars.ContainsKey("ExtraDamage");
    }

    private static async Task SelectAndEnchant(SelfHelpBook eventModel, Player owner, EnchantmentOptionDef optionDef)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
        var enchantment = GetEnchantmentByType(optionDef.EnchantmentType);
        var cardModel = (await CardSelectCmd.FromDeckForEnchantment(
            owner,
            enchantment,
            _enchantAmount,
            c => c is not null && c.Pile is not null && c.Pile.Type == PileType.Deck && enchantment!.CanEnchant(c) && optionDef!.CardFilter(c),
            prefs
        )).FirstOrDefault();

        if (cardModel != null)
        {
            ApplyEnchantmentByType(cardModel, optionDef.EnchantmentType);
        }

        var description = eventModel.L10NLookup(optionDef.DescriptionKey);
        SetEventFinished(eventModel, description);
        await Task.CompletedTask;
    }

    private static EnchantmentModel GetEnchantmentByType(Type enchantmentType)
    {
        var method = typeof(ModelDb).GetMethod("Enchantment")?.MakeGenericMethod(enchantmentType);
        return method?.Invoke(null, null) as EnchantmentModel 
            ?? throw new InvalidOperationException($"Failed to get enchantment of type {enchantmentType.Name}");
    }

    private static void ApplyEnchantmentByType(CardModel card, Type enchantmentType)
    {
        var enchantment = GetEnchantmentByType(enchantmentType).ToMutable();
        CardCmd.Enchant(enchantment, card, _enchantAmount);

        var vfx = NCardEnchantVfx.Create(card);
        if (vfx != null)
        {
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
    }

    private static void SetEventFinished(EventModel eventModel, LocString description)
    {
        var method = typeof(EventModel).GetMethod("SetEventFinished",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(eventModel, [description]);
    }
}
