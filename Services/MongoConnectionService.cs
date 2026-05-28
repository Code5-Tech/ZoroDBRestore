using MongoDB.Driver;
using ZoroDBRestore.Models;

namespace ZoroDBRestore.Services;

public class MongoConnectionService : IMongoConnectionService
{
    public async Task<(bool Success, string Message)> TestConnectionAsync(MongoProfile profile, CancellationToken ct = default)
    {
        try
        {
            var settings = MongoClientSettings.FromConnectionString(profile.BuildConnectionString());
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout = TimeSpan.FromSeconds(5);

            var client = new MongoClient(settings);
            await client.ListDatabaseNamesAsync(ct);
            return (true, "Connection successful.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<IReadOnlyList<string>> GetDatabasesAsync(MongoProfile profile, CancellationToken ct = default)
    {
        var settings = MongoClientSettings.FromConnectionString(profile.BuildConnectionString());
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

        var client = new MongoClient(settings);
        var cursor = await client.ListDatabaseNamesAsync(ct);
        var names = await cursor.ToListAsync(ct);
        return names
            .Where(n => n is not ("admin" or "config" or "local"))
            .OrderBy(n => n)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetCollectionsAsync(MongoProfile profile, string database, CancellationToken ct = default)
    {
        var settings = MongoClientSettings.FromConnectionString(profile.BuildConnectionString());
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

        var client = new MongoClient(settings);
        var db = client.GetDatabase(database);
        var cursor = await db.ListCollectionNamesAsync(cancellationToken: ct);
        var names = await cursor.ToListAsync(ct);
        return names.OrderBy(n => n).ToList();
    }
}
