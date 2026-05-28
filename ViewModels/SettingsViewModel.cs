using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZoroDBRestore.Services;

namespace ZoroDBRestore.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly ICliToolService _cliToolService;
    private readonly IToolExtractorService _toolExtractor;

    [ObservableProperty] private string _mongoDumpPath = "mongodump";
    [ObservableProperty] private string _mongoRestorePath = "mongorestore";
    [ObservableProperty] private string _defaultBackupDirectory = string.Empty;
    [ObservableProperty] private bool _mongoDumpAvailable;
    [ObservableProperty] private bool _mongoRestoreAvailable;
    [ObservableProperty] private string _extractionLog = string.Empty;
    [ObservableProperty] private bool _bundledToolsAvailable;

    public SettingsViewModel(
        ISettingsService settingsService,
        ICliToolService cliToolService,
        IToolExtractorService toolExtractor)
    {
        _settingsService = settingsService;
        _cliToolService  = cliToolService;
        _toolExtractor   = toolExtractor;
        LoadFromSettings();
        CheckBundledToolsExist();
    }

    public void LoadFromSettings()
    {
        var s = _settingsService.Settings;
        MongoDumpPath          = s.MongoDumpPath;
        MongoRestorePath       = s.MongoRestorePath;
        DefaultBackupDirectory = s.DefaultBackupDirectory;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        SetBusy(true, "Saving settings…");
        try
        {
            var s = _settingsService.Settings;
            s.MongoDumpPath          = MongoDumpPath;
            s.MongoRestorePath       = MongoRestorePath;
            s.DefaultBackupDirectory = DefaultBackupDirectory;
            await _settingsService.SaveAsync();
            SetStatus("Settings saved.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task CheckToolsAsync()
    {
        SetBusy(true, "Checking tools…");
        try
        {
            MongoDumpAvailable    = await _cliToolService.IsToolAvailableAsync(MongoDumpPath);
            MongoRestoreAvailable = await _cliToolService.IsToolAvailableAsync(MongoRestorePath);
            SetStatus(MongoDumpAvailable && MongoRestoreAvailable
                ? "Both tools found."
                : "One or more tools not found — check the paths above.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task ExtractBundledToolsAsync()
    {
        SetBusy(true, "Extracting bundled tools…");
        ExtractionLog = string.Empty;
        try
        {
            var lines    = new List<string>();
            var progress = new Progress<string>(line =>
            {
                lines.Add(line);
                ExtractionLog = string.Join(Environment.NewLine, lines);
            });

            var paths = await _toolExtractor.EnsureToolsExtractedAsync(progress);
            if (paths is not null)
            {
                MongoDumpPath         = paths.MongoDump;
                MongoRestorePath      = paths.MongoRestore;
                BundledToolsAvailable = true;
                SetStatus("Bundled tools extracted and paths updated.");
                await SaveAsync();
            }
            else
            {
                BundledToolsAvailable = false;
                SetStatus("Extraction failed — see log.");
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task BrowseBackupDirectoryAsync()
    {
        var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
        if (result.IsSuccessful)
            DefaultBackupDirectory = result.Folder.Path;
    }

    private void CheckBundledToolsExist()
    {
        var paths = _toolExtractor.GetExpectedPaths();
        BundledToolsAvailable = File.Exists(paths.MongoDump) && File.Exists(paths.MongoRestore);
    }
}
