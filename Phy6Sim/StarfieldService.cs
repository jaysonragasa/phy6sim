using System.Collections.ObjectModel;
using System.Numerics;
using Microsoft.Maui.Graphics;

namespace Phy6Sim;

public class Star
{
	public Vector2 Position { get; set; }
	public float Speed { get; set; }
	public float Size { get; set; }
}

public class Asteroid
{
	public Vector2 Position { get; set; }
	public Vector2 Velocity { get; set; }
	public float Radius { get; set; }
	public float Rotation { get; set; }
	public int Health { get; set; }
	public Color Color { get; set; }
}

public class Bullet
{
	public Vector2 Position { get; set; }
	public Vector2 Velocity { get; set; }
	public float Life { get; set; } = 1f;
}

public class ExplosionParticle
{
	public Vector2 Position { get; set; }
	public Vector2 Velocity { get; set; }
	public float Life { get; set; }
	public Color Color { get; set; }
	public float Size { get; set; }
}

public enum PowerUpType { DoubleBullet, Laser, Shower, Bomb }

public class PowerUp
{
	public Vector2 Position { get; set; }
	public Vector2 Velocity { get; set; }
	public PowerUpType Type { get; set; }
	public Color Color { get; set; }
}

public class Ship
{
	public Vector2 Position { get; set; }
	public float Rotation { get; set; }
	public float Size { get; } = 8f;
	public float Lives { get; set; } = 10f;
	public PowerUpType? ActivePowerUp { get; set; }
	public float PowerUpTimer { get; set; }
	public int Score { get; set; } = 0;
}

public class StarfieldService
{
	private readonly List<Star> _stars = new();
	private readonly List<Asteroid> _asteroids = new();
	private readonly List<Bullet> _bullets = new();
	private readonly List<PowerUp> _powerUps = new();
	private readonly List<ExplosionParticle> _particles = new();
	private readonly Ship _ship = new();
	private readonly Vector2 _worldSize;
	private readonly Random _random = new();
	private float _asteroidSpawnTimer = 0f;
	private float _powerUpSpawnTimer = 0f;
	private const float TimeStep = 1f / 30f;

	public ReadOnlyCollection<Star> Stars => _stars.AsReadOnly();
	public ReadOnlyCollection<Asteroid> Asteroids => _asteroids.AsReadOnly();
	public ReadOnlyCollection<Bullet> Bullets => _bullets.AsReadOnly();
	public ReadOnlyCollection<PowerUp> PowerUps => _powerUps.AsReadOnly();
	public ReadOnlyCollection<ExplosionParticle> Particles => _particles.AsReadOnly();
	public Ship Ship => _ship;

	public StarfieldService(float width, float height)
	{
		_worldSize = new Vector2(width, height);
		_ship.Position = new Vector2(width / 2, height / 2);
	}

	public void Initialize()
	{
		// Create starfield
		for (int i = 0; i < 30; i++)
		{
			_stars.Add(new Star
			{
				Position = new Vector2(_random.NextSingle() * _worldSize.X, _random.NextSingle() * _worldSize.Y),
				Speed = _random.NextSingle() * 2f + 0.5f,
				Size = _random.NextSingle() * 2f + 1f
			});
		}
	}

	public void RotateShip(float angle)
	{
		_ship.Rotation = angle;
	}

	public void Shoot()
	{
		var direction = new Vector2(MathF.Cos(_ship.Rotation), MathF.Sin(_ship.Rotation));
		
		switch (_ship.ActivePowerUp)
		{
			case PowerUpType.DoubleBullet:
				for (int i = 0; i < 2; i++)
				{
					var angle = _ship.Rotation + (i - 0.5f) * 0.2f;
					var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
					_bullets.Add(new Bullet { Position = _ship.Position + dir * 15f, Velocity = dir * 200f });
				}
				break;
			case PowerUpType.Shower:
				for (int i = 0; i < 5; i++)
				{
					var angle = _ship.Rotation + (i - 2) * 0.3f;
					var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
					_bullets.Add(new Bullet { Position = _ship.Position + dir * 15f, Velocity = dir * 180f });
				}
				break;
			case PowerUpType.Laser:
				_bullets.Add(new Bullet { Position = _ship.Position + direction * 15f, Velocity = direction * 400f, Life = 0.3f });
				break;
			case PowerUpType.Bomb:
				for (int i = 0; i < 8; i++)
				{
					var angle = i * MathF.PI / 4;
					var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
					_bullets.Add(new Bullet { Position = _ship.Position + dir * 15f, Velocity = dir * 150f });
				}
				break;
			default:
				_bullets.Add(new Bullet { Position = _ship.Position + direction * 15f, Velocity = direction * 200f });
				break;
		}
	}

