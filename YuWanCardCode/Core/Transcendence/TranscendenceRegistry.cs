using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Core.Transcendence;

/// <summary>
/// Shared starter-card transcendence registry used by ArchaicTooth flows.
/// Supports both explicit registration and self-describing cards via ITranscendenceCard.
/// </summary>
public static class TranscendenceRegistry
{
    private static readonly Dictionary<ModelId, ModelId> RegisteredStarterToAncient = new();
    private static readonly Dictionary<ModelId, ModelId> RegisteredAncientToStarter = new();
    private static readonly HashSet<ModelId> RegisteredAncientIds = new();
    private static readonly Dictionary<ModelId, ModelId> RegisteredDustyTomeCharacterToAncient = new();
    private static readonly Dictionary<ModelId, ModelId> InterfaceStarterToAncient = new();
    private static readonly Dictionary<ModelId, ModelId> InterfaceAncientToStarter = new();
    private static readonly HashSet<Assembly> ScannedTranscendenceAssemblies = new();
    private static readonly HashSet<Type> PendingTranscendenceInterfaceTypes = new();
    private static readonly HashSet<Assembly> ScannedDustyTomeAssemblies = new();
    private static readonly HashSet<Type> PendingDustyTomeInterfaceTypes = new();
    private static bool _defaultsRegistered;

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

    public static bool RegisterDustyTome<TCharacter, TAncient>()
        where TCharacter : CharacterModel
        where TAncient : CardModel
    {
        return RegisterDustyTome(ModelDb.GetId<TCharacter>(), ModelDb.GetId<TAncient>());
    }

    public static bool RegisterDustyTome(ModelId characterId, ModelId ancientCardId)
    {
        if (RegisteredDustyTomeCharacterToAncient.TryGetValue(characterId, out var existingAncient))
        {
            if (existingAncient == ancientCardId)
            {
                return false;
            }

            MainFile.Logger.Warn(
                $"[TranscendenceRegistry] DustyTome mapping for {characterId.Entry}: " +
                $"{existingAncient.Entry} already registered, ignoring {ancientCardId.Entry}");
            return false;
        }

        RegisteredDustyTomeCharacterToAncient[characterId] = ancientCardId;
        return true;
    }

    public static bool IsStarterCard(CardModel card)
    {
        EnsureDefaultsRegistered();
        return TryGetAncientCard(card, out _);
    }

    public static bool IsAncientCard(CardModel card)
    {
        EnsureDefaultsRegistered();
        EnsureInterfaceMappings();
        return RegisteredAncientIds.Contains(card.Id)
            || InterfaceAncientToStarter.ContainsKey(card.Id);
    }

    public static bool TryGetAncientCard(CardModel starterCard, out CardModel ancientCard)
    {
        EnsureDefaultsRegistered();

        if (TryGetInterfaceAncientCard(starterCard, out ancientCard))
        {
            return true;
        }

        if (InterfaceStarterToAncient.TryGetValue(starterCard.Id, out var interfaceAncientId))
        {
            ancientCard = ModelDb.GetById<CardModel>(interfaceAncientId);
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
        EnsureDefaultsRegistered();
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
        EnsureDefaultsRegistered();
        EnsureInterfaceMappings();

        return RegisteredAncientIds
            .Concat(InterfaceAncientToStarter.Keys)
            .Distinct()
            .Select(id => ModelDb.GetById<CardModel>(id))
            .ToList();
    }

    public static bool TryGetDustyTomeAncientCard(Player player, out CardModel ancientCard)
    {
        EnsureDefaultsRegistered();
        EnsureDustyTomeMappings();

        if (RegisteredDustyTomeCharacterToAncient.TryGetValue(player.Character.Id, out var ancientCardId))
        {
            ancientCard = ModelDb.GetById<CardModel>(ancientCardId);
            return true;
        }

        ancientCard = null!;
        return false;
    }

    public static CardModel? CreateTransformedCard(CardModel starterCard)
    {
        EnsureDefaultsRegistered();
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
            if (enchantmentModel.CanEnchant(transformedCard))
            {
                CardCmd.Enchant(enchantmentModel, transformedCard, enchantmentModel.Amount);
            }
        }

        return transformedCard;
    }

