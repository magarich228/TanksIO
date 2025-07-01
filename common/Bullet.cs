using Godot;
public partial class Bullet : RigidBody2D
{
	[Export] public float InitialSpeed = 1000f;
	[Export] public int MaxBounces = 5;
	[Export] public float LifeTime = 10f;
	
	private int _bouncesLeft;
	private Timer _lifeTimer;
	private bool _hasHit;
	
	public PlayerTank Player { get; set; }
	
	public void Initialize(PlayerTank playerTank, Vector2 position, float rotation, float speedMultiplier = 1.0f)
	{
		Player = playerTank;
		Position = position;
		Rotation = rotation;
		GravityScale = 0f;
		
		_bouncesLeft = MaxBounces;
		_hasHit = false;
		
		// Направление и скорость
		Vector2 direction = new Vector2(0, -1).Rotated(rotation);
		LinearVelocity = direction * InitialSpeed * speedMultiplier;
		
		// Настройка физики для отскоков
		PhysicsMaterialOverride = new PhysicsMaterial
		{
			Bounce = 0.8f,
			Rough = false
		};
		
		// Таймер времени жизни
		_lifeTimer = new Timer
		{
			WaitTime = LifeTime,
			OneShot = true
		};
		
		AddChild(_lifeTimer);
		
		_lifeTimer.Timeout += OnLifeTimeEnded;
		_lifeTimer.Start();
	}

	private void OnBodyEntered(Node body)
	{
		// Проверка, что пуля еще активна
		if (_hasHit) return;
		
		// Обработка попадания в танк
		if (body is PlayerTank tank)
		{
			tank.TakeDamage();
			_hasHit = true;
			// CreateHitEffect();
			QueueFree();
			return;
		}
		
		// Обработка отскока от стены
		if (body is StaticBody2D)
		{
			_bouncesLeft--;
			
			// Создаем эффект рикошета
			// CreateRicochetEffect();
			
			// Уничтожаем пулю после последнего отскока
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
