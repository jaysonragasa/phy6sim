using Microsoft.Maui.Graphics;
using System.Numerics;

namespace Phy6Sim;

public class StarfieldDrawable : IDrawable
{
	public StarfieldService? GameService { get; set; }

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.FillColor = Colors.Black;
		canvas.FillRectangle(dirtyRect);

		if (GameService == null) return;

		// Draw stars
		canvas.FillColor = Colors.White;
		foreach (var star in GameService.Stars)
		{
			canvas.FillCircle(star.Position.X, star.Position.Y, star.Size);
		}

		// Draw asteroids
		foreach (var asteroid in GameService.Asteroids)
		{
			canvas.SaveState();
			canvas.Translate(asteroid.Position.X, asteroid.Position.Y);
			canvas.Rotate(asteroid.Rotation * 57.3f); // Convert to degrees
			canvas.FillColor = asteroid.Color;
			
			// Draw irregular asteroid shape
			var points = new PointF[6];
			for (int i = 0; i < 6; i++)
			{
				var angle = i * MathF.PI / 3;
				var radius = asteroid.Radius * (0.8f + 0.4f * MathF.Sin(i));
				points[i] = new PointF(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
			}
			
			var path = new PathF();
			path.MoveTo(points[0]);
			for (int i = 1; i < points.Length; i++)
				path.LineTo(points[i]);
			path.Close();
			canvas.FillPath(path);
			canvas.RestoreState();
		}

		// Draw power-ups
		foreach (var powerUp in GameService.PowerUps)
		{
			canvas.FillColor = powerUp.Color;
			canvas.FillCircle(powerUp.Position.X, powerUp.Position.Y, 8f);
			canvas.StrokeColor = Colors.White;
			canvas.StrokeSize = 1f;
			canvas.DrawCircle(powerUp.Position.X, powerUp.Position.Y, 8f);
		}

		// Draw particles
		foreach (var particle in GameService.Particles)
		{
			canvas.FillColor = particle.Color.WithAlpha(particle.Life);
			canvas.FillCircle(particle.Position.X, particle.Position.Y, particle.Size);
		}

		// Draw bullets
		canvas.FillColor = Colors.Yellow;
		foreach (var bullet in GameService.Bullets)
		{
			canvas.FillCircle(bullet.Position.X, bullet.Position.Y, 2f);
		}

		// Draw ship
		var ship = GameService.Ship;
		canvas.SaveState();
		canvas.Translate(ship.Position.X, ship.Position.Y);
		canvas.Rotate(ship.Rotation * 57.3f);
		
		// Ship color based on power-up
		canvas.StrokeColor = ship.ActivePowerUp switch
		{
			PowerUpType.DoubleBullet => Colors.Green,
			PowerUpType.Laser => Colors.Red,
			PowerUpType.Shower => Colors.Blue,
			PowerUpType.Bomb => Colors.Purple,
			_ => Colors.Cyan
		};
		canvas.StrokeSize = 2f;
		
		var shipPath = new PathF();
		shipPath.MoveTo(ship.Size, 0);
		shipPath.LineTo(-ship.Size * 0.6f, -ship.Size * 0.6f);
		shipPath.LineTo(-ship.Size * 0.3f, 0);
		shipPath.LineTo(-ship.Size * 0.6f, ship.Size * 0.6f);
		shipPath.Close();
		canvas.DrawPath(shipPath);
		canvas.RestoreState();
		
		// Draw life bar (centered at bottom)
		var centerX = dirtyRect.Center.X;
		var lifeBarY = dirtyRect.Height - 30;
		canvas.FillColor = Colors.Red;
		canvas.FillRectangle(centerX - 50, lifeBarY, ship.Lives * 10f, 4f);
		canvas.StrokeColor = Colors.White;
		canvas.StrokeSize = 1f;
		canvas.DrawRectangle(centerX - 50, lifeBarY, 100f, 4f);
		
		// Draw power-up timer (centered above life bar)
		if (ship.ActivePowerUp.HasValue)
		{
			var timerY = lifeBarY - 8;
			canvas.FillColor = Colors.Yellow;
			canvas.FillRectangle(centerX - 50, timerY, ship.PowerUpTimer * 6.67f, 3f);
			canvas.StrokeColor = Colors.White;
			canvas.DrawRectangle(centerX - 50, timerY, 100f, 3f);
		}
		
		// Draw score at top center
		canvas.FontColor = Colors.White;
		canvas.FontSize = 12f;
		canvas.DrawString("Score", dirtyRect.Center.X, 15, HorizontalAlignment.Center);
		canvas.FontSize = 16f;
		canvas.DrawString(ship.Score.ToString(), dirtyRect.Center.X, 30, HorizontalAlignment.Center);
		
		// Draw game over overlay
		if (ship.Lives <= 0)
		{
			canvas.FillColor = Colors.Black.WithAlpha(0.7f);
			canvas.FillRectangle(dirtyRect);
			
			canvas.FontColor = Colors.Red;
			canvas.FontSize = 24f;
			canvas.DrawString("GAME OVER", dirtyRect.Center.X, dirtyRect.Center.Y - 10, HorizontalAlignment.Center);
			
			canvas.FontColor = Colors.White;
			canvas.FontSize = 12f;
			canvas.DrawString("Tap to restart", dirtyRect.Center.X, dirtyRect.Center.Y + 15, HorizontalAlignment.Center);
			
			canvas.FontSize = 14f;
			canvas.DrawString($"Final Score: {ship.Score}", dirtyRect.Center.X, dirtyRect.Center.Y + 35, HorizontalAlignment.Center);
		}
	}
}

