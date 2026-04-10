using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Monsters;
using YuWanCard.Powers;

namespace YuWanCard.Utils;

public static class PetManager
{
    private const float PetVerticalSpacing = 80f;
    private const float PetBaseOffsetY = 30f;
    private const float DefectedPetBaseOffsetY = 40f;

    public static async Task<Creature?> SummonPigMinion(Player owner, int upgradeLevel = 0)
    {
        if (owner == null) return null;

        var existingPig = FindPetByType<PigMinion>(owner.Creature);
        
        if (existingPig != null && existingPig.IsAlive)
        {
            return existingPig;
        }

        SfxCmd.Play("event:/sfx/characters/necrobinder/necrobinder_summon");

        bool isReviving = existingPig != null && existingPig.IsDead;
        Creature? pig;

        if (isReviving && existingPig != null)
        {
            pig = existingPig;
            owner.PlayerCombatState?.AddPetInternal(existingPig);
        }
        else
        {
            var deadPig = FindDeadPetByType<PigMinion>(owner.Creature.CombatState);
            if (deadPig != null)
            {
                owner.Creature.CombatState?.RemoveCreature(deadPig, unattach: true);
            }
            pig = await PlayerCmd.AddPet<PigMinion>(owner);
        }

        if (pig != null)
        {
            await SetupPigMinion(pig, owner.Creature, upgradeLevel, isReviving);
            PositionAllPets(owner.Creature);
            
            if (isReviving)
            {
                PlayReviveAnimation(pig);
            }
        }

        return pig;
    }

    public static async Task<Creature?> DefectEnemyToPet(Player owner, Creature target)
    {
        if (target.Monster == null || owner == null || owner.Creature.CombatState == null) return null;

        MainFile.Logger.Info($"PetManager: Defecting {target.Name} to player side");

        int currentHp = target.CurrentHp;
        int maxHp = target.MaxHp;
        var monsterModel = target.Monster;

        var oldCreatureNode = NCombatRoom.Instance?.GetCreatureNode(target);

        owner.Creature.CombatState.RemoveCreature(target, unattach: true);

        if (oldCreatureNode != null)
        {
            NCombatRoom.Instance?.RemoveCreatureNode(oldCreatureNode);
            oldCreatureNode.QueueFree();
        }

        var canonicalMonster = ModelDb.GetById<MonsterModel>(monsterModel.Id);

        var defectedCreature = owner.Creature.CombatState.CreateCreature(
            canonicalMonster.ToMutable(),
            CombatSide.Player,
            null
        );

        owner.Creature.CombatState.AddCreature(defectedCreature);
        owner.PlayerCombatState?.AddPetInternal(defectedCreature);

        await CreatureCmd.SetMaxHp(defectedCreature, maxHp);
        if (currentHp > 0)
        {
            await CreatureCmd.SetCurrentHp(defectedCreature, Math.Min(currentHp, maxHp));
        }

        await PowerCmd.Apply<PigDefectionPower>(defectedCreature, 1, owner.Creature, null);

        NCombatRoom.Instance?.AddCreature(defectedCreature);
        await CombatManager.Instance.AfterCreatureAdded(defectedCreature);
        await Hook.AfterCreatureAddedToCombat(owner.Creature.CombatState, defectedCreature);

        PositionAllPets(owner.Creature, true);

        SfxCmd.Play("event:/sfx/characters/necrobinder/necrobinder_summon");

        MainFile.Logger.Info($"PetManager: Successfully defected {target.Name} to player side");

        return defectedCreature;
    }

