using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using System.Numerics;

namespace Phy6Sim;

public class LiquidSimDrawable : IDrawable
{
	public LiquidSimService? SimService { get; set; }

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.DarkSlateGray;
		canvas.FillRectangle(dirtyRect);

		if (SimService == null) return;

		canvas.FillColor = Colors.Aqua;
		foreach (var particle in SimService.Particles)
		{
			canvas.FillCircle(particle.Position.X, particle.Position.Y, particle.Radius);
		}
	}
}


public partial class LiquidSimPage : ContentPage
{
	private LiquidSimService? _simService;
	private IDispatcherTimer? _gameLoopTimer;
	private bool _isInitialized = false;
	private readonly LiquidSimDrawable _simDrawable = new();

	public LiquidSimPage()
	{
		InitializeComponent();
		CanvasView.Drawable = _simDrawable;
		
		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += OnTapped;
		CanvasView.GestureRecognizers.Add(tapGesture);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		StartSimulation();
		ToggleAccelerometer(true);
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		StopSimulation();
		ToggleAccelerometer(false);
	}

	private void StartSimulation()
	{
		// The timer is now started immediately. Initialization happens in the tick.
		_gameLoopTimer = Dispatcher.CreateTimer();
		_gameLoopTimer.Interval = TimeSpan.FromMilliseconds(33); // ~30 FPS for watchOS
		_gameLoopTimer.Tick += (s, e) => GameLoopTick();
		_gameLoopTimer.Start();
	}

	private void StopSimulation()
	{
		_gameLoopTimer?.Stop();
	}

	private void GameLoopTick()
	{
		// Initialization logic is moved here to ensure CanvasView has a size.
		if (!_isInitialized && CanvasView.Width > 0)
		{
			_simService = new LiquidSimService((float)CanvasView.Width, (float)CanvasView.Height);
			_simService.Initialize(6, 4, 8f); // Reduced for watchOS
			_simDrawable.SimService = _simService;
			_isInitialized = true;
		}

		if (!_isInitialized || _simService == null) return;

		_simService.Step();
		CanvasView.Invalidate();
	}

	private void ToggleAccelerometer(bool start)
	{
		if (Accelerometer.Default.IsSupported)
		{
			if (start && !Accelerometer.Default.IsMonitoring)
			{
				Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
				Accelerometer.Default.Start(SensorSpeed.Game);
			}
			else if (!start && Accelerometer.Default.IsMonitoring)
			{
				Accelerometer.Default.Stop();
				Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
			}
		}
	}

	private void OnAccelerometerReadingChanged(object sender, AccelerometerChangedEventArgs e)
	{
		if (_simService == null) return;

		float gravityX = -e.Reading.Acceleration.X;
		float gravityY = e.Reading.Acceleration.Y;

		_simService.SetGravity(gravityX, gravityY);
	}

	private void OnTapped(object sender, TappedEventArgs e)
	{
		if (_simService == null) return;
		
		var pos = e.GetPosition(CanvasView);
		if (pos.HasValue)
		{
			var tapPoint = new Vector2((float)pos.Value.X, (float)pos.Value.Y);
			foreach (var particle in _simService.Particles)
			{
				float dx = particle.Position.X - tapPoint.X;
				float dy = particle.Position.Y - tapPoint.Y;
				float distSq = dx * dx + dy * dy;
				if (distSq < 2500)
				{
					var impulse = new Vector2(dx, dy) * 0.1f;
					particle.Velocity += impulse;
				}
			}
		}
	}

	private async void BackButton_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}

