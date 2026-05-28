using System.Diagnostics;
using System.Text;
using ZoroDBRestore.Models;

namespace ZoroDBRestore.Services;

public class CliToolService : ICliToolService
{
    public async Task<OperationResult> RunAsync(
        string executable,
        string arguments,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var output = new StringBuilder();
        var errors = new StringBuilder();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                output.AppendLine(e.Data);
                progress?.Report(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                errors.AppendLine(e.Data);
                // mongodump/mongorestore write progress to stderr
                progress?.Report(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);

            var fullOutput = output.ToString() + errors.ToString();
            var success = process.ExitCode == 0;
            var message = success
                ? "Operation completed successfully."
                : $"Process exited with code {process.ExitCode}.";

            return new OperationResult(success, message, fullOutput.Trim());
        }
        catch (OperationCanceledException)
        {
            return new OperationResult(false, "Operation was cancelled.", output.ToString());
        }
        catch (Exception ex)
        {
            return new OperationResult(false, ex.Message, output.ToString());
        }
    }

    public async Task<bool> IsToolAvailableAsync(string executablePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return false;
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
