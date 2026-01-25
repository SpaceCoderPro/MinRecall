using Microsoft.Data.Sqlite;
using MinRecall.Core.Models;
using System.Collections.Concurrent;
using System.Text;

namespace MinRecall.Core.Database;

public class DatabaseManager : IDisposable
{
    private readonly string _dbPath;
    private readonly ConcurrentDictionary<string, SqliteConnection> _connections = new();
    private readonly object _lock = new();

    public DatabaseManager(string dbPath)
    {
        try
        {
            Console.WriteLine($"[DEBUG] DatabaseManager constructor - path: {dbPath}");
            _dbPath = dbPath;
            
            Console.WriteLine("[DEBUG] Calling EnsureDatabaseCreated...");
            EnsureDatabaseCreated();
            Console.WriteLine("[DEBUG] Database created/verified successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] DatabaseManager constructor failed:");
            Console.WriteLine($"  Type: {ex.GetType().FullName}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public string DatabasePath => _dbPath;

    private SqliteConnection GetConnection()
    {
        var threadId = Thread.CurrentThread.ManagedThreadId.ToString();
        if (!_connections.TryGetValue(threadId, out var connection))
        {
            connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            _connections[threadId] = connection;
        }
        return connection;
    }

    private void EnsureDatabaseCreated()
    {
        try
        {
            Console.WriteLine("[DEBUG] EnsureDatabaseCreated starting...");
            
            var directory = Path.GetDirectoryName(_dbPath);
            Console.WriteLine($"[DEBUG] Database directory: {directory}");
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Console.WriteLine($"[DEBUG] Creating directory: {directory}");
                Directory.CreateDirectory(directory);
                Console.WriteLine("[DEBUG] Directory created successfully");
            }
            else
            {
                Console.WriteLine("[DEBUG] Directory already exists or is empty");
            }

            Console.WriteLine($"[DEBUG] Opening SQLite connection to: {_dbPath}");
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            Console.WriteLine("[DEBUG] SQLite connection opened successfully");

            var command = connection.CreateCommand();
        command.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA cache_size = -64000;
            PRAGMA page_size = 4096;

            CREATE TABLE IF NOT EXISTS Screenshots (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp INTEGER NOT NULL,
                WindowTitle TEXT NOT NULL,
                ProcessName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                IsKeyframe INTEGER NOT NULL DEFAULT 0,
                KeyframeId INTEGER,
                DeltaFileId INTEGER,
                Width INTEGER NOT NULL,
                Height INTEGER NOT NULL,
                FileSize INTEGER NOT NULL,
                Status INTEGER NOT NULL DEFAULT 0,
                ProcessedAt INTEGER
            );

            CREATE TABLE IF NOT EXISTS ActivityLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessName TEXT NOT NULL,
                WindowTitle TEXT NOT NULL,
                StartTime INTEGER NOT NULL,
                EndTime INTEGER NOT NULL,
                DurationSeconds INTEGER NOT NULL,
                ScreenshotCount INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS DeltaFiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ScreenshotId INTEGER NOT NULL,
                FilePath TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                CreatedAt INTEGER NOT NULL,
                FOREIGN KEY (ScreenshotId) REFERENCES Screenshots(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS OcrData (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ScreenshotId INTEGER NOT NULL UNIQUE,
                Text TEXT NOT NULL,
                Language TEXT NOT NULL,
                Confidence REAL,
                ProcessedAt INTEGER NOT NULL,
                FOREIGN KEY (ScreenshotId) REFERENCES Screenshots(Id) ON DELETE CASCADE
            );

            CREATE VIRTUAL TABLE IF NOT EXISTS ScreenshotsFts USING fts5(
                ScreenshotId,
                WindowTitle,
                OcrText,
                content='Screenshots',
                content_rowid='Id'
            );

            CREATE INDEX IF NOT EXISTS idx_screenshots_timestamp ON Screenshots(Timestamp);
            CREATE INDEX IF NOT EXISTS idx_screenshots_process ON Screenshots(ProcessName);
            CREATE INDEX IF NOT EXISTS idx_screenshots_keyframe ON Screenshots(IsKeyframe, KeyframeId);
            CREATE INDEX IF NOT EXISTS idx_activity_start ON ActivityLog(StartTime);
            CREATE INDEX IF NOT EXISTS idx_activity_process ON ActivityLog(ProcessName);

            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
        ";
            Console.WriteLine("[DEBUG] Executing database schema creation SQL...");
            command.ExecuteNonQuery();
            Console.WriteLine("[DEBUG] Database schema created/verified successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] EnsureDatabaseCreated failed:");
            Console.WriteLine($"  Type: {ex.GetType().FullName}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public long InsertScreenshot(Screenshot screenshot)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Screenshots (
                Timestamp, WindowTitle, ProcessName, FilePath, IsKeyframe,
                KeyframeId, Width, Height, FileSize, Status
            ) VALUES (
                @timestamp, @windowTitle, @processName, @filePath, @isKeyframe,
                @keyframeId, @width, @height, @fileSize, @status
            );
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@timestamp", screenshot.Timestamp.ToFileTimeUtc());
        command.Parameters.AddWithValue("@windowTitle", screenshot.WindowTitle);
        command.Parameters.AddWithValue("@processName", screenshot.ProcessName);
        command.Parameters.AddWithValue("@filePath", screenshot.FilePath);
        command.Parameters.AddWithValue("@isKeyframe", screenshot.IsKeyframe ? 1 : 0);
        command.Parameters.AddWithValue("@keyframeId", screenshot.KeyframeId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@width", screenshot.Width);
        command.Parameters.AddWithValue("@height", screenshot.Height);
        command.Parameters.AddWithValue("@fileSize", screenshot.FileSize);
        command.Parameters.AddWithValue("@status", (int)screenshot.Status);

        return (long)command.ExecuteScalar()!;
    }

    public void UpdateScreenshotStatus(long id, ScreenshotStatus status, DateTime? processedAt = null)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Screenshots
            SET Status = @status, ProcessedAt = @processedAt
            WHERE Id = @id
        ";

        command.Parameters.AddWithValue("@status", (int)status);
        command.Parameters.AddWithValue("@processedAt", processedAt?.ToFileTimeUtc() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@id", id);

        command.ExecuteNonQuery();
    }

    public void LinkDeltaToScreenshot(long screenshotId, long deltaFileId)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Screenshots
            SET DeltaFileId = @deltaFileId
            WHERE Id = @screenshotId
        ";

        command.Parameters.AddWithValue("@deltaFileId", deltaFileId);
        command.Parameters.AddWithValue("@screenshotId", screenshotId);

        command.ExecuteNonQuery();
    }

    public long InsertDeltaFile(long screenshotId, string filePath, long fileSize)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO DeltaFiles (ScreenshotId, FilePath, FileSize, CreatedAt)
            VALUES (@screenshotId, @filePath, @fileSize, @createdAt);
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@screenshotId", screenshotId);
        command.Parameters.AddWithValue("@filePath", filePath);
        command.Parameters.AddWithValue("@fileSize", fileSize);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToFileTimeUtc());

        return (long)command.ExecuteScalar()!;
    }

    public long InsertOcrData(long screenshotId, string text, string language, double? confidence)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO OcrData (ScreenshotId, Text, Language, Confidence, ProcessedAt)
            VALUES (@screenshotId, @text, @language, @confidence, @processedAt)
            ON CONFLICT(ScreenshotId) DO UPDATE SET
                Text = excluded.Text,
                Language = excluded.Language,
                Confidence = excluded.Confidence,
                ProcessedAt = excluded.ProcessedAt;
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@screenshotId", screenshotId);
        command.Parameters.AddWithValue("@text", text);
        command.Parameters.AddWithValue("@language", language);
        command.Parameters.AddWithValue("@confidence", confidence ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@processedAt", DateTime.UtcNow.ToFileTimeUtc());

        var ocrId = (long)command.ExecuteScalar()!;

        // Update FTS index
        UpdateFtsIndex(screenshotId);
        return ocrId;
    }

    private void UpdateFtsIndex(long screenshotId)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ScreenshotsFts (ScreenshotId, WindowTitle, OcrText)
            SELECT Id, WindowTitle,
                (SELECT Text FROM OcrData WHERE ScreenshotId = Id LIMIT 1)
            FROM Screenshots
            WHERE Id = @id;
        ";

        command.Parameters.AddWithValue("@id", screenshotId);
        command.ExecuteNonQuery();
    }

    public List<Screenshot> GetScreenshots(DateTime? start = null, DateTime? end = null, string? processName = null, int limit = 100)
    {
        var connection = GetConnection();
        var query = new StringBuilder("SELECT * FROM Screenshots WHERE 1=1");
        var parameters = new List<SqliteParameter>();

        if (start.HasValue)
        {
            query.Append(" AND Timestamp >= @start");
            parameters.Add(new SqliteParameter("@start", start.Value.ToFileTimeUtc()));
        }

        if (end.HasValue)
        {
            query.Append(" AND Timestamp <= @end");
            parameters.Add(new SqliteParameter("@end", end.Value.ToFileTimeUtc()));
        }

        if (!string.IsNullOrEmpty(processName))
        {
            query.Append(" AND ProcessName = @processName");
            parameters.Add(new SqliteParameter("@processName", processName));
        }

        query.Append(" ORDER BY Timestamp DESC LIMIT @limit");
        parameters.Add(new SqliteParameter("@limit", limit));

        using var command = connection.CreateCommand();
        command.CommandText = query.ToString();
        command.Parameters.AddRange(parameters.ToArray());

        var results = new List<Screenshot>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapScreenshot(reader));
        }

