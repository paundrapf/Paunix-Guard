using Microsoft.Data.Sqlite;
using PaunixGuard.Core.Events;
using PaunixGuard.Core.GuardState;
using PaunixGuard.Core.Triggers;
using PaunixGuard.Storage.Paths;

namespace PaunixGuard.Storage.Events;

public sealed class SqliteEventHistoryStore(AppDataPaths paths) : IEventHistoryStore
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        paths.EnsureCreated();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS guard_events (
                id TEXT PRIMARY KEY,
                created_at TEXT NOT NULL,
                trigger_type TEXT NOT NULL,
                guard_state_before TEXT NOT NULL,
                alarm_started_at TEXT NULL,
                alarm_stopped_at TEXT NULL,
                disarm_method TEXT NOT NULL,
                resolution TEXT NOT NULL,
                app_version TEXT NOT NULL,
                reason TEXT NOT NULL,
                source TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_guard_events_created_at
                ON guard_events(created_at DESC);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddAsync(GuardEvent guardEvent, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO guard_events (
                id,
                created_at,
                trigger_type,
                guard_state_before,
                alarm_started_at,
                alarm_stopped_at,
                disarm_method,
                resolution,
                app_version,
                reason,
                source
            ) VALUES (
                $id,
                $created_at,
                $trigger_type,
                $guard_state_before,
                $alarm_started_at,
                $alarm_stopped_at,
                $disarm_method,
                $resolution,
                $app_version,
                $reason,
                $source
            );
            """;
        Bind(command, guardEvent);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(GuardEvent guardEvent, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE guard_events SET
                alarm_started_at = $alarm_started_at,
                alarm_stopped_at = $alarm_stopped_at,
                disarm_method = $disarm_method,
                resolution = $resolution,
                reason = $reason,
                source = $source
            WHERE id = $id;
            """;
        Bind(command, guardEvent);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<GuardEvent?> GetLatestAsync(CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   created_at,
                   trigger_type,
                   guard_state_before,
                   alarm_started_at,
                   alarm_stopped_at,
                   disarm_method,
                   resolution,
                   app_version,
                   reason,
                   source
            FROM guard_events
            ORDER BY created_at DESC
            LIMIT 1;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Read(reader);
    }

    public async Task<IReadOnlyList<GuardEvent>> GetAllAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,
                   created_at,
                   trigger_type,
                   guard_state_before,
                   alarm_started_at,
                   alarm_stopped_at,
                   disarm_method,
                   resolution,
                   app_version,
                   reason,
                   source
            FROM guard_events
            ORDER BY created_at DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        var events = new List<GuardEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(Read(reader));
        }

        return events.AsReadOnly();
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={paths.EventsDatabasePath}");
    }

    private static void Bind(SqliteCommand command, GuardEvent guardEvent)
    {
        command.Parameters.AddWithValue("$id", guardEvent.Id.ToString("D"));
        command.Parameters.AddWithValue("$created_at", guardEvent.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$trigger_type", guardEvent.TriggerType.ToString());
        command.Parameters.AddWithValue("$guard_state_before", guardEvent.GuardStateBefore.ToString());
        command.Parameters.AddWithValue("$alarm_started_at", ToDb(guardEvent.AlarmStartedAt));
        command.Parameters.AddWithValue("$alarm_stopped_at", ToDb(guardEvent.AlarmStoppedAt));
        command.Parameters.AddWithValue("$disarm_method", guardEvent.DisarmMethod.ToString());
        command.Parameters.AddWithValue("$resolution", guardEvent.Resolution.ToString());
        command.Parameters.AddWithValue("$app_version", guardEvent.AppVersion);
        command.Parameters.AddWithValue("$reason", guardEvent.Reason);
        command.Parameters.AddWithValue("$source", guardEvent.Source);
    }

    private static object ToDb(DateTimeOffset? value)
    {
        return value.HasValue ? value.Value.ToString("O") : (object)DBNull.Value;
    }

    private static GuardEvent Read(SqliteDataReader reader)
    {
        return new GuardEvent
        {
            Id = Guid.Parse(reader.GetString(0)),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
            TriggerType = Enum.Parse<TriggerType>(reader.GetString(2)),
            GuardStateBefore = Enum.Parse<GuardState>(reader.GetString(3)),
            AlarmStartedAt = ReadDateTime(reader, 4),
            AlarmStoppedAt = ReadDateTime(reader, 5),
            DisarmMethod = Enum.Parse<DisarmMethod>(reader.GetString(6)),
            Resolution = Enum.Parse<EventResolution>(reader.GetString(7)),
            AppVersion = reader.GetString(8),
            Reason = reader.GetString(9),
            Source = reader.GetString(10)
        };
    }

    private static DateTimeOffset? ReadDateTime(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : DateTimeOffset.Parse(reader.GetString(ordinal), null, System.Globalization.DateTimeStyles.RoundtripKind);
    }
}

