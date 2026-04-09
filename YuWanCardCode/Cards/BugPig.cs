using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using YuWanCard.Characters;
using YuWanCard.Utils;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class BugPig : YuWanCardModel
{
    private const int BaseDamage = 7;
    private const int ErrorDamageBonus = 3;
    private const int ErrorDamageBonusUpgraded = 5;

    private static int _cachedErrorCount = -1;
    private static int _initialErrorCount = -1;

    [SavedProperty]
    private int YuWanCard_CalculatedDamageBonus { get; set; } = -1;

    public static void ResetErrorCount()
    {
        _cachedErrorCount = -1;
        _initialErrorCount = -1;
    }

    public static void CaptureInitialErrorCount()
    {
        if (_initialErrorCount < 0)
        {
            _initialErrorCount = CountTotalErrorsInLog();
            _cachedErrorCount = 0;
        }
    }

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
            int damageBonus = GetDamageBonus();
            int totalDamage = BaseDamage + damageBonus;

            await DamageCmd.Attack(totalDamage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        if (!TestMode.IsOn)
        {
            VfxUtils.PlayCentered("res://YuWanCard/scenes/vfx/vfx_glitch.tscn");
        }
    }

    private int GetDamageBonus()
    {
        if (YuWanCard_CalculatedDamageBonus >= 0)
        {
            return YuWanCard_CalculatedDamageBonus;
        }

        int damageBonus;

        if (IsMultiplayerMode())
        {
            damageBonus = CalculateDeterministicDamageBonus();
        }
        else
        {
            CaptureInitialErrorCount();
            int newErrors = CountNewErrorsInLog();
            damageBonus = IsUpgraded ? newErrors * ErrorDamageBonusUpgraded : newErrors * ErrorDamageBonus;
        }

        YuWanCard_CalculatedDamageBonus = damageBonus;
        return damageBonus;
    }

    private bool IsMultiplayerMode()
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null)
        {
            return false;
        }

        return netService.Type != NetGameType.Singleplayer;
    }

    private int CalculateDeterministicDamageBonus()
    {
        if (CombatState == null)
        {
            return 0;
        }

        int deterministicValue = 0;

        deterministicValue += CombatState.RoundNumber;

        deterministicValue += CombatState.Creatures.Count(c => !c.IsPlayer);

        if (Owner?.Creature != null)
        {
            var hand = PileType.Hand.GetPile(Owner);
            if (hand != null)
            {
                deterministicValue += hand.Cards.Count;
            }
        }

        deterministicValue %= 10;

        return IsUpgraded ? deterministicValue * ErrorDamageBonusUpgraded : deterministicValue * ErrorDamageBonus;
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

    public static int CountNewErrorsInLog()
    {
        if (_cachedErrorCount >= 0)
        {
            return _cachedErrorCount;
        }

        int totalErrors = CountTotalErrorsInLog();
        int newErrors = Math.Max(0, totalErrors - _initialErrorCount);
        _cachedErrorCount = newErrors;

        return newErrors;
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
        if (card is BugPig bugPig)
        {
            int damageBonus;
            if (bugPig.HasSynchronizedDamage())
            {
                damageBonus = bugPig.GetSynchronizedDamageBonus();
            }
            else
            {
                var netService = RunManager.Instance?.NetService;
                bool isMultiplayer = netService != null && netService.Type != NetGameType.Singleplayer;

                if (isMultiplayer)
                {
                    damageBonus = 0;
                }
                else
                {
                    BugPig.CaptureInitialErrorCount();
                    int errorCount = BugPig.CountNewErrorsInLog();
                    bool isUpgraded = card.IsUpgraded;
                    damageBonus = isUpgraded ? errorCount * errorBonusUpgraded : errorCount * errorBonus;
                }
            }

            decimal totalDamage = _baseDamage + damageBonus;
            BaseValue = totalDamage;
            PreviewValue = totalDamage;
        }
        else
        {
            var netService = RunManager.Instance?.NetService;
            bool isMultiplayer = netService != null && netService.Type != NetGameType.Singleplayer;

            int damageBonus = 0;
            if (!isMultiplayer)
            {
                BugPig.CaptureInitialErrorCount();
                int errorCount = BugPig.CountNewErrorsInLog();
                bool isUpgraded = card.IsUpgraded;
                damageBonus = isUpgraded ? errorCount * errorBonusUpgraded : errorCount * errorBonus;
            }

            decimal totalDamage = _baseDamage + damageBonus;
            BaseValue = totalDamage;
            PreviewValue = totalDamage;
        }
    }
}
