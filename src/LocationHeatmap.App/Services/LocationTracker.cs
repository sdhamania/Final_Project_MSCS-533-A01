using LocationHeatmap.Core;

namespace LocationHeatmap.App.Services;

/// <summary>Cancellable foreground polling. No background location permission is requested.</summary>
public sealed class LocationTracker(LocationRepository repository)
{
    public async Task RunAsync(Func<Task> onSaved, Action<string> onStatus, CancellationToken token)
    {
        var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(8));
        while (!token.IsCancellationRequested)
        {
            Location? location = await Geolocation.Default.GetLocationAsync(request, token);
            token.ThrowIfCancellationRequested();
            if (location is null || location.Accuracy is null || location.Accuracy > 100)
            {
                onStatus("Waiting for an accurate GPS fix...");
            }
            else
            {
                await repository.SaveAsync(new LocationSample
                {
                    Latitude = location.Latitude, Longitude = location.Longitude,
                    AccuracyMeters = location.Accuracy.Value,
                    TimestampUtcTicks = location.Timestamp.UtcDateTime.Ticks
                });
                await onSaved();
                onStatus($"Tracking every 10s | accuracy {location.Accuracy:0} m");
            }
            // Sequential awaits prevent overlapping GPS requests and writes.
            await Task.Delay(TimeSpan.FromSeconds(10), token);
        }
    }
}
