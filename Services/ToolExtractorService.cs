using System.Diagnostics;

namespace ZoroDBRestore.Services;

public class ToolExtractorService : IToolExtractorService
{
    // Versioning the extraction dir means re-bundling new tool versions forces a re-extract
    private const string ToolVersion = "100.10.0";

    private static readonly string ExtractRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ZoroDBRestore",
        "tools",
        ToolVersion);

    public ToolPaths GetExpectedPaths()
    {
#if WINDOWS
        return new ToolPaths(
            Path.Combine(ExtractRoot, "mongodump.exe"),
            Path.Combine(ExtractRoot, "mongorestore.exe"));
#else
        return new ToolPaths(
            Path.Combine(ExtractRoot, "mongodump"),
            Path.Combine(ExtractRoot, "mongorestore"));
#endif
    }

    public async Task<ToolPaths?> EnsureToolsExtractedAsync(IProgress<string>? progress = null)
    {
        var paths = GetExpectedPaths();

        // Already extracted
        if (File.Exists(paths.MongoDump) && File.Exists(paths.MongoRestore))
        {
            progress?.Report("Bundled tools already extracted.");
            return paths;
        }

        Directory.CreateDirectory(ExtractRoot);

#if WINDOWS
        const string dumpAsset    = "tools/windows/mongodump.exe";
        const string restoreAsset = "tools/windows/mongorestore.exe";
#else
        const string dumpAsset    = "tools/macos/mongodump";
        const string restoreAsset = "tools/macos/mongorestore";
#endif

        var dumpExtracted    = await TryExtractAsync(dumpAsset,    paths.MongoDump,    progress);
        var restoreExtracted = await TryExtractAsync(restoreAsset, paths.MongoRestore, progress);

        if (!dumpExtracted || !restoreExtracted)
            return null;

#if MACCATALYST || IOS
        await MakeExecutableAsync(paths.MongoDump,    progress);
        await MakeExecutableAsync(paths.MongoRestore, progress);
#endif

        return paths;
    }

    private static async Task<bool> TryExtractAsync(
        string assetPath,
        string destinationPath,
        IProgress<string>? progress)
    {
        try
        {
            progress?.Report($"Extracting {Path.GetFileName(destinationPath)}…");

            await using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
            await using var dest   = File.Create(destinationPath);
            await stream.CopyToAsync(dest);

            progress?.Report($"  → {destinationPath}");
            return true;
        }
        catch (Exception ex)
        {
            progress?.Report($"[WARNING] Could not extract {assetPath}: {ex.Message}");
            return false;
        }
    }

    private static async Task MakeExecutableAsync(string filePath, IProgress<string>? progress)
    {
        try
        {
            var psi = new ProcessStartInfo("chmod", $"+x \"{filePath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow  = true
            };
            using var p = Process.Start(psi);
            if (p is not null) await p.WaitForExitAsync();
            progress?.Report($"chmod +x applied to {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            progress?.Report($"[WARNING] chmod failed for {filePath}: {ex.Message}");
        }
    }
}
