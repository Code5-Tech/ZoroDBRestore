# ZoroDBRestore

A cross-platform desktop utility for **MongoDB backup and restore**, built with **.NET MAUI**. It wraps the official MongoDB Database Tools (`mongodump` / `mongorestore`) in a simple UI so you can manage connections, run backups, and restore databases on **Windows** and **macOS** without using the command line.

---

## Features

### Connection management
- Save **multiple MongoDB connection profiles** with names (e.g. Local, Staging, Production).
- Set one profile as the **default**; Backup and Restore pick it up automatically.
- **Standard mode**: host, port, username, password, auth database, TLS, and optional “allow invalid certificates”.
- **Connection URI mode**: paste a full URI for **MongoDB Atlas**, SRV (`mongodb+srv://`), or replica sets.
- **Test connection** from the UI (uses MongoDB.Driver with a short timeout).
- **Add**, **edit**, **duplicate**, and **delete** profiles (at least one profile is always kept).
- Empty auth database defaults to **`admin`** so `mongodump` / `mongorestore` never receive an invalid `--authenticationDatabase ""`.

### Backup
- Connect to a server and **list user databases** (excludes `admin`, `config`, `local`).
- Back up a **single database** or **all databases**.
- Choose an **output folder** (with optional default path in Settings).
- Options:
  - **`--gzip`** — compress backup files (enabled by default).
  - **`--oplog`** — include oplog for point-in-time style backups (replica sets).
- **Live log output** streamed from the tool (stdout + stderr).
- **Cancel** an in-progress backup.
- **Connection picker** on the Backup tab to use any saved profile.

### Restore
- Select a **mongodump output folder** via folder picker.
- **Auto-detect** databases in the backup folder and **gzip** (`.gz`) files; `--gzip` is turned on when a compressed backup is detected.
- **Source database** — name as stored in the dump folder.
- **Target database** — restore into the same name or **rename** using `--nsFrom` / `--nsTo`.
- Options:
  - **`--drop`** — drop collections before restore (with a visible warning).
  - **`--gzip`** — read gzipped dumps.
  - **`--stopOnError`** — stop on first error.
- **Live log output** and **cancel** support.
- **Connection picker** for the target server.

