using System.Collections.ObjectModel;
using System.Numerics;

namespace Phy6Sim;

// NOTE: For a real application, you would move these shared classes
// into a separate file (e.g., "PhysicsObjects.cs") to avoid duplication.
public class PhysicsPoint
{
	public Vector2 Position { get; set; }
	public Vector2 OldPosition { get; set; }
	public bool IsPinned { get; set; } = false;
}

public class PhysicsStick
{
	public PhysicsPoint PointA { get; }
	public PhysicsPoint PointB { get; }
	public float Length { get; }

	public PhysicsStick(PhysicsPoint pA, PhysicsPoint pB)
	{
		PointA = pA;
		PointB = pB;
		Length = Vector2.Distance(pA.Position, pB.Position);
	}
}


/// <summary>
/// Manages the physics for a chain constructed from points and sticks.
/// </summary>
public class ChainPhysicsService
{
	private readonly List<PhysicsPoint> _points = new();
	private readonly List<PhysicsStick> _sticks = new();
	private Vector2 _gravity = new(0, 1f);
	private readonly Vector2 _worldSize;
	private const float TimeStep = 1f / 60f;

	public ReadOnlyCollection<PhysicsPoint> Points => _points.AsReadOnly();
	public ReadOnlyCollection<PhysicsStick> Sticks => _sticks.AsReadOnly();

	public ChainPhysicsService(float width, float height)
	{
		_worldSize = new Vector2(width, height);
	}

	/// <summary>
	/// Creates a chain of points and sticks hanging from the top.
	/// </summary>
	public void Initialize(int segments, float segmentLength)
	{
		// Limit segments for watchOS
		segments = Math.Min(segments, 12);
		
		for (int i = 0; i < segments; i++)
		{
			var point = new PhysicsPoint
			{
				Position = new Vector2(_worldSize.X / 2, 30 + i * segmentLength),
				OldPosition = new Vector2(_worldSize.X / 2, 30 + i * segmentLength)
			};
			_points.Add(point);
		}

		if (_points.Any())
		{
			_points[0].IsPinned = true;
		}

		for (int i = 0; i < segments - 1; i++)
		{
			_sticks.Add(new PhysicsStick(_points[i], _points[i + 1]));
		}
	}

	public void SetGravity(float x, float y)
	{
		_gravity = new Vector2(x, y) * 4f;
	}

	public void Step()
	{
		UpdatePoints();
		// Reduced iterations for watchOS
		for (int i = 0; i < 3; i++)
		{
			UpdateSticks();
			ApplyWorldBounds();
		}
	}

	public PhysicsPoint? GetPointAtPosition(Vector2 position, float radius = 15f)
	{
		foreach (var point in _points)
		{
			float dx = point.Position.X - position.X;
			float dy = point.Position.Y - position.Y;
			if (dx * dx + dy * dy <= radius * radius)
				return point;
		}
		return null;
	}

	public void DragPoint(PhysicsPoint point, Vector2 newPosition)
	{
		if (!point.IsPinned)
		{
			point.Position = newPosition;
			point.OldPosition = newPosition;
		}
	}

	private void UpdatePoints()
	{
		foreach (var p in _points)
		{
			if (p.IsPinned) continue;

			Vector2 velocity = p.Position - p.OldPosition;
			p.OldPosition = p.Position;
			p.Position += velocity + _gravity * (TimeStep * TimeStep);
		}
	}

	private void UpdateSticks()
	{
		foreach (var stick in _sticks)
		{
			Vector2 stickCenter = (stick.PointA.Position + stick.PointB.Position) / 2;
			Vector2 stickDir = stick.PointA.Position - stick.PointB.Position;

			if (stickDir.LengthSquared() == 0) continue; // Avoid division by zero

			stickDir = Vector2.Normalize(stickDir);
			float halfLength = stick.Length / 2;

			if (!stick.PointA.IsPinned)
				stick.PointA.Position = stickCenter + stickDir * halfLength;
			if (!stick.PointB.IsPinned)
				stick.PointB.Position = stickCenter - stickDir * halfLength;
		}
	}

	private void ApplyWorldBounds()
	{
		float centerX = _worldSize.X / 2;
		float centerY = _worldSize.Y / 2;
		float screenRadius = Math.Min(_worldSize.X, _worldSize.Y) / 2 - 10;
		
		foreach (var p in _points)
		{
			if (p.IsPinned) continue;

			float dx = p.Position.X - centerX;
			float dy = p.Position.Y - centerY;
			float distanceFromCenter = MathF.Sqrt(dx * dx + dy * dy);
			
			if (distanceFromCenter > screenRadius)
			{
				Vector2 velocity = p.Position - p.OldPosition;
				float normalX = dx / distanceFromCenter;
				float normalY = dy / distanceFromCenter;
				
				p.Position = new Vector2(centerX + normalX * screenRadius, centerY + normalY * screenRadius);
				float dotProduct = velocity.X * normalX + velocity.Y * normalY;
				p.OldPosition = new Vector2(
					p.Position.X - (velocity.X - 2 * dotProduct * normalX) * 0.5f,
					p.Position.Y - (velocity.Y - 2 * dotProduct * normalY) * 0.5f
				);
			}
		}
	}
}
