using Godot;

public partial class Bullet : RigidBody2D
{
	[Export] public float InitialSpeed = 1000f;
	[Export] public int MaxBounces = 50;
	[Export] public float LifeTime = 10f;
	
	private int _bouncesLeft;
	private Timer _lifeTimer;
	private bool _hasHit;
	
	public PlayerTank Player { get; set; }
	
	public override void _Ready()
	{
		// Создаем и настраиваем таймер
		_lifeTimer = new Timer
		{
			WaitTime = LifeTime,
			OneShot = true
		};
		
		GravityScale = 0f;
		
		AddChild(_lifeTimer);
		_lifeTimer.Timeout += OnLifeTimeEnded;
	}
	
	public void Initialize(PlayerTank playerTank, Vector2 position, float rotation, float speedMultiplier = 1.0f)
	{
		Player = playerTank;
		Position = position;
		Rotation = rotation;
		
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
		
		// Запускаем таймер только если он добавлен в дерево
		if (_lifeTimer != null && !_lifeTimer.IsStopped())
		{
			_lifeTimer.Stop();
		}
		
		if (_lifeTimer != null)
		{
			_lifeTimer.Start();
		}
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
