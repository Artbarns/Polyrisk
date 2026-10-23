using System.Text.Json;

public sealed record LobbyDiskSnapshot(int SchemaVersion, LobbyDiskState[] Lobbies);

public sealed record LobbyDiskState(
    string Code,
    int MaxPlayers,
    string HostPlayerId,
    LobbyStatus Status,
    long Version,
    DateTimeOffset LastActivityUtc,
    LobbyDiskPlayer[] Players,
    long LatestCommandSequence,
    GameCommandEnvelope[] Commands,
    GameStateEnvelope? GameState,
    GameCommandReceipt[]? CommandReceipts = null);

public sealed record LobbyDiskPlayer(
    string PlayerId,
    string ReconnectTokenHash,
    string DisplayName,
    bool IsHost,
    bool IsReady,
    DateTimeOffset JoinedUtc,
    DateTimeOffset LastSeenUtc);

public sealed class LobbyPersistence
{
    private const int SchemaVersion = 1;
    private readonly string path;
    private readonly ILogger<LobbyPersistence> logger;
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public LobbyPersistence(IConfiguration configuration, ILogger<LobbyPersistence> logger)
    {
        this.logger = logger;
        path = configuration["POLYRISK_DATA_PATH"]
            ?? Path.Combine(AppContext.BaseDirectory, "data", "lobbies.json");
    }

    public LobbyDiskState[] Load()
    {
        if (!File.Exists(path)) return Array.Empty<LobbyDiskState>();
        try
        {
            var snapshot = JsonSerializer.Deserialize<LobbyDiskSnapshot>(File.ReadAllText(path), jsonOptions);
            if (snapshot == null || snapshot.SchemaVersion != SchemaVersion)
            {
                logger.LogWarning("Ignoring lobby storage with unsupported schema at {StoragePath}", path);
                return Array.Empty<LobbyDiskState>();
            }
            return snapshot.Lobbies ?? Array.Empty<LobbyDiskState>();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogError(exception, "Could not read lobby storage at {StoragePath}", path);
            return Array.Empty<LobbyDiskState>();
        }
    }

    public void Save(IEnumerable<LobbyState> lobbies)
    {
        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporaryPath = path + ".tmp";
            var snapshot = new LobbyDiskSnapshot(SchemaVersion, lobbies.Select(lobby => lobby.ToDisk()).ToArray());
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(snapshot, jsonOptions));
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Could not persist lobby storage at {StoragePath}", path);
        }
    }
}
