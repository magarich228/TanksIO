using System;
using Godot;
using Godot.Collections;

public partial class PlayerTank : CharacterBody2D
{
	[Export] public float MoveSpeed = 150f;
	[Export] public float RotateSpeed = 3.0f;
	public int Id { get; set; }

	[Signal]
	public delegate void KilledEventHandler(PlayerTank tank);
	
	public event Action<Bullet> OnShoot;

	private Dictionary<string, float> currentInput = new();

	[Export] private PackedScene _bulletScene;
	[Export] private float _fireRate = 0.5f;
	[Export] private float _bulletSpeedMultiplier = 1.0f;
	
	private float _fireCooldown;
	
	private NetworkManager networkManager;

	public override void _Ready()
	{
		_bulletScene = ResourceLoader.Load<PackedScene>("res://common/Bullet.tscn");
		
		base._Ready();
	}

	public override void _EnterTree()
	{
		this.networkManager = GetNode<NetworkManager>("/root/NetworkManager");

		base._EnterTree();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Multiplayer.IsServer())
		{
			var direction = new Vector2(0, -1).Rotated(Rotation);

			currentInput.TryGetValue("move", out var move);
			currentInput.TryGetValue("rotate", out var rotate);
			
			Velocity = direction * move * MoveSpeed;
			Rotation += rotate * RotateSpeed * (float) delta;

			if (currentInput.ContainsKey("shoot"))
			{
				GD.Print("server shoot");
				Shoot();
			}

			MoveAndSlide();
		}
		else
		{
			var connectionStatus = Multiplayer.MultiplayerPeer.GetConnectionStatus();

			if (connectionStatus != MultiplayerPeer.ConnectionStatus.Connected)
			{
				GD.Print($"Connection status: {connectionStatus}");
				return;
			}

			var input = new Dictionary<string, float>
			{
				["move"] = Input.GetAxis("move_backward", "move_forward"),
				["rotate"] = Input.GetAxis("rotate_left", "rotate_right")
			};

			if (Input.IsActionJustPressed("shoot"))
			{
				input.Add("shoot", 1.0f);
			}

			networkManager.RpcId(1, nameof(NetworkManager.ReceiveInput), input);
		}
	}

	public void ApplyInput(Dictionary<string, float> input)
	{
		currentInput = input;
	}

	public void UpdateState(Vector2 position, float rotation)
	{
		if (!Multiplayer.IsServer())
		{
			Position = Position.Lerp(position, 0.2f);
			Rotation = Mathf.LerpAngle(Rotation, rotation, 0.2f);
		}
	}
	
	private void Shoot()
	{
		if (_bulletScene == null) return;
		
		var bullet = _bulletScene.Instantiate<Bullet>();
		bullet.Name = bullet.GetInstanceId()
			.ToString();
		
		GetParent().AddChild(bullet);
		
		Vector2 spawnPos = Position + new Vector2(0, -30).Rotated(Rotation);
		bullet.Initialize(this, spawnPos, Rotation, _bulletSpeedMultiplier);
		
		OnShoot?.Invoke(bullet);
		
		// Сигнал столкновения
		// bullet.BodyEntered += OnBulletBodyEntered;
		
		// Звук выстрела
		// GetNode<AudioStreamPlayer>("ShootSound").Play();
	}
	
	public void TakeDamage(int damage = 0)
	{
		Explode();
	}
	
	private void Explode()
	{
		// Эффект взрыва
		// var explosion = GD.Load<PackedScene>("res://effects/explosion.tscn").Instantiate();
		// explosion.Position = Position;
		// GetParent().AddChild(explosion);

		EmitSignal(SignalName.Killed, this);
	}
}
