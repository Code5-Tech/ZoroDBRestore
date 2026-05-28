using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZoroDBRestore.Models;
using ZoroDBRestore.Services;

namespace ZoroDBRestore.ViewModels;

public partial class RestoreViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly ICliToolService _cliToolService;

    // Profile picker
    [ObservableProperty] private ObservableCollection<MongoProfile> _profiles = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveProfileLabel))]
    private MongoProfile? _selectedProfile;

    public string ActiveProfileLabel =>
        SelectedProfile is null ? "No connection selected"
        : SelectedProfile.UseDirectUri
            ? $"{SelectedProfile.Name}  —  {SelectedProfile.DirectUri}"
            : $"{SelectedProfile.Name}  —  {SelectedProfile.Host}:{SelectedProfile.Port}";

    // Source: the backup folder on disk
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSourcePath))]
    private string _sourcePath = string.Empty;
    public bool HasSourcePath => !string.IsNullOrWhiteSpace(SourcePath);

    // Source DB: the database name as it appears inside the backup folder
    [ObservableProperty] private string _sourceDatabase = string.Empty;

    // Target DB: the name to restore INTO (can differ from source for rename)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRenaming))]
    private string _targetDatabase = string.Empty;

    /// <summary>True when source and target names differ (rename mode).</summary>
    public bool IsRenaming =>
        !string.IsNullOrWhiteSpace(SourceDatabase)
        && !string.IsNullOrWhiteSpace(TargetDatabase)
        && SourceDatabase != TargetDatabase;

    // Detected databases in the backup folder (for source picker)
    [ObservableProperty] private ObservableCollection<string> _detectedDatabases = [];

    [ObservableProperty] private bool _dropBeforeRestore;
    [ObservableProperty] private bool _useGzip;
    [ObservableProperty] private bool _gzipAutoDetected;   // shown as info badge
    [ObservableProperty] private bool _stopOnError = true;
    [ObservableProperty] private string _logOutput = string.Empty;

    private CancellationTokenSource? _cts;

    public RestoreViewModel(ISettingsService settingsService, ICliToolService cliToolService)
    {
        _settingsService = settingsService;
        _cliToolService  = cliToolService;
    }

    public void RefreshProfiles()
    {
        var settings = _settingsService.Settings;
        Profiles = new ObservableCollection<MongoProfile>(settings.Profiles);
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == settings.ActiveProfileId)
                       ?? Profiles.FirstOrDefault();
    }

    [RelayCommand]
    private async Task BrowseSourcePathAsync()
    {
        var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
        if (result.IsSuccessful)
        {
            SourcePath = result.Folder.Path;
            ScanBackupFolder(SourcePath);
        }
    }

    [RelayCommand]
    private async Task StartRestoreAsync()
    {
        if (SelectedProfile is null)
        {
            SetStatus("Please select a connection.");
            return;
        }
        if (string.IsNullOrWhiteSpace(SourcePath))
        {
            SetStatus("Please select a backup source directory.");
            return;
        }

        _cts = new CancellationTokenSource();
        SetBusy(true, "Running mongorestore…");
        LogOutput = string.Empty;

        try
        {
            var restoreExe = _settingsService.Settings.MongoRestorePath;
            var args = SelectedProfile.BuildMongoRestoreArgs(
                SourcePath,
                SourceDatabase,
                TargetDatabase,
                DropBeforeRestore,
                UseGzip,
                StopOnError);

            AppendLog($"$ {restoreExe} {args}");
            AppendLog(string.Empty);

            var progress = new Progress<string>(AppendLog);
            var result   = await _cliToolService.RunAsync(restoreExe, args, progress, _cts.Token);

            AppendLog(string.Empty);
            AppendLog(result.Success ? "✓ Restore completed successfully." : $"✗ {result.Message}");
            SetStatus(result.Success ? "Restore complete." : "Restore failed.");
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelRestore()
    {
        _cts?.Cancel();
        AppendLog("[Cancelling…]");
    }

    // ─────────────────────────────────────────────────────────
    // Scans the selected folder and fills:
    //   DetectedDatabases  — sub-folder names (= DB names in the dump)
    //   SourceDatabase     — pre-selected to first detected DB
    //   TargetDatabase     — pre-filled with SourceDatabase (no rename by default)
    //   UseGzip / GzipAutoDetected — set if .bson.gz files found
    // ─────────────────────────────────────────────────────────
    private void ScanBackupFolder(string path)
    {
        DetectedDatabases.Clear();
        SourceDatabase    = string.Empty;
        TargetDatabase    = string.Empty;
        GzipAutoDetected  = false;

        try
        {
            if (!Directory.Exists(path)) return;

            var subdirs = Directory.GetDirectories(path)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .OrderBy(n => n)
                .ToList();

            foreach (var db in subdirs!)
                DetectedDatabases.Add(db!);

            if (DetectedDatabases.Count == 1)
            {
                SourceDatabase = DetectedDatabases[0];
                TargetDatabase = DetectedDatabases[0];
            }

            // Auto-detect gzip: look for at least one .bson.gz file anywhere in subdirs
            var hasGzip = Directory
                .EnumerateFiles(path, "*.gz", SearchOption.AllDirectories)
                .Any();

            if (hasGzip)
            {
                UseGzip          = true;
                GzipAutoDetected = true;
            }
        }
        catch { /* non-critical */ }
    }

    partial void OnSourceDatabaseChanged(string value)
    {
        // When source DB changes and the user hasn't customised the target yet, sync it
        if (string.IsNullOrWhiteSpace(TargetDatabase) || TargetDatabase == SourceDatabase)
            TargetDatabase = value;

        OnPropertyChanged(nameof(IsRenaming));
    }

    partial void OnTargetDatabaseChanged(string value)
        => OnPropertyChanged(nameof(IsRenaming));

    private void AppendLog(string line) => LogOutput += line + Environment.NewLine;
}
