using System.Security.Cryptography;
using System.Text.Json;

namespace YuWanCard.Loader;

/// <summary>
///     Reads the <c>yuwan-variants.manifest</c> bundle, validates each variant
///     (sha256 + <c>compat-target.txt</c> marker + directory name), and picks the
///     newest variant whose compat target is &lt;= the host game version.
/// </summary>
internal static class LoaderVariantBundle
{
    private const string ManifestName = "yuwan-variants.manifest";
    private const string CompatTargetMarkerName = "compat-target.txt";
    private const string VariantAssemblyName = "YuWanCard.Content.dll";

    /// <summary>Picks the best variant for the given host version, or <see langword="null" /> when none is usable.</summary>
    internal static VariantCandidate? PickVariant(string loaderDir, Version? host)
    {
        var variants = LoadVariantManifest(loaderDir);
        if (variants.Count == 0)
            return null;

        variants.Sort(static (a, b) => a.Version.CompareTo(b.Version));

        if (host is null)
        {
            LoaderMain.Logger.Info("[Loader] Host numeric version unknown; using newest bundled variant.");
            return variants[^1];
        }

        var candidates = variants.Where(x => x.Version <= host).ToList();
        if (candidates.Count > 0)
            return candidates[^1];

        LoaderMain.Logger.Info(
            $"[Loader] No bundled variant <= host {host}; using newest bundled variant as best-effort fallback.");
        return variants[^1];
    }

    private static List<VariantCandidate> LoadVariantManifest(string loaderDir)
    {
        var manifestPath = Path.Combine(loaderDir, ManifestName);
        if (!File.Exists(manifestPath))
        {
            LoaderMain.Logger.Error($"[Loader] Missing variant manifest: {manifestPath}");
            return [];
        }

        BundleVariantManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<BundleVariantManifest>(
                File.ReadAllText(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            LoaderMain.Logger.Error($"[Loader] Failed to read variant manifest {manifestPath}: {ex}");
            return [];
        }

        if (manifest?.Variants is not { Count: > 0 })
        {
            LoaderMain.Logger.Error($"[Loader] Variant manifest contains no variants: {manifestPath}");
            return [];
        }

        var libRootFull = Path.GetFullPath(Path.Combine(loaderDir, "lib"));

        return
        [
            .. manifest.Variants.Select(entry => TryCreateVariantCandidate(loaderDir, libRootFull, entry))
                .OfType<VariantCandidate>(),
        ];
    }

    private static VariantCandidate? TryCreateVariantCandidate(
        string loaderDir,
        string libRootFull,
        BundleVariantEntry entry)
    {
        var compatTarget = entry.CompatTarget?.Trim();
        if (string.IsNullOrWhiteSpace(compatTarget) ||
            !LoaderHostVersion.TryParseVersionCore(compatTarget, out var version))
        {
            LoaderMain.Logger.Error($"[Loader] Ignoring invalid variant target '{entry.CompatTarget}'.");
            return null;
        }

        var relativeDir = string.IsNullOrWhiteSpace(entry.Directory)
            ? Path.Combine("lib", compatTarget)
            : entry.Directory.Trim();
        var variantDir = Path.GetFullPath(Path.Combine(loaderDir, relativeDir));
        if (!IsUnderDirectory(variantDir, libRootFull))
        {
            LoaderMain.Logger.Error($"[Loader] Ignoring variant outside lib directory: {relativeDir}");
            return null;
        }

        if (!string.Equals(Path.GetFileName(variantDir), compatTarget, StringComparison.OrdinalIgnoreCase))
        {
            LoaderMain.Logger.Error($"[Loader] Ignoring variant with mismatched directory: {relativeDir}");
            return null;
        }

        var marker = Path.Combine(variantDir, CompatTargetMarkerName);
        if (!File.Exists(marker) ||
            !string.Equals(File.ReadAllText(marker).Trim(), compatTarget, StringComparison.OrdinalIgnoreCase))
        {
            LoaderMain.Logger.Error($"[Loader] Ignoring variant with missing or mismatched marker: {marker}");
            return null;
        }

        var assemblyName = string.IsNullOrWhiteSpace(entry.Assembly) ? VariantAssemblyName : entry.Assembly.Trim();
        if (!string.Equals(assemblyName, VariantAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            LoaderMain.Logger.Error($"[Loader] Ignoring variant with unexpected assembly name: {assemblyName}");
            return null;
        }

        var dllPath = Path.Combine(variantDir, assemblyName);
        if (!File.Exists(dllPath))
        {
            LoaderMain.Logger.Error($"[Loader] Ignoring variant missing {VariantAssemblyName}: {dllPath}");
            return null;
        }

        if (MatchesExpectedHash(dllPath, entry.Sha256))
            return new(compatTarget, version, dllPath);

        LoaderMain.Logger.Error($"[Loader] Ignoring variant with mismatched hash: {dllPath}");
        return null;
    }

    private static bool IsUnderDirectory(string path, string root)
    {
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
        var normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesExpectedHash(string path, string? expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256))
            return false;

        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        return string.Equals(actual, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    internal sealed record VariantCandidate(string CompatTarget, Version Version, string DllPath);

    private sealed class BundleVariantManifest
    {
        public List<BundleVariantEntry>? Variants { get; init; }
    }

    private sealed class BundleVariantEntry
    {
        public string? CompatTarget { get; set; }

        public string? Directory { get; set; }

        public string? Assembly { get; set; }

        public string? Sha256 { get; set; }
    }
}
