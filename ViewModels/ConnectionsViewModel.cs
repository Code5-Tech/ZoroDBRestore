using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZoroDBRestore.Models;
using ZoroDBRestore.Services;

namespace ZoroDBRestore.ViewModels;

/// <summary>
/// Wraps a MongoProfile for display in the connections list,
/// exposing a computed subtitle and default-badge visibility.
/// </summary>
public partial class ProfileListItem : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public MongoProfile Profile { get; }

    [ObservableProperty] private bool _isDefault;

    public string Subtitle => Profile.UseDirectUri
        ? Profile.DirectUri
        : $"{Profile.Host}:{Profile.Port}";

    public ProfileListItem(MongoProfile profile, ISettingsService settingsService)
    {
        Profile = profile;
        _settingsService = settingsService;
        _isDefault = settingsService.Settings.ActiveProfileId == profile.Id;
    }

    public void RefreshIsDefault()
    {
        IsDefault = _settingsService.Settings.ActiveProfileId == Profile.Id;
    }
}

public partial class ConnectionsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IMongoConnectionService _mongoService;

    // --- List panel ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private ObservableCollection<ProfileListItem> _profileItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private ProfileListItem? _selectedItem;

    public bool HasSelection => SelectedItem is not null;
    public bool CanDelete => ProfileItems.Count > 1 && SelectedItem is not null;

    // --- Edit form fields (bound to the selected profile) ---
    [ObservableProperty] private string _editName = string.Empty;

    // URI mode
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStandardMode))]
    private bool _editUseDirectUri;
    [ObservableProperty] private string _editDirectUri = string.Empty;
    public bool IsStandardMode => !EditUseDirectUri;

    // Standard mode
    [ObservableProperty] private string _editHost = "localhost";
    [ObservableProperty] private string _editPort = "27017";
    [ObservableProperty] private string _editUsername = string.Empty;
    [ObservableProperty] private string _editPassword = string.Empty;
    [ObservableProperty] private string _editAuthDatabase = "admin";
    [ObservableProperty] private bool _editUseTls;
    [ObservableProperty] private bool _editTlsAllowInvalidCertificates;

    // --- Test connection result ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTestResult))]
    private string _testResultMessage = string.Empty;

    [ObservableProperty] private bool _testResultSuccess;
    public bool HasTestResult => !string.IsNullOrEmpty(TestResultMessage);

    public ConnectionsViewModel(ISettingsService settingsService, IMongoConnectionService mongoService)
    {
        _settingsService = settingsService;
        _mongoService = mongoService;
        ReloadList();
    }

    // Rebuild the observable list from the settings model
    private void ReloadList()
    {
        var wasSelectedId = SelectedItem?.Profile.Id;
        ProfileItems = new ObservableCollection<ProfileListItem>(
            _settingsService.Settings.Profiles
                .Select(p => new ProfileListItem(p, _settingsService)));

        SelectedItem = ProfileItems.FirstOrDefault(i => i.Profile.Id == wasSelectedId)
                    ?? ProfileItems.FirstOrDefault(i => i.IsDefault)
                    ?? ProfileItems.FirstOrDefault();
    }

    partial void OnSelectedItemChanged(ProfileListItem? value)
    {
        TestResultMessage = string.Empty;
        if (value is null) return;
        var p = value.Profile;
        EditName                         = p.Name;
        EditUseDirectUri                 = p.UseDirectUri;
        EditDirectUri                    = p.DirectUri;
        EditHost                         = p.Host;
        EditPort                         = p.Port.ToString();
        EditUsername                     = p.Username;
        EditPassword                     = p.Password;
        EditAuthDatabase                 = p.AuthDatabase;
        EditUseTls                       = p.UseTls;
        EditTlsAllowInvalidCertificates  = p.TlsAllowInvalidCertificates;
    }

    [RelayCommand]
    private async Task AddProfileAsync()
    {
        var newProfile = _settingsService.AddProfile("New Connection");
        await _settingsService.SaveAsync();
        ReloadList();
        SelectedItem = ProfileItems.FirstOrDefault(i => i.Profile.Id == newProfile.Id);
        SetStatus("New connection added.");
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (SelectedItem is null) return;

        var updated = new MongoProfile
        {
            Id                            = SelectedItem.Profile.Id,
            Name                          = EditName,
            UseDirectUri                  = EditUseDirectUri,
            DirectUri                     = EditDirectUri,
            Host                          = EditHost,
            Port                          = int.TryParse(EditPort, out var p) ? p : 27017,
            Username                      = EditUsername,
            Password                      = EditPassword,
            AuthDatabase                  = EditAuthDatabase,
            UseTls                        = EditUseTls,
            TlsAllowInvalidCertificates   = EditTlsAllowInvalidCertificates
        };

        _settingsService.UpdateProfile(updated);
        await _settingsService.SaveAsync();

        // Refresh the list item's display (name/subtitle may have changed)
        ReloadList();
        SetStatus($"'{updated.Name}' saved.");
    }

    [RelayCommand]
    private async Task DeleteProfileAsync()
    {
        if (SelectedItem is null) return;
        var name = SelectedItem.Profile.Name;

        if (!_settingsService.DeleteProfile(SelectedItem.Profile.Id))
        {
            SetStatus("Cannot delete the last connection.");
            return;
        }

        await _settingsService.SaveAsync();
        ReloadList();
        SetStatus($"'{name}' deleted.");
    }

    [RelayCommand]
    private async Task SetDefaultAsync()
    {
        if (SelectedItem is null) return;
        _settingsService.SetDefaultProfile(SelectedItem.Profile.Id);
        await _settingsService.SaveAsync();

        foreach (var item in ProfileItems)
            item.RefreshIsDefault();

        SetStatus($"'{SelectedItem.Profile.Name}' is now the default connection.");
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (SelectedItem is null) return;
        SetBusy(true, "Testing connection…");
        TestResultMessage = string.Empty;
        try
        {
            var profile = BuildCurrentProfile();
            var (success, message) = await _mongoService.TestConnectionAsync(profile);
            TestResultSuccess  = success;
            TestResultMessage  = message;
            SetStatus(success ? "Connected." : "Connection failed.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // Duplicate: creates a copy of the selected profile with a new Id
    [RelayCommand]
    private async Task DuplicateProfileAsync()
    {
        if (SelectedItem is null) return;
        var src = SelectedItem.Profile;
        var copy = new MongoProfile
        {
            Name                         = src.Name + " (copy)",
            UseDirectUri                 = src.UseDirectUri,
            DirectUri                    = src.DirectUri,
            Host                         = src.Host,
            Port                         = src.Port,
            Username                     = src.Username,
            Password                     = src.Password,
            AuthDatabase                 = src.AuthDatabase,
            UseTls                       = src.UseTls,
            TlsAllowInvalidCertificates  = src.TlsAllowInvalidCertificates
        };
        _settingsService.Settings.Profiles.Add(copy);
        await _settingsService.SaveAsync();
        ReloadList();
        SelectedItem = ProfileItems.FirstOrDefault(i => i.Profile.Id == copy.Id);
        SetStatus($"Duplicated as '{copy.Name}'.");
    }

    private MongoProfile BuildCurrentProfile() => new()
    {
        Id                           = SelectedItem?.Profile.Id ?? string.Empty,
        Name                         = EditName,
        UseDirectUri                 = EditUseDirectUri,
        DirectUri                    = EditDirectUri,
        Host                         = EditHost,
        Port                         = int.TryParse(EditPort, out var p) ? p : 27017,
        Username                     = EditUsername,
        Password                     = EditPassword,
        AuthDatabase                 = EditAuthDatabase,
        UseTls                       = EditUseTls,
        TlsAllowInvalidCertificates  = EditTlsAllowInvalidCertificates
    };
}
