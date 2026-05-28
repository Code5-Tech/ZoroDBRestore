using ZoroDBRestore.Models;

namespace ZoroDBRestore.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }

    Task LoadAsync();
    Task SaveAsync();

    // Profile management
    MongoProfile AddProfile(string name);
    void UpdateProfile(MongoProfile updated);
    bool DeleteProfile(string id);
    void SetDefaultProfile(string id);
}
