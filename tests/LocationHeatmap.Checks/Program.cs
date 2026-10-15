using LocationHeatmap.Core;

static void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    Console.WriteLine($"PASS {description}");
}

string path = Path.Combine(Path.GetTempPath(), $"heatmap-check-{Guid.NewGuid():N}.db3");
var repository = new LocationRepository(path);
try
{
    Check(HeatmapCalculator.Calculate([]).Count == 0, "Empty history has no heat cells");
    IReadOnlyList<LocationSample> route = DemoRoute.Create();
    foreach (LocationSample sample in route) await repository.SaveAsync(sample);
    await repository.CloseAsync();
    repository = new LocationRepository(path);
    List<LocationSample> restored = await repository.GetRecentAsync();
    Check(restored.Count == 141, "141 demo samples persist across database reopen");
    Check(restored[0].Latitude == route[0].Latitude && restored[^1].TimestampUtcTicks == route[^1].TimestampUtcTicks,
        "Coordinates and UTC timestamps survive persistence");
    Check((await repository.GetRecentAsync(10)).Count == 10, "History query respects rendering limit");
    IReadOnlyList<HeatCell> cells = HeatmapCalculator.Calculate(restored);
    Check(cells.All(cell => cell.Intensity is > 0 and <= 1), "Density is normalized to zero through one");
    Check(cells.Any(cell => cell.Intensity < 0.12) && cells.Any(cell => cell.Intensity > 0.9), "Route has low density and hot cells");
    var dense = restored.Where(point => point.Latitude == restored[29].Latitude).ToList();
    Check(dense.Count >= 28, "Stationary visits are retained to show dwell density");
    try
    {
        await repository.SaveAsync(new LocationSample { Latitude = double.NaN });
        throw new InvalidOperationException("Invalid coordinates accepted");
    }
    catch (ArgumentOutOfRangeException) { Console.WriteLine("PASS Invalid coordinates are rejected"); }
    await repository.ClearAsync();
    Check((await repository.GetRecentAsync()).Count == 0, "Clear removes saved samples");
}
finally
{
    await repository.CloseAsync();
    File.Delete(path);
}
