using YuWanCard.Core.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class BugPig : YuWanCardModel
{
    private const int BaseDamage = 7;
    private const int ErrorDamageBonus = 3;
    private const int ErrorDamageBonusUpgraded = 5;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.SingleplayerOnly;

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
        if (cardPlay.Target == null)
        {
            return;
        }

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
                    if (line.Contains("[YuWanCard] BugPig:"))
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
}

public class BugPigDamageVar(int baseDamage, int errorBonus, int errorBonusUpgraded) : DynamicVar(Key, baseDamage)
{
    public const string Key = "BugPigDamage";
    private readonly int _baseDamage = baseDamage;

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        int errorCount = BugPig.CountTotalErrorsInLog();
        bool isUpgraded = card.IsUpgraded;
        int damageBonus = isUpgraded ? errorCount * errorBonusUpgraded : errorCount * errorBonus;

        decimal totalDamage = _baseDamage + damageBonus;
        BaseValue = totalDamage;
        PreviewValue = totalDamage;
    }
}
