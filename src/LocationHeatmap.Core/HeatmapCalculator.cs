namespace LocationHeatmap.Core;

public sealed record HeatCell(double Latitude, double Longitude, double Intensity);

/// <summary>Local meter grid with a small spatial smoothing kernel.</summary>
public static class HeatmapCalculator
{
    public const double CellMeters = 65;

    public static IReadOnlyList<HeatCell> Calculate(IReadOnlyList<LocationSample> samples)
    {
        if (samples.Count == 0) return [];
        double originLatitude = samples[0].Latitude;
        double originLongitude = samples[0].Longitude;
        double longitudeScale = 111320 * Math.Max(0.01, Math.Cos(originLatitude * Math.PI / 180));
        var density = new Dictionary<(int X, int Y), double>();

        // Bin samples, then distribute each fix across nine nearby cells.
        // Repeated fixes at one place increase density even with no movement.
        foreach (LocationSample sample in samples)
        {
            double deltaLongitude = (sample.Longitude - originLongitude + 540) % 360 - 180;
            int x = (int)Math.Floor(deltaLongitude * longitudeScale / CellMeters);
            int y = (int)Math.Floor((sample.Latitude - originLatitude) * 111320 / CellMeters);
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var key = (x + dx, y + dy);
                double weight = Math.Exp(-(dx * dx + dy * dy));
                density[key] = density.GetValueOrDefault(key) + weight;
            }
        }

        double maximum = density.Values.Max();
        return density.OrderBy(pair => pair.Value).Select(pair => new HeatCell(
            originLatitude + (pair.Key.Y + 0.5) * CellMeters / 111320,
            ((originLongitude + (pair.Key.X + 0.5) * CellMeters / longitudeScale + 540) % 360) - 180,
            pair.Value / maximum)).ToArray();
    }
}