	public void Step()
	{
		UpdateStars();
		UpdateAsteroids();
		UpdateBullets();
		UpdatePowerUps();
		UpdateParticles();
		UpdateShip();
		SpawnAsteroids();
		SpawnPowerUps();
		CheckCollisions();
	}

	private void UpdateStars()
	{
		foreach (var star in _stars)
		{
			star.Position += new Vector2(0, star.Speed);
			if (star.Position.Y > _worldSize.Y)
				star.Position = new Vector2(_random.NextSingle() * _worldSize.X, -5);
		}
	}

	private void UpdateAsteroids()
	{
		for (int i = _asteroids.Count - 1; i >= 0; i--)
		{
			var asteroid = _asteroids[i];
			asteroid.Position += asteroid.Velocity * TimeStep;
			asteroid.Rotation += 0.02f;

			var center = _worldSize / 2;
			if (Vector2.Distance(asteroid.Position, center) > _worldSize.X / 2 + 50)
				_asteroids.RemoveAt(i);
		}
	}

	private void UpdateBullets()
	{
		for (int i = _bullets.Count - 1; i >= 0; i--)
		{
			var bullet = _bullets[i];
			bullet.Position += bullet.Velocity * TimeStep;
			bullet.Life -= TimeStep;

			if (bullet.Life <= 0 || Vector2.Distance(bullet.Position, _worldSize / 2) > _worldSize.X / 2)
				_bullets.RemoveAt(i);
		}
	}

	private void UpdatePowerUps()
	{
		for (int i = _powerUps.Count - 1; i >= 0; i--)
		{
			var powerUp = _powerUps[i];
			powerUp.Position += powerUp.Velocity * TimeStep;
			
			var center = _worldSize / 2;
			if (Vector2.Distance(powerUp.Position, center) > _worldSize.X / 2 + 50)
				_powerUps.RemoveAt(i);
			else if (Vector2.Distance(powerUp.Position, center) < 15f)
			{
				_ship.ActivePowerUp = powerUp.Type;
				_ship.PowerUpTimer = 15f;
				_powerUps.RemoveAt(i);
			}
		}
	}
	
	private void UpdateShip()
	{
		if (_ship.PowerUpTimer > 0)
		{
			_ship.PowerUpTimer -= TimeStep;
			if (_ship.PowerUpTimer <= 0)
				_ship.ActivePowerUp = null;
		}
	}

	private void SpawnAsteroids()
	{
		_asteroidSpawnTimer += TimeStep;
		if (_asteroidSpawnTimer > 2f)
		{
			_asteroidSpawnTimer = 0f;
			SpawnAsteroid();
		}
	}
	
	private void SpawnPowerUps()
	{
		_powerUpSpawnTimer += TimeStep;
		if (_powerUpSpawnTimer > 8f)
		{
			_powerUpSpawnTimer = 0f;
			SpawnPowerUp();
		}
	}
	
	private void SpawnPowerUp()
	{
		var angle = _random.NextSingle() * MathF.PI * 2;
		var distance = _worldSize.X / 2 + 30;
		var center = _worldSize / 2;
		var spawnPos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
		var velocity = Vector2.Normalize(center - spawnPos) * 25f;
		
		var type = (PowerUpType)_random.Next(4);
		var color = type switch
		{
			PowerUpType.DoubleBullet => Colors.Green,
			PowerUpType.Laser => Colors.Red,
			PowerUpType.Shower => Colors.Blue,
			PowerUpType.Bomb => Colors.Purple,
			_ => Colors.White
		};
		
		_powerUps.Add(new PowerUp { Position = spawnPos, Velocity = velocity, Type = type, Color = color });
	}

	private void SpawnAsteroid()
	{
		var angle = _random.NextSingle() * MathF.PI * 2;
		var distance = _worldSize.X / 2 + 30;
		var center = _worldSize / 2;
		var spawnPos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
		var velocity = Vector2.Normalize(center - spawnPos) * (_random.NextSingle() * 30f + 20f);

		_asteroids.Add(new Asteroid
		{
			Position = spawnPos,
			Velocity = velocity,
			Radius = _random.NextSingle() * 15f + 10f,
			Health = 3,
			Color = Colors.Gray
		});
	}