    public static void PositionAllPets(Creature owner, bool isDefected = false)
    {
        if (owner == null) return;

        var ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (ownerNode == null) return;

        var pets = owner.Pets.ToList();
        int petCount = pets.Count;

        float baseOffsetX = ownerNode.Hitbox.Size.X * 0.5f + (isDefected ? 260f : 190f);
        float baseOffsetY = isDefected ? DefectedPetBaseOffsetY : PetBaseOffsetY;

        for (int i = 0; i < petCount; i++)
        {
            var pet = pets[i];
            var petNode = NCombatRoom.Instance?.GetCreatureNode(pet);
            if (petNode == null) continue;

            float yOffset = baseOffsetY - i * PetVerticalSpacing;
            Vector2 offset = new Vector2(baseOffsetX, yOffset);
            petNode.Position = ownerNode.Position + offset;

            bool hasDefectionPower = pet.HasPower<PigDefectionPower>();
            if (hasDefectionPower)
            {
                var body = petNode.Body;
                if (body != null)
                {
                    var currentScale = body.Scale;
                    if (currentScale.X > 0)
                    {
                        body.Scale = new Vector2(-currentScale.X, currentScale.Y);
                    }
                }
            }

            petNode.ToggleIsInteractable(true);
        }
    }

    public static void PositionPet(Creature owner, Creature pet, int upgradeLevel = 0, bool isDefected = false)
    {
        if (owner == null || pet == null) return;

        var petNode = NCombatRoom.Instance?.GetCreatureNode(pet);
        var ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (petNode == null || ownerNode == null) return;

        float scale = 0.5f + upgradeLevel * 0.15f;
        petNode.SetDefaultScaleTo(scale, 0f);

        PositionAllPets(owner, isDefected);
    }

    public static async Task UpgradePigMinion(Creature pig, int levels, Creature? owner = null)
    {
        if (pig == null || levels <= 0) return;

        int bonusHp = levels * 5;
        int bonusStrength = levels;

        await CreatureCmd.GainMaxHp(pig, bonusHp);
        await CreatureCmd.Heal(pig, bonusHp);

        if (bonusStrength > 0 && owner != null)
        {
            await PowerCmd.Apply<StrengthPower>(pig, bonusStrength, owner, null);
        }
    }

    public static async Task KillPet(Creature pet, bool animate = false)
    {
        if (pet == null || pet.IsDead) return;

        await CreatureCmd.Kill(pet, animate);
    }

    public static Creature? FindPetByType<T>(Creature owner) where T : MonsterModel
    {
        if (owner == null) return null;

        foreach (var pet in owner.Pets)
        {
            if (pet.Monster is T)
            {
                return pet;
            }
        }
        return null;
    }

    public static Creature? FindDeadPetByType<T>(CombatState? combatState) where T : MonsterModel
    {
        if (combatState == null) return null;

        foreach (var ally in combatState.Allies)
        {
            if (ally.Monster is T && ally.IsDead)
            {
                return ally;
            }
        }
        return null;
    }

    private static async Task SetupPigMinion(Creature pig, Creature owner, int upgradeLevel, bool isReviving = false)
    {
        if (pig == null || owner == null) return;

        int ownerMaxHp = owner.MaxHp;
        int pigHp = ownerMaxHp / 5;
        if (pigHp < 1) pigHp = 1;

        if (isReviving)
        {
            await CreatureCmd.Heal(pig, pigHp, true);
        }
        else
        {
            await CreatureCmd.SetMaxHp(pig, pigHp);
            await CreatureCmd.Heal(pig, pigHp);
            await PowerCmd.Apply<PigMinionPower>(pig, 1, null, null);
        }

        if (upgradeLevel > 0)
        {
            await UpgradePigMinion(pig, upgradeLevel, owner);
        }
    }

    private static void PlayReviveAnimation(Creature pet)
    {
        var petNode = NCombatRoom.Instance?.GetCreatureNode(pet);
        if (petNode == null) return;

        petNode.SetAnimationTrigger("Idle");
        petNode.AnimEnableUi();
        petNode.Modulate = Colors.Transparent;
        Tween tween = petNode.CreateTween();
        tween.TweenProperty(petNode, "modulate", Colors.White, 0.35f).SetDelay(0.1f);
    }
}
