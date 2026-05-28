using System.Text.Json;
using ZoroDBRestore.Models;

namespace ZoroDBRestore.Services;

public class SettingsService : ISettingsService
{
    private readonly IToolExtractorService _toolExtractor;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ZoroDBRestore",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Settings { get; private set; } = new();

    public SettingsService(IToolExtractorService toolExtractor)
    {
        _toolExtractor = toolExtractor;
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = await File.ReadAllTextAsync(SettingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            Settings = new AppSettings();
        }

        // Ensure at least one profile exists
        if (Settings.Profiles.Count == 0)
            Settings.Profiles.Add(new MongoProfile());

        // Ensure ActiveProfileId points to an existing profile
        if (!Settings.Profiles.Any(p => p.Id == Settings.ActiveProfileId))
            Settings.ActiveProfileId = Settings.Profiles[0].Id;

        // If tool paths are still defaults, try the extracted bundled paths
        if (Settings.MongoDumpPath == "mongodump" || Settings.MongoRestorePath == "mongorestore")
        {
            var bundledPaths = _toolExtractor.GetExpectedPaths();
            if (File.Exists(bundledPaths.MongoDump))
                Settings.MongoDumpPath = bundledPaths.MongoDump;
            if (File.Exists(bundledPaths.MongoRestore))
                Settings.MongoRestorePath = bundledPaths.MongoRestore;
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(Settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsPath, json);
        }
        catch
        {
            // Non-fatal
        }
    }

    public MongoProfile AddProfile(string name)
    {
        var profile = new MongoProfile { Name = name };
        Settings.Profiles.Add(profile);
        return profile;
    }

    public void UpdateProfile(MongoProfile updated)
    {
        var index = Settings.Profiles.FindIndex(p => p.Id == updated.Id);
        if (index >= 0)
            Settings.Profiles[index] = updated;
    }

    public bool DeleteProfile(string id)
    {
        if (Settings.Profiles.Count <= 1)
            return false; // must keep at least one

        var removed = Settings.Profiles.RemoveAll(p => p.Id == id) > 0;

        if (removed && Settings.ActiveProfileId == id)
            Settings.ActiveProfileId = Settings.Profiles[0].Id;

        return removed;
    }

    public void SetDefaultProfile(string id)
    {
        if (Settings.Profiles.Any(p => p.Id == id))
            Settings.ActiveProfileId = id;
    }
}
