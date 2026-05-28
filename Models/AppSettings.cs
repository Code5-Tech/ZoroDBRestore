namespace ZoroDBRestore.Models;

public class AppSettings
{
    public string MongoDumpPath { get; set; } = "mongodump";
    public string MongoRestorePath { get; set; } = "mongorestore";
    public string DefaultBackupDirectory { get; set; } = string.Empty;

    public List<MongoProfile> Profiles { get; set; } = [new MongoProfile()];
    public string ActiveProfileId { get; set; } = string.Empty;

    public MongoProfile ActiveProfile =>
        Profiles.FirstOrDefault(p => p.Id == ActiveProfileId)
        ?? Profiles.FirstOrDefault()
        ?? new MongoProfile();
}
