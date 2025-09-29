using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using System.Numerics;

namespace Phy6Sim;

/// <summary>
/// A dedicated class that handles all drawing logic for our physics simulation.
/// It implements IDrawable, which is required by the MAUI GraphicsView.
/// </summary>
public class PhysicsDrawable : IDrawable
{
	public PhysicsService? PhysicsService { get; set; }

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.Black;
		canvas.FillRectangle(dirtyRect);

		if (PhysicsService == null || !PhysicsService.Bodies.Any()) return;

		foreach (var body in PhysicsService.Bodies)
		{
			canvas.FillColor = body.Color;
			DrawBody(canvas, body);
		}
	}

	private void DrawBody(ICanvas canvas, PhysicsBody body)
	{
		canvas.SaveState();

		// Move the canvas origin to the body's position and apply rotation
		canvas.Translate(body.Position.X, body.Position.Y);
		// Correctly convert radians to degrees for the Rotate method
		canvas.Rotate(body.Angle * (180f / MathF.PI));

		if (body.Shape == ShapeType.Circle)
		{
			canvas.FillCircle(0, 0, body.Radius);
		}
		else if (body.Shape == ShapeType.Box)
		{
			// Draw a rectangle centered at the new origin (0,0)
			canvas.FillRectangle(-body.Radius, -body.Radius, body.Radius * 2, body.Radius * 2);
		}

		canvas.RestoreState();
	}
}


public partial class ZeroGravityPage : ContentPage
{
	private PhysicsService? _physicsService;
	private IDispatcherTimer? _gameLoopTimer;
	private bool _isInitialized = false;
	private readonly PhysicsDrawable _physicsDrawable = new();
	private PhysicsBody? _draggedBody;
	private Vector2 _lastTouchPosition;

	public ZeroGravityPage()
	{
		InitializeComponent();
		CanvasView.Drawable = _physicsDrawable;
		
		// Add touch handling
		var panGesture = new PanGestureRecognizer();
		panGesture.PanUpdated += OnPanUpdated;
		CanvasView.GestureRecognizers.Add(panGesture);
		
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
		// The ONLY change in this method is to REMOVE the initialization logic from here.
		// We just set up and start the timer.

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
		// ADD this block here. It will run on every tick until the view is ready.
		if (!_isInitialized && CanvasView.Width > 0)
		{
			_physicsService = new PhysicsService((float)CanvasView.Width, (float)CanvasView.Height);
			_physicsService.Initialize(6); // Reduced for watchOS
			_physicsDrawable.PhysicsService = _physicsService;
			_isInitialized = true;
		}

		// This part remains the same.
		if (!_isInitialized || _physicsService == null) return;

		_physicsService.Step();
		CanvasView.Invalidate(); // Trigger a redraw on the GraphicsView
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
		if (_physicsService == null) return;

		// Invert accelerometer to get proper gravity direction
		float gravityX = -e.Reading.Acceleration.X;
		float gravityY = e.Reading.Acceleration.Y;

		_physicsService.SetGravity(gravityX, gravityY);
	}

	private void OnTapped(object sender, TappedEventArgs e)
	{
		if (_physicsService == null) return;
		
		var pos = e.GetPosition(CanvasView);
		if (pos.HasValue)
		{
			var point = new Vector2((float)pos.Value.X, (float)pos.Value.Y);
			var body = _physicsService.GetBodyAtPoint(point);
			if (body != null)
			{
				body.Velocity += new Vector2((float)(new Random().NextDouble() * 200 - 100), -150);
			}
		}
	}

	private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
	{
		if (_physicsService == null) return;

		switch (e.StatusType)
		{
			case GestureStatus.Started:
				// Use center of canvas as approximation for touch start
				var startPoint = new Vector2((float)CanvasView.Width / 2, (float)CanvasView.Height / 2);
				_draggedBody = _physicsService.GetBodyAtPoint(startPoint);
				if (_draggedBody != null)
				{
					_physicsService.StartDrag(_draggedBody);
					_lastTouchPosition = startPoint;
				}
				break;

			case GestureStatus.Running:
				if (_draggedBody != null)
				{
					// Update position based on pan delta
					var currentPos = _lastTouchPosition + new Vector2((float)e.TotalX, (float)e.TotalY);
					_physicsService.UpdateDrag(_draggedBody, currentPos);
				}
				break;

			case GestureStatus.Completed:
			case GestureStatus.Canceled:
				if (_draggedBody != null)
				{
					var velocity = new Vector2((float)e.TotalX, (float)e.TotalY) * 0.1f;
					_physicsService.EndDrag(_draggedBody, velocity);
					_draggedBody = null;
				}
				break;
		}
	}

	private async void BackButton_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}

