using System.Collections.ObjectModel;
using System.Numerics;
using Microsoft.Maui.Graphics;

namespace Phy6Sim;

public enum ShapeType { Circle, Box }

/// <summary>
/// Represents a single object within our custom physics simulation.
/// </summary>
public class PhysicsBody
{
	public ShapeType Shape { get; }
	public Vector2 Position { get; set; }
	public Vector2 Velocity { get; set; }
	public float Radius { get; } // Used for circle radius or half-width/height of a box
	public float Angle { get; set; }
	public float AngularVelocity { get; set; }
	public float Restitution { get; } = 0.7f; // Bounciness factor
	public float Mass { get; }
	public Color Color { get; set; }
	public bool IsDragging { get; set; }

	public PhysicsBody(ShapeType shape, Vector2 position, float radius, Color? color = null)
	{
		Shape = shape;
		Position = position;
		Radius = radius;
		Mass = radius * radius; // Simple mass calculation
		Velocity = Vector2.Zero;
		AngularVelocity = (float)(new Random().NextDouble() * 2 - 1); // Random initial spin
		Color = color ?? Colors.CornflowerBlue;
	}

	public bool ContainsPoint(Vector2 point)
	{
		// Use squared distance to avoid expensive sqrt
		float dx = Position.X - point.X;
		float dy = Position.Y - point.Y;
		return (dx * dx + dy * dy) <= (Radius * Radius);
	}
}

/// <summary>
/// A reusable service to manage a simple, custom physics world without external libraries.
/// </summary>
public class PhysicsService
{
	private readonly List<PhysicsBody> _bodies = new();
	private Vector2 _gravity = new(0, 9.8f);
	private readonly Vector2 _worldSize;
	private const float TimeStep = 1f / 60f; // Simulate at 60 FPS

	public ReadOnlyCollection<PhysicsBody> Bodies => _bodies.AsReadOnly();

	public PhysicsService(float width, float height)
	{
		_worldSize = new Vector2(width, height);
	}

	/// <summary>
	/// Initializes the simulation with dynamic bodies.
	/// </summary>
	public void Initialize(int numberOfShapes)
	{
		// Reduce shapes for watchOS performance
		numberOfShapes = Math.Min(numberOfShapes, 8);
		
		var random = new Random();
		var colors = new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Orange, Colors.Purple };
		
		for (int i = 0; i < numberOfShapes; i++)
		{
			float radius = 20f; // Fixed size for better performance
			float x = radius + (i % 3) * (_worldSize.X - radius * 2) / 2;
			float y = radius + (i / 3) * 40;
			var shapeType = i % 2 == 0 ? ShapeType.Circle : ShapeType.Box;
			var color = colors[i % colors.Length];

			_bodies.Add(new PhysicsBody(shapeType, new Vector2(x, y), radius, color));
		}
	}

	/// <summary>
	/// Updates the gravity vector of the physics world.
	/// </summary>
	public void SetGravity(float x, float y)
	{
		_gravity = new Vector2(x, y) * 800f;
	}

	/// <summary>
	/// Advances the physics simulation by one time step.
	/// </summary>
	public void Step()
	{
		foreach (var body in _bodies)
		{
			if (!body.IsDragging)
			{
				// Apply gravity to velocity
				body.Velocity += _gravity * TimeStep;

				// Update position based on velocity
				body.Position += body.Velocity * TimeStep;
			}

			// Update angle based on angular velocity
			body.Angle += body.AngularVelocity * TimeStep;

			// Handle collisions with the world boundaries
			HandleWallCollisions(body);
		}
	}

	public PhysicsBody? GetBodyAtPoint(Vector2 point)
	{
		// Reverse iteration for top-most body
		for (int i = _bodies.Count - 1; i >= 0; i--)
		{
			if (_bodies[i].ContainsPoint(point))
				return _bodies[i];
		}
		return null;
	}

	public void StartDrag(PhysicsBody body)
	{
		body.IsDragging = true;
		body.Velocity = Vector2.Zero;
	}

	public void UpdateDrag(PhysicsBody body, Vector2 newPosition)
	{
		body.Position = newPosition;
	}

	public void EndDrag(PhysicsBody body, Vector2 velocity)
	{
		body.IsDragging = false;
		body.Velocity = velocity;
	}

	private void HandleWallCollisions(PhysicsBody body)
	{
		// Circular boundary for watchOS
		float centerX = _worldSize.X / 2;
		float centerY = _worldSize.Y / 2;
		float screenRadius = Math.Min(_worldSize.X, _worldSize.Y) / 2 - 10; // 10px margin
		
		float dx = body.Position.X - centerX;
		float dy = body.Position.Y - centerY;
		float distanceFromCenter = MathF.Sqrt(dx * dx + dy * dy);
		
		if (distanceFromCenter + body.Radius > screenRadius)
		{
			// Normalize direction and place at boundary
			float normalX = dx / distanceFromCenter;
			float normalY = dy / distanceFromCenter;
			
			// Position at boundary
			float newDistance = screenRadius - body.Radius;
			body.Position = new Vector2(centerX + normalX * newDistance, centerY + normalY * newDistance);
			
			// Reflect velocity
			float dotProduct = body.Velocity.X * normalX + body.Velocity.Y * normalY;
			body.Velocity = new Vector2(
				(body.Velocity.X - 2 * dotProduct * normalX) * body.Restitution,
				(body.Velocity.Y - 2 * dotProduct * normalY) * body.Restitution
			);
		}
	}
}

