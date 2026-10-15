using SQLite;

namespace LocationHeatmap.Core;

/// <summary>Serializes initialization and uses parameterized SQLite operations.</summary>
public sealed class LocationRepository(string databasePath)
{
    static LocationRepository()
    {
        // sqlite-net-base leaves native provider initialization to the application.
        SQLitePCL.Batteries_V2.Init();
    }

    private readonly SQLiteAsyncConnection _database = new(databasePath);
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private bool _initialized;

    public async Task InitializeAsync()
    {
        await _initializationGate.WaitAsync();
        try
        {
            if (_initialized) return;
            await _database.CreateTableAsync<LocationSample>();
            _initialized = true;
        }
        finally { _initializationGate.Release(); }
    }

    public async Task SaveAsync(LocationSample sample)
    {
        if (!double.IsFinite(sample.Latitude) || !double.IsFinite(sample.Longitude) ||
            Math.Abs(sample.Latitude) > 90 || Math.Abs(sample.Longitude) > 180 ||
            !double.IsFinite(sample.AccuracyMeters) || sample.AccuracyMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sample), "Invalid GPS coordinates or accuracy.");
        }
        await InitializeAsync();
        await _database.InsertAsync(sample);
    }

    // Keep rendering bounded, while retaining the complete history on disk.
    public async Task<List<LocationSample>> GetRecentAsync(int limit = 2000)
    {
        if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit));
        await InitializeAsync();
        var rows = await _database.Table<LocationSample>()
            .OrderByDescending(row => row.TimestampUtcTicks).Take(limit).ToListAsync();
        rows.Reverse();
        return rows;
    }

    public async Task ClearAsync()
    {
        await InitializeAsync();
        await _database.DeleteAllAsync<LocationSample>();
    }

    public Task CloseAsync() => _database.CloseAsync();
}
