using Godot;

public partial class Bullet : RigidBody2D
{
	[Export] public float InitialSpeed = 250f;
	[Export] public int MaxBounces = 50;
	[Export] public float LifeTime = 10f;
	
	private int _bouncesLeft;
	private Timer _lifeTimer;
	private bool _hasHit;
	private Vector2 _direction;
	private float _currentSpeed;
	
	public PlayerTank Player { get; set; }
	
	public override void _Ready()
	{
		_lifeTimer = new Timer
		{
			WaitTime = LifeTime,
			OneShot = true
		};
		
		GravityScale = 0f;
		ContactMonitor = true;
		MaxContactsReported = 5;
		
		AddChild(_lifeTimer);
		_lifeTimer.Timeout += OnLifeTimeEnded;
	}

	public override void _IntegrateForces(PhysicsDirectBodyState2D state)
	{
		if (_hasHit) return;
		
		if (state.LinearVelocity.LengthSquared() > 0)
		{
			_direction = state.LinearVelocity.Normalized();
		}
		
		state.LinearVelocity = _direction * _currentSpeed;
		
		base._IntegrateForces(state);
	}

	public override void _PhysicsProcess(double delta)
	{
		var collidingBodies = GetCollidingBodies();

		foreach (var body in collidingBodies)
		{
			OnBodyEntered(body);
		}
		
		base._PhysicsProcess(delta);
	}

	public void Initialize(PlayerTank playerTank, Vector2 position, float rotation, float speedMultiplier = 1.0f)
	{
		Player = playerTank;
		Position = position;
		Rotation = rotation;
		
		_bouncesLeft = MaxBounces;
		_hasHit = false;
		
		_direction = new Vector2(0, -1).Rotated(rotation);
		_currentSpeed = InitialSpeed * speedMultiplier;
		LinearVelocity = _direction * _currentSpeed;
		
		PhysicsMaterialOverride = new PhysicsMaterial
		{
			Bounce = 0.8f,
			Rough = false,
			Friction = 0f
		};
		
		if (_lifeTimer != null)
		{
			if (!_lifeTimer.IsStopped())
				_lifeTimer.Stop();
				
			_lifeTimer.Start();
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (_hasHit) 
			return;
		
		if (body is PlayerTank tank)
		{
			GD.Print($"Tank {tank.Id} killed.");
			tank.TakeDamage();
			_hasHit = true;
			// CreateHitEffect();
			QueueFree();
			return;
		}
		
		if (body is StaticBody2D)
		{
			_bouncesLeft--;
			
			// CreateRicochetEffect();
			
			if (_bouncesLeft <= 0)
			{
				_hasHit = true;
				// CreateHitEffect();
				QueueFree();
			}
		}
	}

	private void OnLifeTimeEnded()
	{
		if (!_hasHit)
		{
			// CreateHitEffect();
			QueueFree();
		}
	}
}
