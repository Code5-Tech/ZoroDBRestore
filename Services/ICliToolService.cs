using ZoroDBRestore.Models;

namespace ZoroDBRestore.Services;

public interface ICliToolService
{
    Task<OperationResult> RunAsync(
        string executable,
        string arguments,
        IProgress<string>? progress = null,
        CancellationToken ct = default);

    Task<bool> IsToolAvailableAsync(string executablePath);
}
