using Godot;

public partial class Spawn : Area2D
{
	private int _bodyCount;
	
	public bool IsOccupied => _bodyCount > 0;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		AreaEntered += OnAreaEntered;
		AreaExited += OnAreaExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (ShouldCount(body))
		{
			_bodyCount++;
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (ShouldCount(body))
		{
			_bodyCount = Mathf.Max(0, _bodyCount - 1);
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		if (ShouldCount(area))
		{
			_bodyCount++;
		}
	}

	private void OnAreaExited(Area2D area)
	{
		if (ShouldCount(area))
		{
			_bodyCount = Mathf.Max(0, _bodyCount - 1);
		}
	}
	
	protected virtual bool ShouldCount(Node node)
	{
		// Default behavior: count all objects except other spawn zones
		return !(node is Spawn);
	}
}
