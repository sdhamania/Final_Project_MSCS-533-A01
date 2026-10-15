using Microsoft.Extensions.DependencyInjection;

namespace LocationHeatmap.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var page = new MainPage();
		var window = new Window(page);
		window.Stopped += (_, _) => page.SuspendTracking();
		window.Destroying += (_, _) => page.SuspendTracking();
		return window;
	}
}
