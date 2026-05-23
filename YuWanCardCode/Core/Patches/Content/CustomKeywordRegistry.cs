using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using YuWanCard.Core.Utils;

namespace YuWanCard.Core.Patches.Content;

public static class CustomKeywordRegistry
{
    private static readonly Dictionary<int, string> LocKeyPrefixes = [];
    internal static readonly List<CardKeyword> AdditionalBeforeKeywords = [];
    internal static readonly List<CardKeyword> AdditionalAfterKeywords = [];

    public static void RegisterKeyword(CardKeyword keyword, string locKey, AutoKeywordPosition position)
    {
        LocKeyPrefixes[(int)keyword] = locKey;
        switch (position)
        {
            case AutoKeywordPosition.Before:
                AdditionalBeforeKeywords.Add(keyword);
                break;
            case AutoKeywordPosition.After:
                AdditionalAfterKeywords.Add(keyword);
                break;
        }
    }

    public static bool TryGetLocKeyPrefix(CardKeyword keyword, out string prefix) =>
        LocKeyPrefixes.TryGetValue((int)keyword, out prefix!);

    private static string PascalToUpperSnake(string name)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                result.Append('_');
            result.Append(char.ToUpperInvariant(name[i]));
        }
        return result.ToString();
    }

    /// <summary>
    /// Scans an assembly for static fields with [CustomEnum] and initializes them.
    /// Called from ContentRegistry.RegisterAll.
    /// </summary>
    internal static void InitializeCustomEnumFields(Assembly assembly)
    {
        var cardKeywordMinter = new DynamicEnumValueMinter<CardKeyword>();
        var minterCache = new Dictionary<Type, object>();

        foreach (var type in AssemblyScanner.GetLoadableTypes(assembly))
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (field.GetCustomAttribute<CustomEnumAttribute>() == null)
                    continue;
                if (!field.FieldType.IsEnum)
                    continue;

                var assemblyName = assembly.GetName().Name ?? "UNKNOWN";
                var id = $"{assemblyName}.{field.DeclaringType!.FullName}.{field.Name}";

                if (field.FieldType == typeof(CardKeyword))
                {
                    var keyword = cardKeywordMinter.Mint(id);
                    field.SetValue(null, keyword);

                    var modPrefix = assemblyName.ToUpperInvariant() + "-";
                    var locKey = modPrefix + PascalToUpperSnake(field.Name);
                    var props = field.GetCustomAttribute<KeywordPropertiesAttribute>();
                    var position = props?.Position ?? AutoKeywordPosition.After;
                    RegisterKeyword(keyword, locKey, position);
                }
                else
                {
                    object minter;
                    if (!minterCache.TryGetValue(field.FieldType, out minter!))
                    {
                        var minterType = typeof(DynamicEnumValueMinter<>).MakeGenericType(field.FieldType);
                        minter = Activator.CreateInstance(minterType)!;
                        minterCache[field.FieldType] = minter;
                    }

                    var mintMethod = minter.GetType().GetMethod("Mint")!;
                    var value = mintMethod.Invoke(minter, [id]);
                    field.SetValue(null, value);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(CardKeywordOrder), MethodType.StaticConstructor)]
static class AutoKeywordTextPatch
{
    [HarmonyPostfix]
    static void Postfix(ref CardKeyword[] ___beforeDescription, ref CardKeyword[] ___afterDescription)
    {
        if (CustomKeywordRegistry.AdditionalBeforeKeywords.Count > 0)
            ___beforeDescription = [.. ___beforeDescription, .. CustomKeywordRegistry.AdditionalBeforeKeywords];
        if (CustomKeywordRegistry.AdditionalAfterKeywords.Count > 0)
            ___afterDescription = [.. ___afterDescription, .. CustomKeywordRegistry.AdditionalAfterKeywords];
    }
}

[HarmonyPatch(typeof(CardKeywordExtensions), nameof(CardKeywordExtensions.GetLocKeyPrefix))]
static class CustomLocKeyPatch
{
    [HarmonyPrefix]
    static bool Prefix(CardKeyword keyword, ref string? __result)
    {
        if (CustomKeywordRegistry.TryGetLocKeyPrefix(keyword, out var prefix))
        {
            __result = prefix;
            return false;
        }
        return true;
    }
}
