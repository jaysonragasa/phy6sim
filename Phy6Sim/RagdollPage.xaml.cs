using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using System.Numerics;

namespace Phy6Sim;

public class RagdollDrawable : IDrawable
{
	public RagdollService? SimService { get; set; }

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.AntiqueWhite;
		canvas.FillRectangle(dirtyRect);

		if (SimService == null) return;

		// Draw the sticks (limbs)
		canvas.StrokeColor = Colors.Black;
		canvas.StrokeSize = 4;
		foreach (var stick in SimService.Sticks)
		{
			canvas.DrawLine(stick.PointA.Position.X, stick.PointA.Position.Y, stick.PointB.Position.X, stick.PointB.Position.Y);
		}

		// Draw the points (joints)
		canvas.FillColor = Colors.DarkRed;
		foreach (var point in SimService.Points)
		{
			// Make the head bigger
			float radius = (SimService.Points.IndexOf(point) == 0) ? 15f : 5f;
			canvas.FillCircle(point.Position.X, point.Position.Y, radius);
		}
	}
}

public partial class RagdollPage : ContentPage
{
	private RagdollService? _simService;
	private IDispatcherTimer? _gameLoopTimer;
	private bool _isInitialized = false;
	private readonly RagdollDrawable _simDrawable = new();
	private const double ShakeThreshold = 3.0;
	private RagdollPoint? _draggedPoint;

	public RagdollPage()
	{
		InitializeComponent();
		CanvasView.Drawable = _simDrawable;
		
		var panGesture = new PanGestureRecognizer();
		panGesture.PanUpdated += OnPanUpdated;
		CanvasView.GestureRecognizers.Add(panGesture);
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
			_simService = new RagdollService((float)CanvasView.Width, (float)CanvasView.Height);
			_simService.Initialize();
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

		var data = e.Reading;
		float gravityX = -data.Acceleration.X;
		float gravityY = data.Acceleration.Y;
		_simService.SetGravity(gravityX, gravityY);

		// Shake detection
		if (data.Acceleration.LengthSquared() > ShakeThreshold * ShakeThreshold)
		{
			var random = new Random();
			var impulse = new Vector2(
				(float)(random.NextDouble() * 50 - 25),
				(float)(random.NextDouble() * 50 - 25)
			);
			_simService.ApplyImpulse(impulse);
		}
	}

	private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
	{
		if (_simService == null) return;

		switch (e.StatusType)
		{
			case GestureStatus.Started:
				var startPoint = new Vector2((float)CanvasView.Width / 2, (float)CanvasView.Height / 2);
				_draggedPoint = _simService.GetPointAtPosition(startPoint);
				break;

			case GestureStatus.Running:
				if (_draggedPoint != null)
				{
					var currentPos = _draggedPoint.Position + new Vector2((float)e.TotalX * 0.1f, (float)e.TotalY * 0.1f);
					_simService.DragPoint(_draggedPoint, currentPos);
				}
				break;

			case GestureStatus.Completed:
			case GestureStatus.Canceled:
				_draggedPoint = null;
				break;
		}
	}

	private async void BackButton_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}

