namespace ZoroDBRestore.Services;

public interface IToolExtractorService
{
    /// <summary>
    /// Extracts bundled mongodump/mongorestore binaries to a writable directory
    /// on first run. Returns the directory where tools were extracted.
    /// </summary>
    Task<ToolPaths?> EnsureToolsExtractedAsync(IProgress<string>? progress = null);

    /// <summary>
    /// Returns the expected extraction paths without extracting.
    /// </summary>
    ToolPaths GetExpectedPaths();
}

public record ToolPaths(string MongoDump, string MongoRestore);
