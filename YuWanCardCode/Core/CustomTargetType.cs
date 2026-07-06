using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Monsters;
using YuWanCard.Powers;

namespace YuWanCard.Core;

public static class CustomTargetType
{
    private static readonly DynamicEnumValueMinter<TargetType> TargetTypeMinter = new();

    public static TargetType AllPlayers { get; } = Mint("all_players");

    public static TargetType Everyone { get; } = Mint("everyone");

    public static TargetType Anyone { get; } = Mint("anyone");

    public static TargetType AnyOtherPlayer { get; } = Mint("any_other_player");

    public static TargetType AnyFriendly { get; } = Mint("any_friendly");

    public static TargetType AnyPigMinion { get; } = Mint("any_pig_minion");

    public static TargetType AnyYouArePigTarget { get; } = Mint("any_you_are_pig");

    public static TargetType AnyPigPawnTarget { get; } = Mint("any_pig_pawn_target");

    public static bool IsYuWanCustom(TargetType type)
    {
        return type == AllPlayers
               || type == Everyone
               || type == Anyone
               || type == AnyOtherPlayer
               || type == AnyFriendly
               || type == AnyPigMinion
               || type == AnyYouArePigTarget
               || type == AnyPigPawnTarget
               || CustomTargetTypeRegistry.IsYuWanCustom(type);
    }

    public static bool IsCustomSingleTargetType(TargetType type)
    {
        return type == Anyone
               || type == AnyOtherPlayer
               || type == AnyFriendly
               || type == AnyPigMinion
               || type == AnyYouArePigTarget
               || type == AnyPigPawnTarget
               || CustomTargetTypeRegistry.IsCustomSingleTargetType(type);
    }

    public static bool IsCustomMultiTargetType(TargetType type)
    {
        return type == AllPlayers
               || type == Everyone
               || CustomTargetTypeRegistry.IsCustomMultiTargetType(type);
    }

    public static bool IsAnyFriendlyTarget(Creature? target)
    {
        if (target is not { IsAlive: true })
        {
            return false;
        }

        if (IsAnyPigMinionTarget(target))
        {
            return true;
        }

        return target.IsPlayer && !target.IsPet;
    }

    public static bool IsAnyOtherPlayerTarget(CardModel? card, Creature? target)
    {
        return card?.Owner != null
               && target is { IsAlive: true, IsPlayer: true, IsPet: false, Player: not null }
               && target.Player != card.Owner;
    }

    public static bool IsAnyPigMinionTarget(Creature? target)
    {
        return target is { IsAlive: true, IsPet: true } && target.Monster is PigMinion;
    }

    public static bool IsAnyYouArePigTarget(Creature? target)
    {
        return target is { IsAlive: true, IsPet: false } && target.HasPower<YouArePigPower>();
    }

    public static bool IsAnyPigPawnTarget(Creature? target)
    {
        return IsAnyPigMinionTarget(target) || IsAnyYouArePigTarget(target);
    }

    private static TargetType Mint(string localStem)
    {
        var id = $"{MainFile.ModId.ToUpperInvariant()}_TARGETTYPE_{localStem.ToUpperInvariant()}";
        return TargetTypeMinter.Mint(id);
    }
}
