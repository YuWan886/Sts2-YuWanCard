using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Patches;
using System.Reflection;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;

namespace YuWanCard.Core.Registration;

/// <summary>
/// Scans the assembly for models with [Pool] attribute and registers them
/// with the game's ModHelper.AddModelToPool. Also provides a per-constructor
/// registration method.
/// </summary>
public static class ContentRegistry
{
    public static void RegisterAll(Assembly assembly)
    {
        int cardCount = 0, relicCount = 0, potionCount = 0, otherCount = 0, eventCount = 0;

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract) continue;

            var poolAttr = type.GetCustomAttribute<PoolAttribute>();
            if (poolAttr != null)
            {
                ModHelper.AddModelToPool(poolAttr.PoolType, type);

                if (typeof(CardModel).IsAssignableFrom(type))
                    cardCount++;
                else if (typeof(RelicModel).IsAssignableFrom(type))
                    relicCount++;
                else if (typeof(PotionModel).IsAssignableFrom(type))
                    potionCount++;
                else
                    otherCount++;
                continue;
            }

            if (typeof(EventModel).IsAssignableFrom(type) && !typeof(AncientEventModel).IsAssignableFrom(type))
            {
                var instance = (EventModel)Activator.CreateInstance(type)!;
                CustomEventRegistry.Register(instance);
                eventCount++;
            }
        }

        MainFile.Logger.Info(
            $"ContentRegistry: registered {cardCount} cards, {relicCount} relics, {potionCount} potions, {eventCount} events, {otherCount} other models");
    }

    /// <summary>
    /// Per-constructor registration.
    /// Called from YuWanCardModel, YuWanRelicModel, etc. constructors.
    /// </summary>
    public static void AddModel(Type modelType)
    {
        var poolAttr = modelType.GetCustomAttribute<PoolAttribute>();
        if (poolAttr != null)
            ModHelper.AddModelToPool(poolAttr.PoolType, modelType);
    }
}