### Settings & tools
- Configure **`mongodump`** and **`mongorestore`** paths (or rely on bundled tools).
- **Check tool availability** (`--version`).
- **Default backup directory** for new backup jobs.
- **Bundled MongoDB Database Tools**: optional platform-specific binaries can be shipped inside the app and **extracted on first launch** to:
  - **Windows:** `%AppData%\ZoroDBRestore\tools\<version>\`
  - **macOS:** same path under Application Support; `chmod +x` is applied after extract.

### UX
- Tabbed shell: **Dashboard**, **Connections**, **Backup**, **Restore**, **Settings**.
- High-contrast theme: light page background, distinct input fields, dark info/alert panels, indigo tab bar.
- Minimum window size: **800 × 620**.

---

## How it works

| Layer | Role |
|--------|------|
| **MongoDB.Driver** | Test connections, list databases/collections in the UI |
| **mongodump / mongorestore** | Actual backup and restore (BSON on disk) |
| **MVVM** | `CommunityToolkit.Mvvm` ViewModels + XAML views |
| **Settings** | JSON file under `%AppData%\ZoroDBRestore\settings.json` (Windows) or equivalent on macOS |

Backup/restore does **not** reimplement BSON export in C#; it invokes the same CLI tools you would use in a terminal, with arguments built from your connection profile.

---

## Requirements

### Runtime
- **Windows 10** (17763+) or **macOS 15+** (Mac Catalyst)
- [.NET 9 SDK](https://dotnet.microsoft.com/download) and **.NET MAUI workload** for building from source

### MongoDB Database Tools
You need **`mongodump`** and **`mongorestore`** available either:

1. **Bundled in the app** (recommended for distribution) — see [Bundling tools](#bundling-mongodb-database-tools) below, or  
2. **Installed on PATH** or pointed to in **Settings → Tool paths**

Download tools: [MongoDB Database Tools](https://www.mongodb.com/try/download/database-tools)

### Network (remote / Atlas)
- Firewall / security group allows outbound access to MongoDB.
- For Atlas: IP allowlist and a valid connection string (use **URI mode**).
- User must have read rights for backup and appropriate rights for restore.

---

## Getting started

### Clone and build

```bash
git clone https://github.com/<your-org>/ZoroDBRestore.git
cd ZoroDBRestore
dotnet restore
```

**Windows (Visual Studio 2022 / VS Enterprise recommended):**

```bash
dotnet build -f net9.0-windows10.0.19041.0
```

Run from Visual Studio with target **Windows Machine**, or:

```bash
dotnet run -f net9.0-windows10.0.19041.0
```

**macOS:**

```bash
dotnet build -f net9.0-maccatalyst
dotnet run -f net9.0-maccatalyst
```

Install the MAUI workload if needed:

```bash
dotnet workload install maui
```

### First-time setup

1. Open **Settings** → **Extract Bundled Tools** (if you bundled binaries), or set paths to your installed `mongodump` / `mongorestore`.
2. Open **Connections** → add a profile → **Test Connection** → **Save** → optionally **Set Default**.
3. Use **Backup** or **Restore** with the connection picker at the top of each page.

---

## Bundling MongoDB Database Tools

Before building a release, copy the official binaries into the project:

| Platform | Folder | Files |
|----------|--------|--------|
| Windows | `Resources/Raw/tools/windows/` | `mongodump.exe`, `mongorestore.exe` |
| macOS | `Resources/Raw/tools/macos/` | `mongodump`, `mongorestore` (no extension) |

The `.csproj` includes only the matching platform folder per target. On first run, tools are extracted to `%AppData%\ZoroDBRestore\tools\100.10.0\` (version constant in `ToolExtractorService`).

To ship a new tools version, update `ToolVersion` in `Services/ToolExtractorService.cs` and replace the binaries.

---

## Usage guide

### Connections tab
- **Left:** list of saved profiles; **Default** badge on the active default.
- **Right:** edit form, **Test Connection**, **Set Default**, **Duplicate**, **Delete**, **Save**.
- Toggle **Use Connection URI** for Atlas / SRV strings.

### Backup tab
1. Select a **connection**.
2. Click **Refresh Databases**.
3. Choose a database or enable **Backup all databases**.
4. Pick **output directory**.
5. Enable **gzip** / **oplog** if needed.
6. **Start Backup** — watch the log; **Cancel** if needed.

### Restore tab
1. Select a **connection** (target server).
2. **Browse** to the mongodump output folder.
3. Confirm **source** / **target** database names (rename uses `--nsFrom` / `--nsTo`).
4. Enable **gzip** if the dump was compressed (often auto-detected).
5. **Start Restore**.

### Settings tab
- Extract or configure **mongodump** / **mongorestore** paths.
- Set **default backup directory**.
- **Save Settings**.

---

## Project structure

```
ZoroDBRestore/
├── Models/              # MongoProfile, AppSettings, OperationResult
├── Services/            # Settings, Mongo connection, CLI runner, tool extractor
├── ViewModels/          # MVVM for each page
├── Views/               # Backup, Restore, Connections, Settings (XAML)
├── Resources/
│   ├── Raw/tools/       # Optional bundled mongodump/mongorestore
│   └── Styles/          # Colors.xaml, Styles.xaml
├── MainPage.xaml        # Dashboard
├── AppShell.xaml        # Tab navigation
└── MauiProgram.cs       # DI and startup
```

---

## Configuration file

Settings are stored as JSON:

**Windows:** `%AppData%\ZoroDBRestore\settings.json`

Example shape:

```json
{
  "MongoDumpPath": "...",
  "MongoRestorePath": "...",
  "DefaultBackupDirectory": "E:\\Backups\\MongoDB",
  "Profiles": [
    {
      "Id": "...",
      "Name": "Production",
      "Host": "db.example.com",
      "Port": 27017,
      "Username": "user",
      "Password": "...",
      "AuthDatabase": "admin",
      "UseTls": true,
      "UseDirectUri": false,
      "DirectUri": ""
    }
  ],
  "ActiveProfileId": "..."
}
```

Passwords are stored in plain text locally — treat the settings file as sensitive.

---

## Troubleshooting

| Issue | What to check |
|--------|----------------|
| **Authentication failed** | Username/password; set **Auth database** (`admin` or the DB where the user was created). For Atlas, use **URI mode**. |
| **Remote backup fails** | Network, firewall, IP allowlist, TLS. Use URI for Atlas. |
| **Tools not found** | Settings → paths or **Extract Bundled Tools**; run **Check Availability**. |
| **Restore finds no data** | Point to the folder that **contains** database subfolders (mongodump `--out` path). Enable **gzip** if backup used `--gzip`. |
| **Restore to a new DB name** | Set **source** = name in dump, **target** = new name (rename banner shows `--nsFrom` / `--nsTo`). |
| **Build fails on Windows** | Close a running `ZoroDBRestore.exe` if the build cannot copy the output file. |

---

## Tech stack

- [.NET 9](https://dotnet.microsoft.com/) + [.NET MAUI](https://learn.microsoft.com/dotnet/maui/)
- [CommunityToolkit.Maui](https://learn.microsoft.com/dotnet/communitytoolkit/maui/) 9.1.1
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) 8.4.2
- [MongoDB.Driver](https://www.mongodb.com/docs/drivers/csharp/) 3.8.0

---

## License

This project is licensed under the **GNU General Public License v3.0** — see [LICENSE](LICENSE).

MongoDB Database Tools are distributed separately under [MongoDB’s tools license](https://www.mongodb.com/legal/terms/tools); ensure you comply with their terms when bundling binaries.

---

## Contributing

Issues and pull requests are welcome. When changing backup/restore behavior, verify against the same `mongodump` / `mongorestore` versions you ship or document in Settings.
