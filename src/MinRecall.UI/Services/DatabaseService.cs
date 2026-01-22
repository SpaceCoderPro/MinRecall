using MinRecall.Core.Database;
using MinRecall.Core.Models;

namespace MinRecall.UI.Services;

public class DatabaseService
{
    private readonly DatabaseManager _database;
    private static DatabaseService? _instance;
    private static readonly object _lock = new();

    public static DatabaseService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        var dbPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "MinRecall",
                            "minrecall.db"
                        );
                        _instance = new DatabaseService(dbPath);
                    }
                }
            }
            return _instance;
        }
    }

    public DatabaseService(string dbPath)
    {
        _database = new DatabaseManager(dbPath);
    }

    public List<Screenshot> GetScreenshots(DateTime? start = null, DateTime? end = null, string? processName = null, int limit = 100)
    {
        return _database.GetScreenshots(start, end, processName, limit);
    }

    public Screenshot? GetScreenshotById(long id)
    {
        return _database.GetScreenshotById(id);
    }

    public List<Screenshot> SearchScreenshots(string searchTerm, DateTime? start = null, DateTime? end = null, int limit = 50)
    {
        return _database.SearchScreenshots(searchTerm, start, end, limit);
    }

    public Dictionary<string, long> GetActivityHeatmap(DateTime start, DateTime end)
    {
        return _database.GetActivityHeatmap(start, end);
    }

    public List<ActivityLog> GetActivityLog(DateTime start, DateTime end, string? processName = null)
    {
        return _database.GetActivityLog(start, end, processName);
    }

    public string? GetSetting(string key)
    {
        return _database.GetSetting(key);
    }

    public void SaveSetting(string key, string value)
    {
        _database.SaveSetting(key, value);
    }

    public string DatabasePath => _database.DatabasePath;
}
