using YuWanCard.Core.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
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
        // 最终伤害 = BaseDamage + 每个 ERROR 的加成 × 日志中的 ERROR 数量。
        WithCalculatedDamage(
            ValueProp.Move,
            multiplierCalc: static (_, _) => CountTotalErrorsInLog(),
            baseVal: BaseDamage,
            extraVal: ErrorDamageBonus,
            extraUpgrade: ErrorDamageBonusUpgraded - ErrorDamageBonus);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        MainFile.Logger.Info($"BugPig: dealing {DynamicVars.CalculatedDamage} damage");

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, null)
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
