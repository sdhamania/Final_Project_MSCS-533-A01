using SQLite;

namespace LocationHeatmap.Core;

/// <summary>A persisted GPS fix. UTC ticks avoid local time ambiguity.</summary>
public sealed class LocationSample
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AccuracyMeters { get; set; }
    [Indexed]
    public long TimestampUtcTicks { get; set; }
}
