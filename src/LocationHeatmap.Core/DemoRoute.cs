namespace LocationHeatmap.Core;

/// <summary>Synthetic route for reproducible demonstrations; never a user's history.</summary>
public static class DemoRoute
{
    public static IReadOnlyList<LocationSample> Create()
    {
        var result = new List<LocationSample>();
        DateTime start = new(2026, 10, 11, 15, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 85; i++)
        {
            double fraction = i / 84.0;
            result.Add(new LocationSample
            {
                Latitude = 37.392 - 0.019 * fraction + 0.001 * Math.Sin(fraction * Math.PI * 2),
                Longitude = -122.081 + 0.042 * fraction,
                AccuracyMeters = 5,
                TimestampUtcTicks = start.AddSeconds(result.Count * 10).Ticks
            });
            // Simulated pauses produce visibly hotter regions along the route.
            if (i is 28 or 55)
            {
                for (int pause = 0; pause < 28; pause++)
                {
                    LocationSample point = result[^1];
                    result.Add(new LocationSample
                    {
                        Latitude = point.Latitude, Longitude = point.Longitude,
                        AccuracyMeters = 5, TimestampUtcTicks = start.AddSeconds(result.Count * 10).Ticks
                    });
                }
            }
        }
        return result;
    }
}
