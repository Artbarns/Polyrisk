using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(WebGLHosting.ListenUrls());
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 384 * 1024);
int requestBudget = WebGLHosting.RequestBudget();
var allowedOrigins = (Environment.GetEnvironmentVariable("POLYRISK_ALLOWED_ORIGINS") ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    else
        policy.SetIsOriginAllowed(IsLocalOrigin).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = requestBudget,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("lobby-entry", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddSingleton<LobbyPersistence>();
builder.Services.AddSingleton<LobbyStore>();
builder.Services.AddHostedService<LobbyCleanupService>();

var app = builder.Build();
if (string.Equals(Environment.GetEnvironmentVariable("POLYRISK_REQUIRE_HTTPS"), "true", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseWebGLHosting();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok(new
{
    service = "PolyRisk Lobby",
    status = "ok",
    protocol = 1,
    utc = DateTimeOffset.UtcNow
}));

app.MapPost("/api/lobbies", (CreateLobbyRequest request, LobbyStore store) =>
{
    if (!PlayerName.TryNormalize(request.DisplayName, out var displayName, out var error))
        return Results.BadRequest(new ApiError(error));

    int maxPlayers = Math.Clamp(request.MaxPlayers, 2, 6);
    return Results.Ok(store.Create(displayName, maxPlayers));
}).RequireRateLimiting("lobby-entry");

app.MapPost("/api/lobbies/{code}/join", (string code, JoinLobbyRequest request, LobbyStore store) =>
{
    if (!PlayerName.TryNormalize(request.DisplayName, out var displayName, out var error))
        return Results.BadRequest(new ApiError(error));

    return store.TryJoin(code, displayName, out var session, out error)
        ? Results.Ok(session)
        : Results.BadRequest(new ApiError(error));
}).RequireRateLimiting("lobby-entry");

app.MapPost("/api/lobbies/{code}/snapshot", (string code, SessionRequest request, LobbyStore store) =>
    store.TrySnapshot(code, request, out var snapshot, out var error)
        ? Results.Ok(snapshot)
        : Results.BadRequest(new ApiError(error)));

app.MapPost("/api/lobbies/{code}/ready", (string code, SetReadyRequest request, LobbyStore store) =>
    store.TrySetReady(code, request, out var snapshot, out var error)
        ? Results.Ok(snapshot)
        : Results.BadRequest(new ApiError(error)));

app.MapPost("/api/lobbies/{code}/start", (string code, SessionRequest request, LobbyStore store) =>
    store.TryStart(code, request, out var snapshot, out var error)
        ? Results.Ok(snapshot)
        : Results.BadRequest(new ApiError(error)));

app.MapPost("/api/lobbies/{code}/abandon", (string code, SessionRequest request, LobbyStore store) =>
    store.TryAbandon(code, request, out var snapshot, out var error)
        ? Results.Ok(snapshot)
        : Results.BadRequest(new ApiError(error)));

app.MapPost("/api/lobbies/{code}/leave", (string code, SessionRequest request, LobbyStore store) =>
    store.TryLeave(code, request, out var snapshot, out var error)
        ? Results.Ok(snapshot)
        : Results.BadRequest(new ApiError(error)));

app.MapPost("/api/lobbies/{code}/game/commands", (string code, SubmitGameCommandRequest request, LobbyStore store) =>
    store.TrySubmitCommand(code, request, out var command, out var error)
        ? Results.Ok(command)
        : Results.BadRequest(new ApiError(error)));

app.MapPost("/api/lobbies/{code}/game/commands/poll", (string code, PollGameCommandsRequest request, LobbyStore store) =>
    store.TryPollCommands(code, request, out var commands, out var error)
        ? Results.Ok(commands)
        : Results.BadRequest(new ApiError(error)));

app.MapPost("/api/lobbies/{code}/game/state", (string code, PublishGameStateRequest request, LobbyStore store) =>
    store.TryPublishState(code, request, out var state, out var error)
        ? Results.Ok(state)
        : Results.BadRequest(new ApiError(error)));

app.MapPost("/api/lobbies/{code}/game/state/get", (string code, SessionRequest request, LobbyStore store) =>
    store.TryGetState(code, request, out var state, out var error)
        ? Results.Ok(state)
        : Results.BadRequest(new ApiError(error)));

app.Run();

static bool IsLocalOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
    return uri.Scheme is "http" or "https"
        && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host == "127.0.0.1"
            || uri.Host == "::1");
}

public sealed record ApiError(string Error);
public sealed record CreateLobbyRequest(string DisplayName, int MaxPlayers = 6);
public sealed record JoinLobbyRequest(string DisplayName);
public record SessionRequest(string PlayerId, string ReconnectToken);
public sealed record SetReadyRequest(string PlayerId, string ReconnectToken, bool IsReady)
    : SessionRequest(PlayerId, ReconnectToken);
public sealed record SubmitGameCommandRequest(
    string PlayerId,
    string ReconnectToken,
    long ExpectedStateVersion,
    string Type,
    string PayloadJson,
    string ClientCommandId = "") : SessionRequest(PlayerId, ReconnectToken);
public sealed record PollGameCommandsRequest(
    string PlayerId,
    string ReconnectToken,
    long AfterSequence) : SessionRequest(PlayerId, ReconnectToken);
public sealed record PublishGameStateRequest(
    string PlayerId,
    string ReconnectToken,
    long ExpectedStateVersion,
    long LastAppliedCommandSequence,
    string PayloadJson) : SessionRequest(PlayerId, ReconnectToken);

public sealed record LobbySessionResponse(string PlayerId, string ReconnectToken, LobbySnapshot Lobby);
public sealed record LobbySnapshot(
    string Code,
    int MaxPlayers,
    string Status,
    long Version,
    string HostPlayerId,
    LobbyPlayerSnapshot[] Players);
public sealed record LobbyPlayerSnapshot(string PlayerId, string DisplayName, bool IsHost, bool IsReady);
public sealed record GameCommandEnvelope(
    long Sequence,
    long ExpectedStateVersion,
    string PlayerId,
    string Type,
    string PayloadJson,
    DateTimeOffset CreatedUtc);
public sealed record GameCommandReceipt(
    string ClientCommandId,
    long Sequence,
    long ExpectedStateVersion,
    string PlayerId,
    string Type,
    string PayloadHash,
    DateTimeOffset CreatedUtc);
public sealed record GameCommandBatch(long StateVersion, long LatestSequence, GameCommandEnvelope[] Commands);
public sealed record GameStateEnvelope(
    long Version,
    long LastAppliedCommandSequence,
    string PayloadJson,
    DateTimeOffset UpdatedUtc);

public enum LobbyStatus
{
    Waiting,
    Started,
    Abandoned
}

public sealed class LobbyStore
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly HashSet<string> AllowedCommandTypes = new(StringComparer.Ordinal)
    {
        "turn.roll", "action.primary", "action.secondary", "transit.choose",
        "audit.card", "audit.pay", "audit.roll", "audit.stay",
        "dice.adjust", "dice.turbo", "ultimate.use", "venture.confirm", "stock.invest",
        "marketplace.pass", "marketplace.sanction", "marketplace.tender.begin",
        "marketplace.tender.cancel", "marketplace.tender.seal",
        "marketplace.bid.submit", "marketplace.bid.skip",
        "bankruptcy.upgrades", "bankruptcy.mortgage", "bankruptcy.sell", "bankruptcy.give_up"
    };
    private readonly object gate = new();
    private readonly Dictionary<string, LobbyState> lobbies = new(StringComparer.OrdinalIgnoreCase);
    private readonly LobbyPersistence persistence;
    private readonly ILogger<LobbyStore> logger;

    public LobbyStore(LobbyPersistence persistence, ILogger<LobbyStore> logger)
    {
        this.persistence = persistence;
        this.logger = logger;
        foreach (var saved in persistence.Load())
        {
            try
            {
                var lobby = LobbyState.Restore(saved);
                if (lobby.Players.Count is < 1 or > 6 || lobby.Code.Length != 6) continue;
                lobbies[lobby.Code] = lobby;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Ignoring an invalid persisted lobby");
            }
        }
        logger.LogInformation("Loaded {LobbyCount} persisted PolyRisk lobbies", lobbies.Count);
    }

    public LobbySessionResponse Create(string displayName, int maxPlayers)
    {
        lock (gate)
        {
            string code;
            do code = CreateCode(); while (lobbies.ContainsKey(code));

            var host = LobbyPlayer.Create(displayName, isHost: true);
            var lobby = new LobbyState(code, maxPlayers, host);
            lobbies.Add(code, lobby);
            PersistLocked();
            logger.LogInformation("Lobby {LobbyCode} created for {MaxPlayers} players", code, maxPlayers);
            return Session(lobby, host);
        }
    }

    public bool TryJoin(string rawCode, string displayName, out LobbySessionResponse? session, out string error)
    {
        lock (gate)
        {
            session = null;
            if (!TryGetLobby(rawCode, out var lobby, out error)) return false;
            if (lobby.Status != LobbyStatus.Waiting)
            {
                error = "This game has already started.";
                return false;
            }
            if (lobby.Players.Count >= lobby.MaxPlayers)
            {
                error = "This lobby is full.";
                return false;
            }
            if (lobby.Players.Any(p => string.Equals(p.DisplayName, displayName, StringComparison.OrdinalIgnoreCase)))
            {
                error = "This name is already used in the lobby.";
                return false;
            }

            var player = LobbyPlayer.Create(displayName, isHost: false);
            lobby.Players.Add(player);
            lobby.Touch(change: true);
            PersistLocked();
            logger.LogInformation("Player {PlayerId} joined lobby {LobbyCode}", player.PlayerId, lobby.Code);
            session = Session(lobby, player);
            return true;
        }
    }

    public bool TrySnapshot(string code, SessionRequest request, out LobbySnapshot? snapshot, out string error)
    {
        lock (gate)
        {
            snapshot = null;
            if (!TryAuthenticate(code, request, out var lobby, out var player, out error)) return false;
            player.LastSeenUtc = DateTimeOffset.UtcNow;
            lobby.Touch(change: false);
            snapshot = Snapshot(lobby);
            return true;
        }
    }

    public bool TrySetReady(string code, SetReadyRequest request, out LobbySnapshot? snapshot, out string error)
    {
        lock (gate)
        {
            snapshot = null;
            if (!TryAuthenticate(code, request, out var lobby, out var player, out error)) return false;
            if (lobby.Status != LobbyStatus.Waiting)
            {
                error = "The game has already started.";
                return false;
            }

            if (player.IsReady != request.IsReady)
            {
                player.IsReady = request.IsReady;
                lobby.Touch(change: true);
                PersistLocked();
            }
            else
            {
                lobby.Touch(change: false);
            }
            snapshot = Snapshot(lobby);
            return true;
        }
    }

    public bool TryStart(string code, SessionRequest request, out LobbySnapshot? snapshot, out string error)
    {
        lock (gate)
        {
            snapshot = null;
            if (!TryAuthenticate(code, request, out var lobby, out var player, out error)) return false;
            if (!player.IsHost)
            {
                error = "Only the host can start the game.";
                return false;
            }
            if (lobby.Players.Count < 2)
            {
                error = "At least two players are required.";
                return false;
            }
            if (lobby.Players.Any(p => !p.IsReady))
            {
                error = "Every player must be ready.";
                return false;
            }

            lobby.Status = LobbyStatus.Started;
            lobby.Touch(change: true);
            PersistLocked();
            logger.LogInformation("Lobby {LobbyCode} started with {PlayerCount} players", lobby.Code, lobby.Players.Count);
            snapshot = Snapshot(lobby);
            return true;
        }
    }

    public bool TryLeave(string code, SessionRequest request, out LobbySnapshot? snapshot, out string error)
    {
        lock (gate)
        {
            snapshot = null;
            if (!TryAuthenticate(code, request, out var lobby, out var player, out error)) return false;

            lobby.Players.Remove(player);
            if (lobby.Players.Count == 0)
            {
                lobbies.Remove(lobby.Code);
                PersistLocked();
                return true;
            }

            if (player.IsHost)
            {
                var nextHost = lobby.Players.OrderBy(p => p.JoinedUtc).First();
                nextHost.IsHost = true;
                lobby.HostPlayerId = nextHost.PlayerId;
            }
            lobby.Touch(change: true);
            PersistLocked();
            snapshot = Snapshot(lobby);
            return true;
        }
    }

    public bool TryAbandon(string code, SessionRequest request, out LobbySnapshot? snapshot, out string error)
    {
        lock (gate)
        {
            snapshot = null;
            if (!TryAuthenticate(code, request, out var lobby, out _, out error)) return false;
            if (lobby.Status == LobbyStatus.Abandoned)
            {
                snapshot = Snapshot(lobby);
                return true;
            }
            if (lobby.Status != LobbyStatus.Started)
            {
                error = "Only a started game can be abandoned.";
                return false;
            }

            lobby.Status = LobbyStatus.Abandoned;
            lobby.Touch(change: true);
            PersistLocked();
            logger.LogInformation("Lobby {LobbyCode} abandoned by a player", lobby.Code);
            snapshot = Snapshot(lobby);
            return true;
        }
    }

    public bool TrySubmitCommand(
        string code,
        SubmitGameCommandRequest request,
        out GameCommandEnvelope? command,
        out string error)
    {
        lock (gate)
        {
            command = null;
            if (!TryAuthenticate(code, request, out var lobby, out _, out error)) return false;
            if (!ValidateStarted(lobby, out error)) return false;
            string type = (request.Type ?? string.Empty).Trim();
            if (type.Length is < 2 or > 64 || type.Any(c => !char.IsLetterOrDigit(c) && c is not '.' and not '_' and not '-'))
            {
                error = "The command type is invalid.";
                return false;
            }
            if (!AllowedCommandTypes.Contains(type))
            {
                error = "The command type is not supported by this protocol version.";
                return false;
            }
            string payload = request.PayloadJson ?? "{}";
            string clientCommandId = (request.ClientCommandId ?? string.Empty).Trim();
            string payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
            if (clientCommandId.Length > 64 || clientCommandId.Any(c => !char.IsLetterOrDigit(c) && c is not '-' and not '_'))
            {
                error = "The client command ID is invalid.";
                return false;
            }
            if (!PayloadSize.TryValidate(payload, 32 * 1024, out error)) return false;

            if (clientCommandId.Length > 0)
            {
                var receipt = lobby.CommandReceipts.FirstOrDefault(item =>
                    string.Equals(item.ClientCommandId, clientCommandId, StringComparison.Ordinal));
                if (receipt != null)
                {
                    bool sameRequest = receipt.PlayerId == request.PlayerId
                        && receipt.ExpectedStateVersion == request.ExpectedStateVersion
                        && receipt.Type == type
                        && receipt.PayloadHash == payloadHash;
                    if (sameRequest)
                    {
                        command = new GameCommandEnvelope(receipt.Sequence, receipt.ExpectedStateVersion,
                            receipt.PlayerId, receipt.Type, payload, receipt.CreatedUtc);
                        error = string.Empty;
                        return true;
                    }
                    error = "The client command ID has already been used.";
                    return false;
                }
            }

            if (request.ExpectedStateVersion != lobby.GameState.Version)
            {
                error = $"Stale game state. Expected version {lobby.GameState.Version}.";
                return false;
            }

            command = new GameCommandEnvelope(
                ++lobby.LatestCommandSequence,
                request.ExpectedStateVersion,
                request.PlayerId,
                type,
                payload,
                DateTimeOffset.UtcNow);
            lobby.Commands.Add(command);
            if (clientCommandId.Length > 0)
            {
                lobby.CommandReceipts.Add(new GameCommandReceipt(clientCommandId, command.Sequence,
                    command.ExpectedStateVersion, command.PlayerId, command.Type, payloadHash, command.CreatedUtc));
                if (lobby.CommandReceipts.Count > 512)
                    lobby.CommandReceipts.RemoveRange(0, lobby.CommandReceipts.Count - 512);
            }
            lobby.Touch(change: false);
            PersistLocked();
            return true;
        }
    }

    public bool TryPollCommands(
        string code,
        PollGameCommandsRequest request,
        out GameCommandBatch? batch,
        out string error)
    {
        lock (gate)
        {
            batch = null;
            if (!TryAuthenticate(code, request, out var lobby, out var player, out error)) return false;
            if (!ValidateStarted(lobby, out error)) return false;
            if (!player.IsHost)
            {
                error = "Only the host can consume game commands.";
                return false;
            }

            batch = new GameCommandBatch(
                lobby.GameState.Version,
                lobby.LatestCommandSequence,
                lobby.Commands.Where(c => c.Sequence > request.AfterSequence).Take(100).ToArray());
            return true;
        }
    }

    public bool TryPublishState(
        string code,
        PublishGameStateRequest request,
        out GameStateEnvelope? state,
        out string error)
    {
        lock (gate)
        {
            state = null;
            if (!TryAuthenticate(code, request, out var lobby, out var player, out error)) return false;
            if (!ValidateStarted(lobby, out error)) return false;
            if (!player.IsHost)
            {
                error = "Only the host can publish the game state.";
                return false;
            }
            if (request.ExpectedStateVersion != lobby.GameState.Version)
            {
                error = $"Stale game state. Expected version {lobby.GameState.Version}.";
                return false;
            }
            if (request.LastAppliedCommandSequence < lobby.GameState.LastAppliedCommandSequence ||
                request.LastAppliedCommandSequence > lobby.LatestCommandSequence)
            {
                error = "The applied command sequence is invalid.";
                return false;
            }
            if (!PayloadSize.TryValidate(request.PayloadJson, 256 * 1024, out error)) return false;

            state = new GameStateEnvelope(
                lobby.GameState.Version + 1,
                request.LastAppliedCommandSequence,
                request.PayloadJson ?? "{}",
                DateTimeOffset.UtcNow);
            lobby.GameState = state;
            lobby.Commands.RemoveAll(c => c.Sequence <= request.LastAppliedCommandSequence);
            lobby.Touch(change: true);
            PersistLocked();
            return true;
        }
    }

    public bool TryGetState(string code, SessionRequest request, out GameStateEnvelope? state, out string error)
    {
        lock (gate)
        {
            state = null;
            if (!TryAuthenticate(code, request, out var lobby, out var viewer, out error)) return false;
            if (!ValidateStarted(lobby, out error)) return false;
            string payload = lobby.GameState.PayloadJson;
            if (!viewer.IsHost && !TryProjectHiddenBalances(payload, viewer.PlayerId, out payload))
            {
                error = "The current game state could not be projected for this player.";
                return false;
            }
            state = lobby.GameState with { PayloadJson = payload };
            return true;
        }
    }

    private static bool TryProjectHiddenBalances(string payloadJson, string viewerPlayerId, out string projectedJson)
    {
        projectedJson = payloadJson;
        JsonObject? root;
        try { root = JsonNode.Parse(payloadJson) as JsonObject; }
        catch (JsonException) { return false; }
        if (root == null) return false;
        if (root["players"] is not JsonArray players) return true;

        double sgo = root["currentSgo"] is JsonValue sgoNode && sgoNode.TryGetValue<double>(out var parsedSgo)
            ? Math.Max(0, parsedSgo)
            : 0;
        foreach (var node in players)
        {
            if (node is not JsonObject player) return false;
            if (player["lobbyPlayerId"]?.GetValue<string>() is string lobbyPlayerId
                && string.Equals(lobbyPlayerId, viewerPlayerId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (player["balance"] is not JsonValue balanceNode || !balanceNode.TryGetValue<double>(out var balance))
                return false;
            player["balance"] = GetWealthBandRepresentative(balance, sgo);
        }

        projectedJson = root.ToJsonString();
        return true;
    }

    private static double GetWealthBandRepresentative(double balance, double sgo)
    {
        if (sgo <= 0 || balance <= 5 * sgo) return 0;
        if (balance <= 20 * sgo) return 10 * sgo;
        if (balance <= 50 * sgo) return 30 * sgo;
        return 51 * sgo;
    }

    public void RemoveExpired(DateTimeOffset cutoff)
    {
        lock (gate)
        {
            var expired = lobbies.Values.Where(l => l.LastActivityUtc < cutoff).Select(l => l.Code).ToArray();
            foreach (var code in expired)
                lobbies.Remove(code);
            if (expired.Length > 0) PersistLocked();
        }
    }

    private bool TryGetLobby(string rawCode, out LobbyState lobby, out string error)
    {
        lobby = null!;
        string code = (rawCode ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length != 6 || !lobbies.TryGetValue(code, out var found))
        {
            error = "Game code not found.";
            return false;
        }
        lobby = found;
        error = string.Empty;
        return true;
    }

    private bool TryAuthenticate(
        string code,
        SessionRequest request,
        out LobbyState lobby,
        out LobbyPlayer player,
        out string error)
    {
        lobby = null!;
        player = null!;
        if (!TryGetLobby(code, out lobby, out error)) return false;
        player = lobby.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId)!;
        if (player == null || !TokensMatch(player.ReconnectTokenHash, request.ReconnectToken))
        {
            error = "This player session is no longer valid.";
            return false;
        }
        player.LastSeenUtc = DateTimeOffset.UtcNow;
        return true;
    }

    private static bool ValidateStarted(LobbyState lobby, out string error)
    {
        if (lobby.Status != LobbyStatus.Started)
        {
            error = "The game has not started.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    private static bool TokensMatch(string expectedHash, string? actual)
    {
        if (string.IsNullOrEmpty(actual)) return false;
        byte[] actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
        byte[] expected;
        try { expected = Convert.FromHexString(expectedHash); }
        catch (FormatException) { return false; }
        if (expected.Length != actualHash.Length) return false;
        return CryptographicOperations.FixedTimeEquals(
            expected,
            actualHash);
    }

    private static LobbySessionResponse Session(LobbyState lobby, LobbyPlayer player) =>
        new(player.PlayerId, player.ReconnectToken
            ?? throw new InvalidOperationException("A new session must have a reconnect token."), Snapshot(lobby));

    private static LobbySnapshot Snapshot(LobbyState lobby) => new(
        lobby.Code,
        lobby.MaxPlayers,
        lobby.Status.ToString().ToLowerInvariant(),
        lobby.Version,
        lobby.HostPlayerId,
        lobby.Players
            .OrderByDescending(p => p.IsHost)
            .ThenBy(p => p.JoinedUtc)
            .Select(p => new LobbyPlayerSnapshot(p.PlayerId, p.DisplayName, p.IsHost, p.IsReady))
            .ToArray());

    private static string CreateCode()
    {
        Span<char> chars = stackalloc char[6];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        return new string(chars);
    }

    private void PersistLocked() => persistence.Save(lobbies.Values);
}

public sealed class LobbyState
{
    public LobbyState(string code, int maxPlayers, LobbyPlayer host)
    {
        Code = code;
        MaxPlayers = maxPlayers;
        HostPlayerId = host.PlayerId;
        Players.Add(host);
        LastActivityUtc = DateTimeOffset.UtcNow;
    }

    public string Code { get; }
    public int MaxPlayers { get; }
    public string HostPlayerId { get; set; }
    public LobbyStatus Status { get; set; }
    public long Version { get; private set; } = 1;
    public DateTimeOffset LastActivityUtc { get; private set; }
    public List<LobbyPlayer> Players { get; } = new();
    public long LatestCommandSequence { get; set; }
    public List<GameCommandEnvelope> Commands { get; } = new();
    public List<GameCommandReceipt> CommandReceipts { get; } = new();
    public GameStateEnvelope GameState { get; set; } = new(0, 0, "{}", DateTimeOffset.UtcNow);

    public void Touch(bool change)
    {
        LastActivityUtc = DateTimeOffset.UtcNow;
        if (change) Version++;
    }

    public LobbyDiskState ToDisk() => new(
        Code, MaxPlayers, HostPlayerId, Status, Version, LastActivityUtc,
        Players.Select(player => player.ToDisk()).ToArray(),
        LatestCommandSequence, Commands.ToArray(), GameState, CommandReceipts.ToArray());

    public static LobbyState Restore(LobbyDiskState saved)
    {
        if (saved.Players == null || saved.Players.Length == 0)
            throw new InvalidDataException("Persisted lobby has no players.");
        var players = saved.Players.Select(LobbyPlayer.Restore).ToArray();
        var lobby = new LobbyState(saved.Code, Math.Clamp(saved.MaxPlayers, 2, 6), players[0])
        {
            HostPlayerId = saved.HostPlayerId,
            Status = saved.Status,
            Version = Math.Max(1, saved.Version),
            LastActivityUtc = saved.LastActivityUtc,
            LatestCommandSequence = Math.Max(0, saved.LatestCommandSequence),
            GameState = saved.GameState ?? new GameStateEnvelope(0, 0, "{}", DateTimeOffset.UtcNow)
        };
        lobby.Players.Clear();
        lobby.Players.AddRange(players);
        lobby.Commands.AddRange(saved.Commands ?? Array.Empty<GameCommandEnvelope>());
        lobby.CommandReceipts.AddRange(saved.CommandReceipts ?? Array.Empty<GameCommandReceipt>());
        return lobby;
    }
}

public static class PayloadSize
{
    public static bool TryValidate(string? payload, int maxBytes, out string error)
    {
        payload ??= "{}";
        if (Encoding.UTF8.GetByteCount(payload) > maxBytes)
        {
            error = $"Payload exceeds the {maxBytes}-byte limit.";
            return false;
        }
        try
        {
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 64 });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Payload must be a JSON object.";
                return false;
            }
        }
        catch (JsonException)
        {
            error = "Payload is not valid JSON.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}

public sealed class LobbyPlayer
{
    public required string PlayerId { get; init; }
    public string? ReconnectToken { get; init; }
    public required string ReconnectTokenHash { get; init; }
    public required string DisplayName { get; init; }
    public bool IsHost { get; set; }
    public bool IsReady { get; set; }
    public DateTimeOffset JoinedUtc { get; init; }
    public DateTimeOffset LastSeenUtc { get; set; }

    public static LobbyPlayer Create(string displayName, bool isHost)
    {
        var now = DateTimeOffset.UtcNow;
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        return new LobbyPlayer
        {
            PlayerId = Guid.NewGuid().ToString("N"),
            ReconnectToken = token,
            ReconnectTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))),
            DisplayName = displayName,
            IsHost = isHost,
            JoinedUtc = now,
            LastSeenUtc = now
        };
    }

    public LobbyDiskPlayer ToDisk() => new(
        PlayerId, ReconnectTokenHash, DisplayName, IsHost, IsReady, JoinedUtc, LastSeenUtc);

    public static LobbyPlayer Restore(LobbyDiskPlayer saved) => new()
    {
        PlayerId = saved.PlayerId,
        ReconnectToken = null,
        ReconnectTokenHash = saved.ReconnectTokenHash,
        DisplayName = saved.DisplayName,
        IsHost = saved.IsHost,
        IsReady = saved.IsReady,
        JoinedUtc = saved.JoinedUtc,
        LastSeenUtc = saved.LastSeenUtc
    };
}

public static class PlayerName
{
    public static bool TryNormalize(string? raw, out string value, out string error)
    {
        value = (raw ?? string.Empty).Trim().Normalize(NormalizationForm.FormC);
        if (value.Length is < 2 or > 18)
        {
            error = "The player name must contain 2 to 18 characters.";
            return false;
        }
        if (value.Any(char.IsControl))
        {
            error = "The player name contains an unsupported character.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}

public sealed class LobbyCleanupService(LobbyStore store) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            store.RemoveExpired(DateTimeOffset.UtcNow.AddHours(-2));
    }
}
