using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using YuWanCard.Characters;
using YuWanCard.GameActions;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class BugPig : YuWanCardModel
{
    private const int BaseDamage = 7;
    private const int ErrorDamageBonus = 3;
    private const int ErrorDamageBonusUpgraded = 5;

    [SavedProperty]
    private int YuWanCard_CalculatedDamageBonus { get; set; } = -1;

    public BugPig() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithVars(new BugPigDamageVar(BaseDamage, ErrorDamageBonus, ErrorDamageBonusUpgraded));
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            var netService = RunManager.Instance?.NetService;
            bool isMultiplayer = netService != null && netService.Type != NetGameType.Singleplayer && netService.Type != NetGameType.Replay;
            bool isHost = netService?.Type == NetGameType.Host;

            if (isMultiplayer && !isHost)
            {
                MainFile.Logger.Debug($"BugPig: Client skipping OnPlay, waiting for Host to sync damage");
                return;
            }

            if (isMultiplayer)
            {
                int errorCount = CountTotalErrorsInLog();
                int damageBonus = IsUpgraded ? errorCount * ErrorDamageBonusUpgraded : errorCount * ErrorDamageBonus;
                int totalDamage = BaseDamage + damageBonus;
                YuWanCard_CalculatedDamageBonus = damageBonus;

                int targetIndex = -1;
                var combatState = Owner.Creature?.CombatState;
                if (combatState != null)
                {
                    int index = 0;
                    foreach (var creature in combatState.Creatures)
                    {
                        if (creature == cardPlay.Target)
                        {
                            targetIndex = index;
                            break;
                        }
                        index++;
                    }
                }

                MainFile.Logger.Info($"BugPig: Host calculated total error count: {errorCount}, damage bonus: {damageBonus}, total damage: {totalDamage}");

                var action = new BugPigAction(Owner, targetIndex, totalDamage);
                RunManager.Instance?.ActionQueueSynchronizer?.RequestEnqueue(action);
            }
            else
            {
                int errorCount = CountTotalErrorsInLog();
                int damageBonus = IsUpgraded ? errorCount * ErrorDamageBonusUpgraded : errorCount * ErrorDamageBonus;
                int totalDamage = BaseDamage + damageBonus;

                MainFile.Logger.Info($"BugPig: Singleplayer error count: {errorCount}, damage bonus: {damageBonus}, total damage: {totalDamage}");

                await DamageCmd.Attack(totalDamage)
                    .FromCard(this)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);

                if (!TestMode.IsOn)
                {
                    VfxUtils.PlayCentered("res://YuWanCard/scenes/vfx/vfx_glitch.tscn");
                }
            }
        }
    }

    public static int CountTotalErrorsInLog()
    {
        try
        {
            string logPath = Path.Combine(OS.GetUserDataDir(), "logs", "godot.log");
            if (!File.Exists(logPath))
            {
                return 0;
            }

            int errorCount = 0;

            using (var fileStream = new FileStream(logPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line.Contains("[YuWanCard] BugPig:") || line.Contains("BugPigAction:"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[ERROR]", StringComparison.Ordinal) ||
                        line.StartsWith("ERROR:", StringComparison.Ordinal))
                    {
                        errorCount++;
                    }
                }
            }

            return errorCount;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"BugPig: Error reading log file: {ex.Message}");
            return 0;
        }
    }

    public int GetSynchronizedDamageBonus()
    {
        return YuWanCard_CalculatedDamageBonus >= 0 ? YuWanCard_CalculatedDamageBonus : 0;
    }

    public bool HasSynchronizedDamage()
    {
        return YuWanCard_CalculatedDamageBonus >= 0;
    }
}

public class BugPigDamageVar(int baseDamage, int errorBonus, int errorBonusUpgraded) : DynamicVar(Key, baseDamage)
{
    public const string Key = "BugPigDamage";
    private readonly int _baseDamage = baseDamage;

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        var netService = RunManager.Instance?.NetService;
        bool isMultiplayer = netService != null && netService.Type != NetGameType.Singleplayer && netService.Type != NetGameType.Replay;
        bool isHost = netService?.Type == NetGameType.Host;

        int damageBonus = 0;
        
        if (card is BugPig bugPig && bugPig.HasSynchronizedDamage())
        {
            damageBonus = bugPig.GetSynchronizedDamageBonus();
        }
        else if (!isMultiplayer || isHost)
        {
            int errorCount = BugPig.CountTotalErrorsInLog();
            bool isUpgraded = card.IsUpgraded;
            damageBonus = isUpgraded ? errorCount * errorBonusUpgraded : errorCount * errorBonus;
        }

        decimal totalDamage = _baseDamage + damageBonus;
        BaseValue = totalDamage;
        PreviewValue = totalDamage;
    }
}
