using Microsoft.Maui.Graphics;

namespace Phy6Sim;

public partial class FishSimPage : ContentPage
{
	private FishTankRenderer? _fishTankRenderer;
	private IDispatcherTimer? _gameLoopTimer;
	private bool _isInitialized = false;

	public FishSimPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		StartSimulation();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		StopSimulation();
	}

	private void StartSimulation()
	{
		_fishTankRenderer = new FishTankRenderer();
		CanvasView.Drawable = _fishTankRenderer;

		_gameLoopTimer = Dispatcher.CreateTimer();
		_gameLoopTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
		_gameLoopTimer.Tick += (s, e) => 
		{
			if (_fishTankRenderer != null)
			{
				_fishTankRenderer.Update();
				CanvasView.Invalidate();
			}
		};
		_gameLoopTimer.Start();
	}

	private void StopSimulation()
	{
		_gameLoopTimer?.Stop();
	}

	private async void BackButton_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}