public partial class StarfieldPage : ContentPage
{
	private StarfieldService? _gameService;
	private IDispatcherTimer? _gameLoopTimer;
	private bool _isInitialized = false;
	private readonly StarfieldDrawable _gameDrawable = new();
	private float _lastTouchAngle = 0f;

	public StarfieldPage()
	{
		InitializeComponent();
		CanvasView.Drawable = _gameDrawable;
		
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
		StartGame();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		StopGame();
	}

	private void StartGame()
	{
		_gameLoopTimer = Dispatcher.CreateTimer();
		_gameLoopTimer.Interval = TimeSpan.FromMilliseconds(33);
		_gameLoopTimer.Tick += (s, e) => GameLoopTick();
		_gameLoopTimer.Start();
	}

	private void StopGame()
	{
		_gameLoopTimer?.Stop();
	}

	private void GameLoopTick()
	{
		if (!_isInitialized && CanvasView.Width > 0)
		{
			_gameService = new StarfieldService((float)CanvasView.Width, (float)CanvasView.Height);
			_gameService.Initialize();
			_gameDrawable.GameService = _gameService;
			_isInitialized = true;
		}

		if (!_isInitialized || _gameService == null) return;

		// Only update game if not game over
		if (_gameService.Ship.Lives > 0)
			_gameService.Step();
			
		CanvasView.Invalidate();
	}

	private void OnTapped(object sender, TappedEventArgs e)
	{
		if (_gameService == null) return;
		
		// Restart game if game over
		if (_gameService.Ship.Lives <= 0)
		{
			_gameService = new StarfieldService((float)CanvasView.Width, (float)CanvasView.Height);
			_gameService.Initialize();
			_gameDrawable.GameService = _gameService;
			return;
		}
		
		var pos = e.GetPosition(CanvasView);
		if (pos.HasValue)
		{
			var centerX = CanvasView.Width / 2;
			var centerY = CanvasView.Height / 2;
			var angle = MathF.Atan2((float)(pos.Value.Y - centerY), (float)(pos.Value.X - centerX));
			_gameService.RotateShip(angle);
			_gameService.Shoot();
		}
	}

	private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
	{
		// Remove pan gesture functionality - only use tap
	}

	private async void BackButton_Clicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}