    private static void EnsureDefaultsRegistered()
    {
        RegisterDefaults();
    }

    private static void EnsureInterfaceMappings()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!ScannedTranscendenceAssemblies.Add(assembly))
            {
                continue;
            }

            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type == null || type.IsAbstract || !typeof(ITranscendenceCard).IsAssignableFrom(type) ||
                    !typeof(CardModel).IsAssignableFrom(type))
                {
                    continue;
                }

                PendingTranscendenceInterfaceTypes.Add(type);
            }
        }

        if (PendingTranscendenceInterfaceTypes.Count == 0)
        {
            return;
        }

        foreach (var type in PendingTranscendenceInterfaceTypes.ToArray())
        {
            if (TryRegisterInterfaceMapping(type))
            {
                PendingTranscendenceInterfaceTypes.Remove(type);
            }
        }
    }

    private static void EnsureDustyTomeMappings()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!ScannedDustyTomeAssemblies.Add(assembly))
            {
                continue;
            }

            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type == null || type.IsAbstract || !typeof(IDustyTomeCard).IsAssignableFrom(type) ||
                    !typeof(CardModel).IsAssignableFrom(type))
                {
                    continue;
                }

                PendingDustyTomeInterfaceTypes.Add(type);
            }
        }

        if (PendingDustyTomeInterfaceTypes.Count == 0)
        {
            return;
        }

        foreach (var type in PendingDustyTomeInterfaceTypes.ToArray())
        {
            if (TryRegisterDustyTomeInterfaceMapping(type))
            {
                PendingDustyTomeInterfaceTypes.Remove(type);
            }
        }
    }

    private static bool TryRegisterInterfaceMapping(Type starterType)
    {
        try
        {
            var starterId = ModelDb.GetId(starterType);
            var starterCard = ModelDb.GetById<CardModel>(starterId);
            return TryGetInterfaceAncientCard(starterCard, out _);
        }
        catch
        {
            // Retry on a later call if the type has not finished registering yet.
            return false;
        }
    }

    private static bool TryGetInterfaceAncientCard(CardModel starterCard, out CardModel ancientCard)
    {
        if (starterCard is not ITranscendenceCard transcendenceCard)
        {
            ancientCard = null!;
            return false;
        }

        try
        {
            ancientCard = transcendenceCard.GetTranscendenceTransformedCard();
            if (ancientCard == null)
            {
                ancientCard = null!;
                return false;
            }

            InterfaceStarterToAncient[starterCard.Id] = ancientCard.Id;
            InterfaceAncientToStarter[ancientCard.Id] = starterCard.Id;
            return true;
        }
        catch
        {
            ancientCard = null!;
            return false;
        }
    }

    private static bool TryRegisterDustyTomeInterfaceMapping(Type ancientType)
    {
        try
        {
            var ancientCardId = ModelDb.GetId(ancientType);
            var ancientCard = ModelDb.GetById<CardModel>(ancientCardId);
            return TryGetInterfaceDustyTomeCharacter(ancientCard, out _);
        }
        catch
        {
            // Retry on a later call if the type has not finished registering yet.
            return false;
        }
    }

    private static bool TryGetInterfaceDustyTomeCharacter(CardModel ancientCard, out CharacterModel character)
    {
        if (ancientCard is not IDustyTomeCard dustyTomeCard)
        {
            character = null!;
            return false;
        }

        try
        {
            character = dustyTomeCard.GetDustyTomeCharacter();
            if (character == null)
            {
                character = null!;
                return false;
            }

            RegisterDustyTome(character.Id, ancientCard.Id);
            return true;
        }
        catch
        {
            character = null!;
            return false;
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
