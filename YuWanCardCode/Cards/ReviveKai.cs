using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class ReviveKai : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public ReviveKai() : base(
        baseCost: 4,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.None)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var deadPlayers = CombatState!.PlayerCreatures
            .Where(c => c.IsPlayer && c.IsDead)
            .ToList();

        if (deadPlayers.Count == 0)
        {
            MainFile.Logger.Warn("没有已死亡的队友，无法使用复活卡");
            return;
        }

        Creature? targetCreature;
        if (deadPlayers.Count == 1)
        {
            targetCreature = deadPlayers[0];
        }
        else
        {
            targetCreature = await SelectDeadPlayer(choiceContext, deadPlayers);
            if (targetCreature == null)
            {
                MainFile.Logger.Warn("未选择要复活的玩家");
                return;
            }
        }

        decimal healAmount = IsUpgraded
            ? targetCreature.MaxHp
            : targetCreature.MaxHp / 2m;

        await CreatureCmd.Heal(targetCreature, healAmount);

        var targetPlayer = targetCreature.Player;
        if (targetPlayer != null)
        {
            await RestorePlayerDeck(choiceContext, targetPlayer);
        }
    }

    private async Task RestorePlayerDeck(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState == null) return;

        var cardsToAdd = new List<CardModel>();
        foreach (var deckCard in player.Deck.Cards)
        {
            var combatCard = CombatState!.CloneCard(deckCard);
            combatCard.DeckVersion = deckCard;
            cardsToAdd.Add(combatCard);
        }

        if (cardsToAdd.Count > 0)
        {
            await CardPileCmd.Add((IEnumerable<MegaCrit.Sts2.Core.Models.CardModel>)cardsToAdd, PileType.Draw, CardPilePosition.Bottom, this, skipVisuals: true);
            player.PlayerCombatState.DrawPile.RandomizeOrderInternal(
                player,
                player.RunState.Rng.Shuffle,
                CombatState!
            );
        }
    }

    private async Task<Creature?> SelectDeadPlayer(PlayerChoiceContext choiceContext, List<Creature> deadPlayers)
    {
        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(Owner);
        await choiceContext.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);

        int selectedIndex;
        if (LocalContext.IsMe(Owner))
        {
            selectedIndex = await ShowDeadPlayerSelection(deadPlayers);
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                Owner,
                choiceId,
                PlayerChoiceResult.FromIndex(selectedIndex)
            );
        }
        else
        {
            selectedIndex = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(Owner, choiceId)).AsIndex();
        }

        await choiceContext.SignalPlayerChoiceEnded();

        if (selectedIndex < 0 || selectedIndex >= deadPlayers.Count)
        {
            return null;
        }

        return deadPlayers[selectedIndex];
    }

    private async Task<int> ShowDeadPlayerSelection(List<Creature> deadPlayers)
    {
        var targetManager = NTargetManager.Instance;
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature);
        var startPosition = creatureNode?.GlobalPosition ?? Vector2.Zero;

        targetManager.StartTargeting(
            TargetType.AnyPlayer,
            startPosition,
            TargetMode.ClickMouseToTarget,
            () => false,
            AllowTargetingDeadPlayer
        );

        var node = await targetManager.SelectionFinished();

        for (int i = 0; i < deadPlayers.Count; i++)
        {
            var deadPlayer = deadPlayers[i];
            if (node is NCreature nCreature && nCreature.Entity == deadPlayer)
            {
                return i;
            }
            if (node is NMultiplayerPlayerState nPlayerState && nPlayerState.Player.Creature == deadPlayer)
            {
                return i;
            }
        }

        return -1;
    }

    private bool AllowTargetingDeadPlayer(Node node)
    {
        if (node is NCreature nCreature)
        {
            return nCreature.Entity.IsPlayer && nCreature.Entity.IsDead;
        }
        if (node is NMultiplayerPlayerState nPlayerState)
        {
            return nPlayerState.Player.Creature.IsPlayer && nPlayerState.Player.Creature.IsDead;
        }
        return false;
    }
}