	private void CheckCollisions()
	{
		// Bullet-Asteroid collisions
		for (int i = _bullets.Count - 1; i >= 0; i--)
		{
			var bullet = _bullets[i];
			for (int j = _asteroids.Count - 1; j >= 0; j--)
			{
				var asteroid = _asteroids[j];
				if (Vector2.Distance(bullet.Position, asteroid.Position) < asteroid.Radius)
				{
					_bullets.RemoveAt(i);
					asteroid.Health--;
					
					if (asteroid.Health <= 0)
					{
						_ship.Score += (int)(asteroid.Radius * 2); // Bigger asteroids = more points
						CreateExplosion(asteroid.Position, asteroid.Color);
						BreakAsteroid(asteroid);
						_asteroids.RemoveAt(j);
					}
					else
					{
						asteroid.Color = Colors.Orange;
						CreateHitEffect(asteroid.Position);
					}
					break;
				}
			}
			
			// Bullet-PowerUp collisions
			for (int j = _powerUps.Count - 1; j >= 0; j--)
			{
				var powerUp = _powerUps[j];
				if (Vector2.Distance(bullet.Position, powerUp.Position) < 10f)
				{
					_bullets.RemoveAt(i);
					_powerUps.RemoveAt(j);
					break;
				}
			}
		}
		
		// Ship-Asteroid collisions
		for (int i = _asteroids.Count - 1; i >= 0; i--)
		{
			var asteroid = _asteroids[i];
			if (Vector2.Distance(_ship.Position, asteroid.Position) < asteroid.Radius + _ship.Size)
			{
				_ship.Lives -= 0.25f;
				if (_ship.Lives < 0) _ship.Lives = 0;
				
				// Create dramatic ship explosion
				CreateShipExplosion(_ship.Position);
				CreateExplosion(asteroid.Position, asteroid.Color);
				
				// Destroy the asteroid
				_asteroids.RemoveAt(i);
				break;
			}
		}
	}

	private void BreakAsteroid(Asteroid asteroid)
	{
		if (asteroid.Radius > 8f)
		{
			for (int i = 0; i < 2; i++)
			{
				var angle = _random.NextSingle() * MathF.PI * 2;
				var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 40f;
				
				_asteroids.Add(new Asteroid
				{
					Position = asteroid.Position + velocity * 0.1f,
					Velocity = velocity,
					Radius = asteroid.Radius * 0.6f,
					Health = 2,
					Color = Colors.Brown
				});
			}
		}
	}
	
	private void UpdateParticles()
	{
		for (int i = _particles.Count - 1; i >= 0; i--)
		{
			var particle = _particles[i];
			particle.Position += particle.Velocity * TimeStep;
			particle.Life -= TimeStep;
			particle.Velocity *= 0.98f;
			
			if (particle.Life <= 0)
				_particles.RemoveAt(i);
		}
	}
	
	private void CreateExplosion(Vector2 position, Color color)
	{
		for (int i = 0; i < 8; i++)
		{
			var angle = i * MathF.PI / 4;
			var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (_random.NextSingle() * 60f + 40f);
			
			_particles.Add(new ExplosionParticle
			{
				Position = position,
				Velocity = velocity,
				Life = 0.8f,
				Color = color,
				Size = _random.NextSingle() * 3f + 2f
			});
		}
	}
	
	private void CreateHitEffect(Vector2 position)
	{
		for (int i = 0; i < 3; i++)
		{
			var angle = _random.NextSingle() * MathF.PI * 2;
			var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 30f;
			
			_particles.Add(new ExplosionParticle
			{
				Position = position,
				Velocity = velocity,
				Life = 0.3f,
				Color = Colors.White,
				Size = 1.5f
			});
		}
	}
	
	private void CreateShipExplosion(Vector2 position)
	{
		// Large central explosion
		for (int i = 0; i < 16; i++)
		{
			var angle = i * MathF.PI / 8;
			var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (_random.NextSingle() * 80f + 60f);
			
			_particles.Add(new ExplosionParticle
			{
				Position = position,
				Velocity = velocity,
				Life = 1.2f,
				Color = Colors.Orange,
				Size = _random.NextSingle() * 4f + 3f
			});
		}
		
		// Bright white flash particles
		for (int i = 0; i < 12; i++)
		{
			var angle = _random.NextSingle() * MathF.PI * 2;
			var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (_random.NextSingle() * 100f + 80f);
			
			_particles.Add(new ExplosionParticle
			{
				Position = position,
				Velocity = velocity,
				Life = 0.6f,
				Color = Colors.White,
				Size = _random.NextSingle() * 3f + 2f
			});
		}
		
		// Red fire particles
		for (int i = 0; i < 8; i++)
		{
			var angle = _random.NextSingle() * MathF.PI * 2;
			var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (_random.NextSingle() * 50f + 30f);
			
			_particles.Add(new ExplosionParticle
			{
				Position = position,
				Velocity = velocity,
				Life = 1.5f,
				Color = Colors.Red,
				Size = _random.NextSingle() * 5f + 4f
			});
		}
	}
}