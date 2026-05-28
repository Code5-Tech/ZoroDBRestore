using ZoroDBRestore.Models;

namespace ZoroDBRestore.Services;

public interface IMongoConnectionService
{
    Task<(bool Success, string Message)> TestConnectionAsync(MongoProfile profile, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDatabasesAsync(MongoProfile profile, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetCollectionsAsync(MongoProfile profile, string database, CancellationToken ct = default);
}
