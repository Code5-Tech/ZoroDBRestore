namespace ZoroDBRestore.Models;

public class MongoProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Default";

    // ── Standard connection fields ──
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 27017;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AuthDatabase { get; set; } = "admin";
    public bool UseTls { get; set; } = false;
    public bool TlsAllowInvalidCertificates { get; set; } = false;

    // ── URI mode (required for Atlas / SRV / replica sets) ──
    // When true, DirectUri is used instead of individual host/port/auth fields.
    // Example: mongodb+srv://user:pass@cluster.mongodb.net/?retryWrites=true
    public bool UseDirectUri { get; set; } = false;
    public string DirectUri { get; set; } = string.Empty;

    // ─────────────────────────────────────────────────────────
    // Auth source for user/password. Empty/whitespace must never be passed to tools or
    // the driver — it becomes "" and causes SCRAM auth to fail. Default: admin.
    // ─────────────────────────────────────────────────────────
    public string GetEffectiveAuthDatabase() =>
        string.IsNullOrWhiteSpace(AuthDatabase) ? "admin" : AuthDatabase.Trim();

    // ─────────────────────────────────────────────────────────
    // MongoDB.Driver connection string (used for Test Connection and DB listing)
    // ─────────────────────────────────────────────────────────
    public string BuildConnectionString()
    {
        if (UseDirectUri && !string.IsNullOrWhiteSpace(DirectUri))
            return DirectUri;

        if (string.IsNullOrWhiteSpace(Username))
            return $"mongodb://{Host}:{Port}";

        var authDb = GetEffectiveAuthDatabase();
        return $"mongodb://{Uri.EscapeDataString(Username)}:{Uri.EscapeDataString(Password)}@{Host}:{Port}/{authDb}";
    }

    // ─────────────────────────────────────────────────────────
    // mongodump argument builder
    // ─────────────────────────────────────────────────────────
    public string BuildMongoDumpArgs(string database, string outputPath, bool gzip, bool oplog)
    {
        var args = new System.Text.StringBuilder();

        AppendConnectionArgs(args);

        if (!string.IsNullOrWhiteSpace(database))
            args.Append($" --db \"{database}\"");

        if (gzip)   args.Append(" --gzip");
        if (oplog)  args.Append(" --oplog");

        args.Append($" --out \"{outputPath}\"");
        return args.ToString();
    }

    // ─────────────────────────────────────────────────────────
    // mongorestore argument builder
    //
    // sourceDatabase  = the DB name as it lives inside the backup folder
    //                   (the sub-directory produced by mongodump)
    // targetDatabase  = the DB name to restore INTO on the server
    //                   if different from sourceDatabase → uses --nsFrom/--nsTo
    //                   if empty → restore everything in the dump
    // ─────────────────────────────────────────────────────────
    public string BuildMongoRestoreArgs(
        string sourcePath,
        string? sourceDatabase,
        string? targetDatabase,
        bool drop,
        bool gzip,
        bool stopOnError)
    {
        var args = new System.Text.StringBuilder();

        AppendConnectionArgs(args);

        var hasSrc = !string.IsNullOrWhiteSpace(sourceDatabase);
        var hasDst = !string.IsNullOrWhiteSpace(targetDatabase);

        if (hasSrc && hasDst && sourceDatabase != targetDatabase)
        {
            // Rename: restore sourcedb → targetdb
            args.Append($" --nsFrom \"{sourceDatabase}.*\" --nsTo \"{targetDatabase}.*\"");
        }
        else if (hasSrc)
        {
            // Filter: only restore the named database (same name)
            args.Append($" --nsInclude \"{sourceDatabase}.*\"");
        }
        // else: no namespace filter → restore everything in the dump

        if (drop)        args.Append(" --drop");
        if (gzip)        args.Append(" --gzip");
        if (stopOnError) args.Append(" --stopOnError");

        args.Append($" --dir \"{sourcePath}\"");
        return args.ToString();
    }

    // ─────────────────────────────────────────────────────────
    // Shared: append --uri OR --host/--port/--auth block
    // ─────────────────────────────────────────────────────────
    private void AppendConnectionArgs(System.Text.StringBuilder args)
    {
        if (UseDirectUri && !string.IsNullOrWhiteSpace(DirectUri))
        {
            args.Append($"--uri \"{DirectUri}\"");
            return;
        }

        args.Append($"--host {Host} --port {Port}");

        if (!string.IsNullOrWhiteSpace(Username))
        {
            var authDb = GetEffectiveAuthDatabase();
            args.Append($" --username \"{Username}\" --password \"{Password}\" --authenticationDatabase \"{authDb}\"");
        }

        if (UseTls)
        {
            args.Append(" --tls");
            if (TlsAllowInvalidCertificates)
                args.Append(" --tlsInsecure");
        }
    }
}