        return results;
    }

    public Screenshot? GetScreenshotById(long id)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Screenshots WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapScreenshot(reader);
        }

        return null;
    }

    public Screenshot? GetLastKeyframe()
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT * FROM Screenshots
            WHERE IsKeyframe = 1
            ORDER BY Timestamp DESC
            LIMIT 1
        ";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return MapScreenshot(reader);
        }

        return null;
    }

    public int GetPendingScreenshotsCount()
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Screenshots WHERE Status = @status";
        command.Parameters.AddWithValue("@status", (int)ScreenshotStatus.Pending);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<Screenshot> GetPendingScreenshots(int limit = 10)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT * FROM Screenshots
            WHERE Status = @status
            ORDER BY Timestamp ASC
            LIMIT @limit
        ";

        command.Parameters.AddWithValue("@status", (int)ScreenshotStatus.Pending);
        command.Parameters.AddWithValue("@limit", limit);

        var results = new List<Screenshot>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapScreenshot(reader));
        }

        return results;
    }

    public List<Screenshot> SearchScreenshots(string searchTerm, DateTime? start = null, DateTime? end = null, int limit = 50)
    {
        var connection = GetConnection();
        var query = new StringBuilder(@"
            SELECT s.* FROM Screenshots s
            INNER JOIN ScreenshotsFts fts ON s.Id = fts.ScreenshotId
            WHERE ScreenshotsFts MATCH @searchTerm
        ");
        var parameters = new List<SqliteParameter>
        {
            new("@searchTerm", searchTerm)
        };

        if (start.HasValue)
        {
            query.Append(" AND s.Timestamp >= @start");
            parameters.Add(new SqliteParameter("@start", start.Value.ToFileTimeUtc()));
        }

        if (end.HasValue)
        {
            query.Append(" AND s.Timestamp <= @end");
            parameters.Add(new SqliteParameter("@end", end.Value.ToFileTimeUtc()));
        }

        query.Append(" ORDER BY s.Timestamp DESC LIMIT @limit");
        parameters.Add(new SqliteParameter("@limit", limit));

        using var command = connection.CreateCommand();
        command.CommandText = query.ToString();
        command.Parameters.AddRange(parameters.ToArray());

        var results = new List<Screenshot>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapScreenshot(reader));
        }

        return results;
    }

    public Dictionary<string, long> GetActivityHeatmap(DateTime start, DateTime end)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT ProcessName, SUM(DurationSeconds) as TotalSeconds
            FROM ActivityLog
            WHERE StartTime >= @start AND EndTime <= @end
            GROUP BY ProcessName
            ORDER BY TotalSeconds DESC
        ";

        command.Parameters.AddWithValue("@start", start.ToFileTimeUtc());
        command.Parameters.AddWithValue("@end", end.ToFileTimeUtc());

        var results = new Dictionary<string, long>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var process = reader.GetString(0);
            var duration = reader.GetInt64(1);
            results[process] = duration;
        }

        return results;
    }

    public List<ActivityLog> GetActivityLog(DateTime start, DateTime end, string? processName = null)
    {
        var connection = GetConnection();
        var query = new StringBuilder("SELECT * FROM ActivityLog WHERE StartTime >= @start AND EndTime <= @end");
        var parameters = new List<SqliteParameter>
        {
            new("@start", start.ToFileTimeUtc()),
            new("@end", end.ToFileTimeUtc())
        };

        if (!string.IsNullOrEmpty(processName))
        {
            query.Append(" AND ProcessName = @processName");
            parameters.Add(new SqliteParameter("@processName", processName));
        }

        query.Append(" ORDER BY StartTime DESC");

        using var command = connection.CreateCommand();
        command.CommandText = query.ToString();
        command.Parameters.AddRange(parameters.ToArray());

        var results = new List<ActivityLog>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new ActivityLog
            {
                Id = reader.GetInt64(0),
                ProcessName = reader.GetString(1),
                WindowTitle = reader.GetString(2),
                StartTime = DateTime.FromFileTimeUtc(reader.GetInt64(3)),
                EndTime = DateTime.FromFileTimeUtc(reader.GetInt64(4)),
                DurationSeconds = reader.GetInt64(5),
                ScreenshotCount = reader.GetInt32(6)
            });
        }

        return results;
    }

    public void UpsertActivityLog(ActivityLog log)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ActivityLog (
                ProcessName, WindowTitle, StartTime, EndTime, DurationSeconds, ScreenshotCount
            ) VALUES (
                @processName, @windowTitle, @startTime, @endTime, @duration, @screenshotCount
            )
            ON CONFLICT DO NOTHING;
        ";

        command.Parameters.AddWithValue("@processName", log.ProcessName);
        command.Parameters.AddWithValue("@windowTitle", log.WindowTitle);
        command.Parameters.AddWithValue("@startTime", log.StartTime.ToFileTimeUtc());
        command.Parameters.AddWithValue("@endTime", log.EndTime.ToFileTimeUtc());
        command.Parameters.AddWithValue("@duration", log.DurationSeconds);
        command.Parameters.AddWithValue("@screenshotCount", log.ScreenshotCount);

        command.ExecuteNonQuery();
    }

    public void SaveSetting(string key, string value)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Settings (Key, Value)
            VALUES (@key, @value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
        ";

        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);

        command.ExecuteNonQuery();
    }

    public string? GetSetting(string key)
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Settings WHERE Key = @key";
        command.Parameters.AddWithValue("@key", key);

        var result = command.ExecuteScalar();
        return result?.ToString();
    }

    public long GetTotalStorageUsed()
    {
        var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT SUM(FileSize) FROM Screenshots
            WHERE Status = @optimized
        ";

        command.Parameters.AddWithValue("@optimized", (int)ScreenshotStatus.Optimized);

        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    public void CleanupOldScreenshots(int retentionMonths)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-retentionMonths);
        var connection = GetConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, FilePath FROM Screenshots
                WHERE Timestamp < @cutoff
            ";

            command.Parameters.AddWithValue("@cutoff", cutoffDate.ToFileTimeUtc());

            var filesToDelete = new List<string>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var filePath = reader.GetString(1);
                filesToDelete.Add(filePath);
            }

            // Delete files
            foreach (var file in filesToDelete)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Log error but continue
                }
            }

            // Delete from database
            command.CommandText = @"
                DELETE FROM Screenshots WHERE Timestamp < @cutoff;
                DELETE FROM OcrData WHERE ScreenshotId NOT IN (SELECT Id FROM Screenshots);
                DELETE FROM DeltaFiles WHERE ScreenshotId NOT IN (SELECT Id FROM Screenshots);
            ";

            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static Screenshot MapScreenshot(SqliteDataReader reader)
    {
        return new Screenshot
        {
            Id = reader.GetInt64(0),
            Timestamp = DateTime.FromFileTimeUtc(reader.GetInt64(1)),
            WindowTitle = reader.GetString(2),
            ProcessName = reader.GetString(3),
            FilePath = reader.GetString(4),
            IsKeyframe = reader.GetInt32(5) == 1,
            KeyframeId = reader.IsDBNull(6) ? null : reader.GetInt64(6),
            DeltaFileId = reader.IsDBNull(7) ? null : reader.GetInt64(7),
            Width = reader.GetInt32(8),
            Height = reader.GetInt32(9),
            FileSize = reader.GetInt64(10),
            Status = (ScreenshotStatus)reader.GetInt32(11),
            ProcessedAt = reader.IsDBNull(12) ? null : DateTime.FromFileTimeUtc(reader.GetInt64(12))
        };
    }

    public void Dispose()
    {
        foreach (var connection in _connections.Values)
        {
            connection.Dispose();
        }
        _connections.Clear();
    }
}
