using System.Collections.ObjectModel;
using System.Numerics;

namespace Phy6Sim;

/// <summary>
/// Represents a single point mass in the ragdoll simulation.
/// </summary>
public class RagdollPoint
{
	public Vector2 Position { get; set; }
	public Vector2 OldPosition { get; set; }
	public bool IsPinned { get; set; } = false;
}

/// <summary>
/// Represents a stick (or constraint) connecting two points.
/// </summary>
public class RagdollStick
{
	public RagdollPoint PointA { get; }
	public RagdollPoint PointB { get; }
	public float Length { get; }

	public RagdollStick(RagdollPoint pA, RagdollPoint pB)
	{
		PointA = pA;
		PointB = pB;
		Length = Vector2.Distance(pA.Position, pB.Position);
	}
}

/// <summary>
/// Manages the physics for a ragdoll constructed from points and sticks.
/// </summary>
public class RagdollService
{
	private readonly List<RagdollPoint> _points = new();
	private readonly List<RagdollStick> _sticks = new();
	private Vector2 _gravity = new(0, 1f);
	private readonly Vector2 _worldSize;
	private const float TimeStep = 1f / 60f;

	public ReadOnlyCollection<RagdollPoint> Points => _points.AsReadOnly();
	public ReadOnlyCollection<RagdollStick> Sticks => _sticks.AsReadOnly();

	public RagdollService(float width, float height)
	{
		_worldSize = new Vector2(width, height);
	}

	/// <summary>
	/// Creates the points and sticks that form the ragdoll figure.
	/// </summary>
	public void Initialize()
	{
		// Simplified ragdoll for watchOS - center on screen
		float centerX = _worldSize.X / 2;
		float startY = _worldSize.Y / 3;
		
		var head = new RagdollPoint { Position = new Vector2(centerX, startY), OldPosition = new Vector2(centerX, startY) };
		var torso = new RagdollPoint { Position = new Vector2(centerX, startY + 40), OldPosition = new Vector2(centerX, startY + 40) };
		var leftHand = new RagdollPoint { Position = new Vector2(centerX - 25, startY + 60), OldPosition = new Vector2(centerX - 25, startY + 60) };
		var rightHand = new RagdollPoint { Position = new Vector2(centerX + 25, startY + 60), OldPosition = new Vector2(centerX + 25, startY + 60) };
		var leftFoot = new RagdollPoint { Position = new Vector2(centerX - 15, startY + 80), OldPosition = new Vector2(centerX - 15, startY + 80) };
		var rightFoot = new RagdollPoint { Position = new Vector2(centerX + 15, startY + 80), OldPosition = new Vector2(centerX + 15, startY + 80) };

		_points.AddRange(new[] { head, torso, leftHand, rightHand, leftFoot, rightFoot });

		// Simplified connections
		_sticks.Add(new RagdollStick(head, torso));
		_sticks.Add(new RagdollStick(torso, leftHand));
		_sticks.Add(new RagdollStick(torso, rightHand));
		_sticks.Add(new RagdollStick(torso, leftFoot));
		_sticks.Add(new RagdollStick(torso, rightFoot));
	}

	public void SetGravity(float x, float y)
	{
		_gravity = new Vector2(x, y) * 4f;
	}

	/// <summary>
	/// Applies a sudden force to all points, for a 'shake' effect.
	/// </summary>
	public void ApplyImpulse(Vector2 force)
	{
		foreach (var p in _points)
		{
			p.Position += force;
		}
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

	public RagdollPoint? GetPointAtPosition(Vector2 position, float radius = 20f)
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

	public void DragPoint(RagdollPoint point, Vector2 newPosition)
	{
		point.Position = newPosition;
		point.OldPosition = newPosition;
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
			Vector2 stickDir = Vector2.Normalize(stick.PointA.Position - stick.PointB.Position);
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
				// Bounce effect
				float dotProduct = velocity.X * normalX + velocity.Y * normalY;
				p.OldPosition = new Vector2(
					p.Position.X - (velocity.X - 2 * dotProduct * normalX) * 0.5f,
					p.Position.Y - (velocity.Y - 2 * dotProduct * normalY) * 0.5f
				);
			}
		}
	}
}
