using LocationHeatmap.App.Controls;
using LocationHeatmap.App.Services;
using LocationHeatmap.Core;
using SQLite;

namespace LocationHeatmap.App;

public sealed class MainPage : ContentPage
{
    private readonly LocationRepository _live = new(Path.Combine(FileSystem.AppDataDirectory, "locations.db3"));
    private readonly LocationRepository _demo = new(Path.Combine(FileSystem.AppDataDirectory, "demo.db3"));
    private readonly HeatmapMap _map = new();
    private readonly Label _status = new() { FontSize = 12, TextColor = Color.FromArgb("#625E70") };
    private readonly Label _count = new() { FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#29243D") };
    private readonly Button _start = new() { Text = "Start tracking", BackgroundColor = Color.FromArgb("#512BD4"), TextColor = Colors.White };
    private readonly Button _stop = new() { Text = "Stop", IsEnabled = false };
    private CancellationTokenSource? _tracking;
    private Task? _trackingTask;
    private bool _demoMode;
    private bool _busy;

    public MainPage()
    {
        Title = "MainView";
        BackgroundColor = Color.FromArgb("#FAF9FC");
        var header = new VerticalStackLayout
        {
            Padding = new Thickness(20, 12), BackgroundColor = Color.FromArgb("#512BD4"),
            Children =
            {
                new Label { Text = "MainView", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = "LOCATION HEAT MAP", FontSize = 11, TextColor = Color.FromArgb("#E0D6FF") }
            }
        };
        var maps = new Grid();
        maps.Add(_map);
        var legend = new HorizontalStackLayout { Spacing = 7, HorizontalOptions = LayoutOptions.Center };
        legend.Children.Add(new Label { Text = "Low", FontSize = 11 });
        foreach (double intensity in new[] { 0.05, 0.2, 0.4, 0.6, 0.8, 1.0 })
        {
            legend.Children.Add(new BoxView { WidthRequest = 22, HeightRequest = 9, Color = HeatPalette.ColorFor(intensity), VerticalOptions = LayoutOptions.Center });
        }
        legend.Children.Add(new Label { Text = "High density", FontSize = 11 });
        var demo = new Button { Text = "Load demo", FontSize = 12 };
        var clear = new Button { Text = "Clear", FontSize = 12 };
        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)), ColumnSpacing = 10 };
        buttons.Add(_start, 0); buttons.Add(_stop, 1);
        var secondary = new HorizontalStackLayout { Spacing = 10, Children = { demo, clear } };
        var footer = new VerticalStackLayout
        {
            Padding = new Thickness(18, 12), Spacing = 7,
            Children = { legend, _count, _status, buttons, secondary,
                new Label { Text = "Saved on this device in SQLite. Tracking stops when the app is hidden.", FontSize = 11, TextColor = Color.FromArgb("#625E70") } }
        };
        var layout = new Grid { RowDefinitions = new RowDefinitionCollection(new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto)) };
        layout.Add(header, 0, 0); layout.Add(maps, 0, 1); layout.Add(footer, 0, 2);
        Content = layout;
        _start.Clicked += async (_, _) => await StartAsync();
        _stop.Clicked += async (_, _) => await StopAsync();
        demo.Clicked += async (_, _) => await LoadDemoAsync();
        clear.Clicked += async (_, _) => await ClearAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { await RefreshAsync(true); SetStatus("Ready | Load demo or start GPS tracking"); }
        catch (SQLiteException) { SetStatus("Could not open the location database."); }
    }

    protected override void OnDisappearing()
    {
        _tracking?.Cancel();
        base.OnDisappearing();
    }

    public void SuspendTracking() => _tracking?.Cancel();

    private void SetStatus(string text) => MainThread.BeginInvokeOnMainThread(() => _status.Text = text);

    private async Task RefreshAsync(bool center = false)
    {
        List<LocationSample> samples = await (_demoMode ? _demo : _live).GetRecentAsync();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _count.Text = $"{samples.Count} saved locations";
            // Both recorded GPS fixes and sample data use the native MAUI Map.
            _map.Render(samples, center);
        });
    }

    private async Task StartAsync()
    {
        if (_busy || _trackingTask is { IsCompleted: false }) return;
        _busy = true;
        try
        {
            PermissionStatus permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted) { SetStatus("Location permission denied. Enable it in Settings."); return; }
            _demoMode = false;
            await RefreshAsync(true);
            _tracking = new CancellationTokenSource();
            _start.IsEnabled = false; _stop.IsEnabled = true;
            SetStatus("Acquiring GPS location...");
            _trackingTask = TrackAsync(_tracking.Token);
        }
        catch (PermissionException) { SetStatus("Location permission is unavailable."); }
        catch (SQLiteException) { SetStatus("Database unavailable. Tracking was not started."); }
        finally { _busy = false; }
    }

    private async Task TrackAsync(CancellationToken token)
    {
        try { await new LocationTracker(_live).RunAsync(() => RefreshAsync(true), SetStatus, token); }
        catch (OperationCanceledException) { SetStatus("Tracking stopped | history saved"); }
        catch (FeatureNotEnabledException) { SetStatus("Turn on device location services, then start again."); }
        catch (FeatureNotSupportedException) { SetStatus("GPS is not supported on this device."); }
        catch (PermissionException) { SetStatus("Location permission was revoked."); }
        catch (SQLiteException) { SetStatus("Database write failed. Tracking stopped."); }
        catch (Exception exception) when (exception is IOException or TimeoutException)
        { SetStatus("GPS request failed. Try starting again."); }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() => { _start.IsEnabled = true; _stop.IsEnabled = false; });
            _tracking?.Dispose(); _tracking = null;
        }
    }

    private async Task StopAsync()
    {
        _tracking?.Cancel();
        if (_trackingTask is not null) await _trackingTask;
    }

    private async Task LoadDemoAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            await StopAsync();
            await _demo.ClearAsync();
            foreach (LocationSample sample in DemoRoute.Create()) await _demo.SaveAsync(sample);
            _demoMode = true;
            await RefreshAsync(true);
            SetStatus("Demo | 141 synthetic GPS fixes from SQLite");
        }
        catch (SQLiteException) { SetStatus("Could not load the demonstration database."); }
        finally { _busy = false; }
    }

    private async Task ClearAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (!await DisplayAlertAsync("Clear saved locations", "Delete the displayed history from this device?", "Delete", "Cancel")) return;
            await StopAsync();
            await (_demoMode ? _demo : _live).ClearAsync();
            await RefreshAsync(); SetStatus("History cleared");
        }
        catch (SQLiteException) { SetStatus("Could not clear the location database."); }
        finally { _busy = false; }
    }
}
