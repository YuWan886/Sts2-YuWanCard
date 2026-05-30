using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Core.Transcendence;

/// <summary>
/// Shared starter-card transcendence registry used by ArchaicTooth/DustyTome flows.
/// Supports both explicit registration and self-describing cards via ITranscendenceCard.
/// </summary>
public static class TranscendenceRegistry
{
    private static readonly Dictionary<ModelId, ModelId> RegisteredStarterToAncient = new();
    private static readonly Dictionary<ModelId, ModelId> RegisteredAncientToStarter = new();
    private static readonly HashSet<ModelId> RegisteredAncientIds = new();
    private static readonly Dictionary<ModelId, ModelId> InterfaceStarterToAncient = new();
    private static readonly Dictionary<ModelId, ModelId> InterfaceAncientToStarter = new();
    private static bool _defaultsRegistered;
    private static bool _interfaceMappingsInitialized;

    public static void RegisterDefaults()
    {
        if (_defaultsRegistered)
        {
            return;
        }

        _defaultsRegistered = true;

        Register(ModelDb.GetId<Bash>(), ModelDb.GetId<Break>());
        Register(ModelDb.GetId<Neutralize>(), ModelDb.GetId<Suppress>());
        Register(ModelDb.GetId<Unleash>(), ModelDb.GetId<Protector>());
        Register(ModelDb.GetId<FallingStar>(), ModelDb.GetId<MeteorShower>());
        Register(ModelDb.GetId<Dualcast>(), ModelDb.GetId<Quadcast>());
    }

    public static bool Register<TStarter, TAncient>()
        where TStarter : CardModel
        where TAncient : CardModel
    {
        return Register(ModelDb.GetId<TStarter>(), ModelDb.GetId<TAncient>());
    }

    public static bool Register(ModelId starterCardId, ModelId ancientCardId)
    {
        if (RegisteredStarterToAncient.TryGetValue(starterCardId, out var existingAncient))
        {
            if (existingAncient == ancientCardId)
            {
                return false;
            }

            MainFile.Logger.Warn(
                $"[TranscendenceRegistry] Conflicting starter mapping for {starterCardId.Entry}: " +
                $"{existingAncient.Entry} already registered, ignoring {ancientCardId.Entry}");
            return false;
        }

        if (RegisteredAncientToStarter.TryGetValue(ancientCardId, out var existingStarter) &&
            existingStarter != starterCardId)
        {
            MainFile.Logger.Warn(
                $"[TranscendenceRegistry] Ancient {ancientCardId.Entry} is already paired with {existingStarter.Entry}, " +
                $"ignoring {starterCardId.Entry}");
            return false;
        }

        RegisteredStarterToAncient[starterCardId] = ancientCardId;
        RegisteredAncientToStarter[ancientCardId] = starterCardId;
        RegisteredAncientIds.Add(ancientCardId);
        return true;
    }

    public static bool IsStarterCard(CardModel card)
    {
        return TryGetAncientCard(card, out _);
    }

    public static bool IsAncientCard(CardModel card)
    {
        EnsureInterfaceMappings();
        return RegisteredAncientIds.Contains(card.Id)
            || InterfaceAncientToStarter.ContainsKey(card.Id);
    }

    public static bool TryGetAncientCard(CardModel starterCard, out CardModel ancientCard)
    {
        EnsureInterfaceMappings();

        if (starterCard is ITranscendenceCard transcendenceCard)
        {
            ancientCard = transcendenceCard.GetTranscendenceTransformedCard();
            return true;
        }

        if (RegisteredStarterToAncient.TryGetValue(starterCard.Id, out var ancientCardId))
        {
            ancientCard = ModelDb.GetById<CardModel>(ancientCardId);
            return true;
        }

        ancientCard = null!;
        return false;
    }

    public static bool TryGetStarterCard(CardModel ancientCard, out CardModel starterCard)
    {
        EnsureInterfaceMappings();

        if (InterfaceAncientToStarter.TryGetValue(ancientCard.Id, out var interfaceStarterId))
        {
            starterCard = ModelDb.GetById<CardModel>(interfaceStarterId);
            return true;
        }

        if (RegisteredAncientToStarter.TryGetValue(ancientCard.Id, out var starterCardId))
        {
            starterCard = ModelDb.GetById<CardModel>(starterCardId);
            return true;
        }

        starterCard = null!;
        return false;
    }

    public static IReadOnlyCollection<CardModel> GetRegisteredAncientCards()
    {
        return RegisteredAncientIds.Select(id => ModelDb.GetById<CardModel>(id)).ToList();
    }

    public static CardModel? CreateTransformedCard(CardModel starterCard)
    {
        if (starterCard.Owner == null || !TryGetAncientCard(starterCard, out CardModel ancientCard))
        {
            return null;
        }

        CardModel transformedCard = starterCard.Owner.RunState.CreateCard(ancientCard, starterCard.Owner);
        if (starterCard.IsUpgraded)
        {
            CardCmd.Upgrade(transformedCard);
        }

        if (starterCard.Enchantment != null)
        {
            EnchantmentModel enchantmentModel = (EnchantmentModel)starterCard.Enchantment.MutableClone();
            CardCmd.Enchant(enchantmentModel, transformedCard, enchantmentModel.Amount);
        }

        return transformedCard;
    }

    private static void EnsureInterfaceMappings()
    {
        if (_interfaceMappingsInitialized)
        {
            return;
        }

        _interfaceMappingsInitialized = true;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type == null || type.IsAbstract || !typeof(ITranscendenceCard).IsAssignableFrom(type) ||
                    !typeof(CardModel).IsAssignableFrom(type))
                {
                    continue;
                }

                try
                {
                    var starterId = ModelDb.GetId(type);
                    var starterCard = ModelDb.GetById<CardModel>(starterId);
                    if (starterCard is not ITranscendenceCard transcendenceCard)
                    {
                        continue;
                    }

                    var ancientCard = transcendenceCard.GetTranscendenceTransformedCard();
                    if (ancientCard == null)
                    {
                        continue;
                    }

                    InterfaceStarterToAncient[starterId] = ancientCard.Id;
                    InterfaceAncientToStarter[ancientCard.Id] = starterId;
                }
                catch
                {
                    // Ignore types that are not fully registered in ModelDb.
                }
            }
        }
    }

    private static IEnumerable<Type?> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types;
        }
    }
}
