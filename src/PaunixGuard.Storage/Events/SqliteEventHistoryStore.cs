using System.Globalization;
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

        return TryRead(reader);
    }

    public async Task<IReadOnlyList<GuardEvent>> GetAllAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 1000);

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
            var guardEvent = TryRead(reader);
            if (guardEvent is not null)
            {
                events.Add(guardEvent);
            }
        }

        return events.AsReadOnly();
    }

    private SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = paths.EventsDatabasePath
        };

        return new SqliteConnection(builder.ToString());
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

    private static GuardEvent? TryRead(SqliteDataReader reader)
    {
        if (!Guid.TryParse(reader.GetString(0), out var id)
            || !DateTimeOffset.TryParse(reader.GetString(1), null, DateTimeStyles.RoundtripKind, out var createdAt)
            || !Enum.TryParse<TriggerType>(reader.GetString(2), out var triggerType)
            || !Enum.TryParse<GuardState>(reader.GetString(3), out var guardStateBefore)
            || !Enum.TryParse<DisarmMethod>(reader.GetString(6), out var disarmMethod)
            || !Enum.TryParse<EventResolution>(reader.GetString(7), out var resolution))
        {
            return null;
        }

        return new GuardEvent
        {
            Id = id,
            CreatedAt = createdAt,
            TriggerType = triggerType,
            GuardStateBefore = guardStateBefore,
            AlarmStartedAt = ReadDateTime(reader, 4),
            AlarmStoppedAt = ReadDateTime(reader, 5),
            DisarmMethod = disarmMethod,
            Resolution = resolution,
            AppVersion = reader.GetString(8),
            Reason = reader.GetString(9),
            Source = reader.GetString(10)
        };
    }

    private static DateTimeOffset? ReadDateTime(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : DateTimeOffset.TryParse(reader.GetString(ordinal), null, DateTimeStyles.RoundtripKind, out var value)
                ? value
                : null;
    }
}
