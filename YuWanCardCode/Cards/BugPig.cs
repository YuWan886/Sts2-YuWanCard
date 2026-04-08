using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class BugPig : YuWanCardModel
{
    private const int BaseDamage = 7;
    private const int ErrorDamageBonus = 3;
    private const int ErrorDamageBonusUpgraded = 5;

    private static int _cachedErrorCount = -1;

    public static void ResetErrorCount()
    {
        _cachedErrorCount = -1;
        MainFile.Logger.Debug("BugPig: Error count cache reset");
    }

    public BugPig() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithVars(new BugPigDamageVar(BaseDamage, ErrorDamageBonus, ErrorDamageBonusUpgraded));
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            int errorCount = CountErrorsInLog();
            int damageBonus = IsUpgraded ? errorCount * ErrorDamageBonusUpgraded : errorCount * ErrorDamageBonus;
            int totalDamage = BaseDamage + damageBonus;

            MainFile.Logger.Info($"BugPig: Error count = {errorCount}, Damage bonus = {damageBonus}, Total damage = {totalDamage}, IsUpgraded = {IsUpgraded}");

            await DamageCmd.Attack(totalDamage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    public static int CountErrorsInLog()
    {
        if (_cachedErrorCount >= 0)
        {
            MainFile.Logger.Debug($"BugPig: Using cached error count = {_cachedErrorCount}");
            return _cachedErrorCount;
        }

        try
        {
            string logPath = Path.Combine(OS.GetUserDataDir(), "logs", "godot.log");
            if (!File.Exists(logPath))
            {
                MainFile.Logger.Warn($"BugPig: Log file not found at {logPath}");
                _cachedErrorCount = 0;
                return 0;
            }

            int errorCount = 0;
            
            using (var fileStream = new FileStream(logPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line.Contains("[YuWanCard] BugPig:"))
                    {
                        continue;
                    }
                    
                    if (line.StartsWith("[ERROR]", StringComparison.Ordinal) ||
                        line.StartsWith("ERROR:", StringComparison.Ordinal))
                    {
                        errorCount++;
                        MainFile.Logger.Debug($"BugPig: Found error line: {line}");
                    }
                }
            }

            _cachedErrorCount = errorCount;
            MainFile.Logger.Info($"BugPig: Total errors found in log: {errorCount} (cached for this combat)");
            return errorCount;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"BugPig: Error reading log file: {ex.Message}");
            _cachedErrorCount = 0;
            return 0;
        }
    }
}

public class BugPigDamageVar(int baseDamage, int errorBonus, int errorBonusUpgraded) : DynamicVar(Key, baseDamage)
{
    public const string Key = "BugPigDamage";
    private readonly int _baseDamage = baseDamage;

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        int errorCount = BugPig.CountErrorsInLog();
        bool isUpgraded = card.IsUpgraded;
        int damageBonus = isUpgraded ? errorCount * errorBonusUpgraded : errorCount * errorBonus;
        decimal totalDamage = _baseDamage + damageBonus;
        BaseValue = totalDamage;
        PreviewValue = totalDamage;
        MainFile.Logger.Info($"BugPigDamageVar: BaseValue = {BaseValue}, PreviewValue = {PreviewValue}");
    }
}
