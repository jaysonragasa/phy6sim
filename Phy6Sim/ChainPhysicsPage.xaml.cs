using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using System.Numerics;

namespace Phy6Sim;

public class ChainDrawable : IDrawable
{
	public ChainPhysicsService? SimService { get; set; }

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.DarkBlue;
		canvas.FillRectangle(dirtyRect);

		if (SimService == null) return;

		canvas.StrokeColor = Colors.Gold;
		canvas.StrokeSize = 5;
		canvas.StrokeLineCap = LineCap.Round;
		foreach (var stick in SimService.Sticks)
		{
			canvas.DrawLine(stick.PointA.Position.X, stick.PointA.Position.Y, stick.PointB.Position.X, stick.PointB.Position.Y);
		}

		var firstPoint = SimService.Points.FirstOrDefault();
		if (firstPoint != null)
		{
			canvas.FillColor = Colors.Silver;
			canvas.FillCircle(firstPoint.Position.X, firstPoint.Position.Y, 10);
		}
	}
}


public partial class ChainPhysicsPage : ContentPage
{
	private ChainPhysicsService? _simService;
	private IDispatcherTimer? _gameLoopTimer;
	private bool _isInitialized = false;
	private readonly ChainDrawable _simDrawable = new();
	private PhysicsPoint? _draggedPoint;

	public ChainPhysicsPage()
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
		ToggleGyroscope(true);
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		StopSimulation();
		ToggleGyroscope(false);
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
			_simService = new ChainPhysicsService((float)CanvasView.Width, (float)CanvasView.Height);
			_simService.Initialize(10, 12); // Reduced for watchOS
			_simDrawable.SimService = _simService;
			_isInitialized = true;
		}

		if (!_isInitialized || _simService == null) return;

		_simService.Step();
		CanvasView.Invalidate();
	}

	private void ToggleGyroscope(bool start)
	{
		if (Gyroscope.Default.IsSupported)
		{
			if (start && !Gyroscope.Default.IsMonitoring)
			{
				Gyroscope.Default.ReadingChanged += OnGyroscopeReadingChanged;
				Gyroscope.Default.Start(SensorSpeed.Game);
			}
			else if (!start && Gyroscope.Default.IsMonitoring)
			{
				Gyroscope.Default.Stop();
				Gyroscope.Default.ReadingChanged -= OnGyroscopeReadingChanged;
			}
		}
	}

	private void OnGyroscopeReadingChanged(object sender, GyroscopeChangedEventArgs e)
	{
		if (_simService == null) return;

		// Gyroscope measures angular velocity. We use it to simulate the anchor point "moving".
		// The Y-axis angular velocity (yaw) corresponds to side-to-side rotation.
		float swayForce = e.Reading.AngularVelocity.Y * -1.5f;
		float downwardForce = 1.0f; // A constant pull downwards

		_simService.SetGravity(swayForce, downwardForce);
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

