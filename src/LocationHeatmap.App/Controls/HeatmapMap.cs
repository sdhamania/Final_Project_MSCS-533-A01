using LocationHeatmap.Core;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using IMap = Microsoft.Maui.Maps.IMap;

namespace LocationHeatmap.App.Controls;

/// <summary>Extends MAUI's native Map with portable colored circle overlays.</summary>
public sealed class HeatmapMap : Microsoft.Maui.Controls.Maps.Map
{
    public bool IsUpdatingElements { get; private set; }

    static HeatmapMap()
    {
        // MAUI normally rebuilds the native overlay after every collection change.
        // Defer those intermediate updates and publish the completed layer once.
        MapHandler.Mapper.ModifyMapping(nameof(IMap.Elements), (handler, map, update) =>
        {
            if (map is not HeatmapMap { IsUpdatingElements: true })
            {
                update?.Invoke(handler, map);
            }
        });
    }

    public HeatmapMap() : base(MapSpan.FromCenterAndRadius(
        new Location(37.382, -122.060), Distance.FromKilometers(3)))
    {
        MapType = MapType.Street;
    }

    public void Render(IReadOnlyList<LocationSample> samples, bool center)
    {
        IsUpdatingElements = true;
        try
        {
            MapElements.Clear();
            foreach (HeatCell cell in HeatmapCalculator.Calculate(samples))
            {
                MapElements.Add(new Circle
                {
                    Center = new Location(cell.Latitude, cell.Longitude),
                    Radius = Distance.FromMeters(HeatmapCalculator.CellMeters * 0.9),
                    StrokeWidth = 0,
                    FillColor = HeatPalette.ColorFor(cell.Intensity).WithAlpha(0.48f)
                });
            }
        }
        finally
        {
            IsUpdatingElements = false;
            Handler?.UpdateValue(nameof(IMap.Elements));
        }
        if (center && samples.Count > 0)
        {
            var midpoint = new Location(
                (samples.Min(sample => sample.Latitude) + samples.Max(sample => sample.Latitude)) / 2,
                (samples.Min(sample => sample.Longitude) + samples.Max(sample => sample.Longitude)) / 2);
            double radius = Math.Max(0.5, samples.Max(sample => Location.CalculateDistance(
                midpoint, new Location(sample.Latitude, sample.Longitude), DistanceUnits.Kilometers)) + 0.3);
            MoveToRegion(MapSpan.FromCenterAndRadius(
                midpoint, Distance.FromKilometers(radius)));
        }
    }
}

public static class HeatPalette
{
    public static Color ColorFor(double intensity) => intensity switch
    {
        < 0.12 => Color.FromArgb("#1677FF"),
        < 0.30 => Color.FromArgb("#00BCD4"),
        < 0.50 => Color.FromArgb("#55C85A"),
        < 0.72 => Color.FromArgb("#FFD43B"),
        < 0.90 => Color.FromArgb("#FF8B26"),
        _ => Color.FromArgb("#EF3440")
    };
}
