using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZoroDBRestore.Models;
using ZoroDBRestore.Services;

namespace ZoroDBRestore.ViewModels;

public partial class BackupViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IMongoConnectionService _mongoService;
    private readonly ICliToolService _cliToolService;

    // Profile picker
    [ObservableProperty] private ObservableCollection<MongoProfile> _profiles = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveProfileLabel))]
    private MongoProfile? _selectedProfile;

    public string ActiveProfileLabel =>
        SelectedProfile is null ? "No connection selected"
        : $"{SelectedProfile.Name}  —  {SelectedProfile.Host}:{SelectedProfile.Port}";

    // Backup settings
    [ObservableProperty] private ObservableCollection<string> _databases = [];
    [ObservableProperty] private string? _selectedDatabase;
    [ObservableProperty] private string _outputDirectory = string.Empty;
    [ObservableProperty] private bool _useGzip = true;
    [ObservableProperty] private bool _useOplog;
    [ObservableProperty] private string _logOutput = string.Empty;
    [ObservableProperty] private bool _backupAllDatabases;

    private CancellationTokenSource? _cts;

    public BackupViewModel(
        ISettingsService settingsService,
        IMongoConnectionService mongoService,
        ICliToolService cliToolService)
    {
        _settingsService = settingsService;
        _mongoService    = mongoService;
        _cliToolService  = cliToolService;
    }

    // Called from OnAppearing so the list stays current after edits in Connections tab
    public void RefreshProfiles()
    {
        var settings = _settingsService.Settings;
        Profiles = new ObservableCollection<MongoProfile>(settings.Profiles);
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == settings.ActiveProfileId)
                       ?? Profiles.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(settings.DefaultBackupDirectory))
            OutputDirectory = settings.DefaultBackupDirectory;
    }

    [RelayCommand]
    public async Task LoadDatabasesAsync()
    {
        if (SelectedProfile is null)
        {
            SetStatus("Please select a connection first.");
            return;
        }

        SetBusy(true, "Connecting to MongoDB…");
        Databases.Clear();
        LogOutput = string.Empty;
        try
        {
            var dbs = await _mongoService.GetDatabasesAsync(SelectedProfile);
            foreach (var db in dbs) Databases.Add(db);
            SelectedDatabase = Databases.FirstOrDefault();
            SetStatus($"Found {Databases.Count} database(s).");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            AppendLog($"[ERROR] {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task BrowseOutputDirectoryAsync()
    {
        var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
        if (result.IsSuccessful)
            OutputDirectory = result.Folder.Path;
    }

    [RelayCommand]
    private async Task StartBackupAsync()
    {
        if (SelectedProfile is null)    { SetStatus("Please select a connection.");          return; }
        if (string.IsNullOrWhiteSpace(OutputDirectory)) { SetStatus("Please select an output directory."); return; }
        if (!BackupAllDatabases && string.IsNullOrWhiteSpace(SelectedDatabase))
        { SetStatus("Please select a database."); return; }

        _cts = new CancellationTokenSource();
        SetBusy(true, "Running mongodump…");
        LogOutput = string.Empty;

        try
        {
            var dumpExe = _settingsService.Settings.MongoDumpPath;
            var dbArg   = BackupAllDatabases ? string.Empty : SelectedDatabase!;
            var args    = SelectedProfile.BuildMongoDumpArgs(dbArg, OutputDirectory, UseGzip, UseOplog);

            AppendLog($"$ {dumpExe} {args}");
            AppendLog(string.Empty);

            var progress = new Progress<string>(AppendLog);
            var result   = await _cliToolService.RunAsync(dumpExe, args, progress, _cts.Token);

            AppendLog(string.Empty);
            AppendLog(result.Success ? "✓ Backup completed successfully." : $"✗ {result.Message}");
            SetStatus(result.Success ? "Backup complete." : "Backup failed.");
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelBackup()
    {
        _cts?.Cancel();
        AppendLog("[Cancelling…]");
    }

    private void AppendLog(string line) => LogOutput += line + Environment.NewLine;
}
