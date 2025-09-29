using System.Collections.ObjectModel;
using System.Numerics;

namespace Phy6Sim;

/// <summary>
/// Represents a single particle in our liquid simulation.
/// </summary>
public class Particle
{
	public Vector2 Position { get; set; }
	public Vector2 Velocity { get; set; }
	public readonly float Radius;

	public Particle(Vector2 position, float radius)
	{
		Position = position;
		Radius = radius;
		Velocity = Vector2.Zero;
	}
}

/// <summary>
/// Manages the physics for a large number of interacting particles.
/// </summary>
public class LiquidSimService
{
	private readonly List<Particle> _particles = new();
	private Vector2 _gravity = new(0, 9.8f);
	private readonly Vector2 _worldSize;
	private const float TimeStep = 1f / 60f;

	public ReadOnlyCollection<Particle> Particles => _particles.AsReadOnly();

	public LiquidSimService(float width, float height)
	{
		_worldSize = new Vector2(width, height);
	}

	/// <summary>
	/// Initializes the simulation by creating a grid of particles.
	/// </summary>
	public void Initialize(int particlesPerRow, int numberOfRows, float particleRadius)
	{
		// Limit particles for watchOS
		particlesPerRow = Math.Min(particlesPerRow, 6);
		numberOfRows = Math.Min(numberOfRows, 4);
		
		for (int i = 0; i < numberOfRows; i++)
		{
			for (int j = 0; j < particlesPerRow; j++)
			{
				float x = 30 + j * (particleRadius * 2.2f);
				float y = _worldSize.Y - 100 - i * (particleRadius * 2.2f);
				_particles.Add(new Particle(new Vector2(x, y), particleRadius));
			}
		}
	}

	public void SetGravity(float x, float y)
	{
		_gravity = new Vector2(x, y) * 1200f;
	}

	/// <summary>
	/// Advances the simulation by one step.
	/// </summary>
	public void Step()
	{
		// Apply gravity and update positions
		foreach (var p in _particles)
		{
			p.Velocity += _gravity * TimeStep;
			p.Position += p.Velocity * TimeStep;
		}

		// Reduced iterations for watchOS performance
		for (int i = 0; i < 2; i++)
		{
			ApplyWorldBounds();
			SolveParticleCollisions();
		}
	}

	private void ApplyWorldBounds()
	{
		float centerX = _worldSize.X / 2;
		float centerY = _worldSize.Y / 2;
		float screenRadius = Math.Min(_worldSize.X, _worldSize.Y) / 2 - 10;
		
		foreach (var p in _particles)
		{
			float dx = p.Position.X - centerX;
			float dy = p.Position.Y - centerY;
			float distanceFromCenter = MathF.Sqrt(dx * dx + dy * dy);
			
			if (distanceFromCenter + p.Radius > screenRadius)
			{
				float normalX = dx / distanceFromCenter;
				float normalY = dy / distanceFromCenter;
				float newDistance = screenRadius - p.Radius;
				p.Position = new Vector2(centerX + normalX * newDistance, centerY + normalY * newDistance);
			}
		}
	}

	private void SolveParticleCollisions()
	{
		// Optimized collision detection for watchOS
		for (int i = 0; i < _particles.Count; i++)
		{
			var p1 = _particles[i];

			for (int j = i + 1; j < _particles.Count; j++)
			{
				var p2 = _particles[j];
				float dx = p1.Position.X - p2.Position.X;
				float dy = p1.Position.Y - p2.Position.Y;
				float distSq = dx * dx + dy * dy;
				float minDist = p1.Radius + p2.Radius;

				if (distSq < minDist * minDist && distSq > 0)
				{
					float dist = MathF.Sqrt(distSq);
					float delta = 0.5f * (minDist - dist) / dist;

					p1.Position += new Vector2(dx * delta, dy * delta);
					p2.Position -= new Vector2(dx * delta, dy * delta);
				}
			}
		}
	}
}
