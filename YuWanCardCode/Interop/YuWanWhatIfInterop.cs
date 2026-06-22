using MegaCrit.Sts2.Core.Models;
using YuWanCard.Cards;
using YuWanCard.Relics;

namespace YuWanCard.WhatIfRelicsCode.Interop;

public static class YuWanWhatIfInterop
{
    public static bool IsAvailable() => true;

    public static string[] GetRegisteredWhatIfRelicTypeNames() => [];

    public static string[] GetSupplementalWhatIfRelicTypeNames() =>
    [
        typeof(Heartsteel).FullName!,
        typeof(TenYearBamboo).FullName!,
        typeof(TriplePlay).FullName!,
        typeof(ArrogantPig).FullName!,
        typeof(JealousPig).FullName!,
        typeof(FuriousPig).FullName!,
        typeof(LazyPig).FullName!,
        typeof(GreedyPig).FullName!,
        typeof(GluttonousPig).FullName!,
        typeof(LustfulPig).FullName!
    ];

    public static string? GetShaCardEntry() => ModelDb.GetId<Sha>().Entry;

    public static string? GetSadArmyWinCardEntry() => ModelDb.GetId<SadArmyWin>().Entry;

    public static string? GetHeartsteelRelicEntry() => ModelDb.GetId<Heartsteel>().Entry;

    public static string? GetTenYearBambooRelicEntry() => ModelDb.GetId<TenYearBamboo>().Entry;

    public static string? GetTriplePlayRelicEntry() => ModelDb.GetId<TriplePlay>().Entry;

    public static string[] GetSeriesRelicEntries() =>
    [
        ModelDb.GetId<ArrogantPig>().Entry,
        ModelDb.GetId<JealousPig>().Entry,
        ModelDb.GetId<FuriousPig>().Entry,
        ModelDb.GetId<LazyPig>().Entry,
        ModelDb.GetId<GreedyPig>().Entry,
        ModelDb.GetId<GluttonousPig>().Entry,
        ModelDb.GetId<LustfulPig>().Entry
    ];
}
