using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

public partial class Client : Node
{
	[Export]
	public int Port = 9001;
	
	[Export] 
	private PackedScene TankScene = ResourceLoader.Load<PackedScene>("res://common/PlayerTank.tscn");
	
	[Export]
	private PackedScene BulletScene = ResourceLoader.Load<PackedScene>("res://common/Bullet.tscn");
	
	private readonly Godot.Collections.Dictionary<int, PlayerTank> tanks = new();
	
	private NetworkManager networkManager;
	private readonly List<int> processedTanks = new();

	private double timer;
	public override void _Process(double delta)
	{
		timer += delta;

		if (timer > 1.0)
		{
			var bullets = GetNode<Node2D>("/root/Main/Map")
				.GetChildren()
				.OfType<Bullet>();
			
			GD.Print($"Bullets: {bullets.Count()}"); // TODO: Remove;
			
			timer = 0;
		}

		if (Input.IsActionJustPressed("ui_accept"))
		{
			var sb = new StringBuilder();
			PrintNode(GetNode<Node>("/root/Main"), 0);

			GD.Print(sb.ToString());
			
			void PrintNode(Node node, int subLevel)
			{
				sb.AppendLine(new string(' ', subLevel) + node.Name);
				
				foreach (var child in node.GetChildren())
				{
					PrintNode(child, subLevel + 1);
				}
			}
		}
		
		base._Process(delta);
	}

	public override void _EnterTree()
	{
		// GetTree().SetMultiplayer(mu, this.GetPath());
		
		networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		networkManager.OnUpdateGameState += OnUpdateGameState;
		
		base._EnterTree();
	}

	private void OnUpdateGameState(
		Godot.Collections.Dictionary<int, Vector2> tankPositions, 
		Godot.Collections.Dictionary<int, float> tankRotations, 
		Godot.Collections.Dictionary<ulong, Vector2> bulletPositions, 
		Godot.Collections.Dictionary<ulong, float> bulletRotations)
	{
		this.networkManager.ResetTimeout();
		
		foreach (var tankPosition in tankPositions)
		{
			if (tanks.TryGetValue(tankPosition.Key, out var tankNode))
			{
				tankNode.UpdateState(tankPosition.Value, tankRotations[tankPosition.Key]);
			}
			else
			{
				SpawnTank(tankPosition.Key, tankPosition.Value, tankRotations[tankPosition.Key]);
			}
			
			processedTanks.Add(tankPosition.Key);
		}

		foreach (var tankToDespawn in tanks.Select(t => t.Key)
					 .Except(processedTanks))
		{
			DespawnTank(tankToDespawn);
		}

		var map = GetNode<Node2D>("/root/Main/Map");
		
		foreach (var bulletPosition in bulletPositions)
		{
			var id = bulletPosition.Key;

			var bullet = GetNodeOrNull<Bullet>($"/root/Main/Map/{id}");

			if (bullet == null)
			{
				bullet = BulletScene.Instantiate<Bullet>();
				bullet.Freeze = true;
				bullet.Name = id.ToString();
				
				map.AddChild(bullet);
			}

			bullet.Position = bulletPosition.Value;
			bullet.Rotation = bulletRotations[id];
		}
		
		foreach (var bulletToDespawn in map.GetChildren()
					 .OfType<Bullet>()
					 .Select(b => ulong.Parse(b.Name))
					 .Except(bulletPositions.Keys))
		{
			GetNode<Bullet>($"/root/Main/Map/{bulletToDespawn}")
				.QueueFree();
		}
		
		processedTanks.Clear();
	}

	public override void _Ready() => Join("127.0.0.1", Port);

	public void Join(string host, int port)
	{
		var multiplayer = new ENetMultiplayerPeer();
		var result = multiplayer.CreateClient(host, port);

		if (result != Error.Ok)
		{
			GD.PrintErr($"Failed to create client: {result}");
		}

		Multiplayer.MultiplayerPeer = multiplayer;
		
		Multiplayer.PeerConnected += MultiplayerOnPeerConnected;
		Multiplayer.PeerDisconnected += MultiplayerOnPeerDisconnected;
	}

	private void MultiplayerOnPeerDisconnected(long id)
	{
		GD.Print($"Peer {id} disconnected (client {Multiplayer.GetUniqueId()})");
		DespawnTank(Multiplayer.GetUniqueId());

		foreach (var tank in this.tanks)
		{
			DespawnTank(tank.Key);
		}
		
		GetTree().SetMultiplayer(null);
	}

	private void MultiplayerOnPeerConnected(long id)
	{
		GD.Print($"Peer {id} connected (client {Multiplayer.GetUniqueId()})");
		SpawnTank(Multiplayer.GetUniqueId());
	}
	
	private void SpawnTank(int peerId, Vector2? position = null, float? rotation = null)
	{
		if (tanks.ContainsKey(peerId))
			return;
		
		var tank = TankScene.Instantiate<PlayerTank>();
		GD.Print($"SpawnTank {peerId}");
		
		tank.Name = $"Tank_{peerId}";
		tank.Id = peerId;
		tank.SetMultiplayerAuthority(peerId);

		tank.Position = new Vector2(200, 200);
		GD.Print("Tank configured.");
		
		base.GetParent<Node2D>()
			.GetNode<Node2D>("Map")
			.AddChild(tank); // TODO: Map вместо Конкретной карты
		
		GD.Print("Tank added.");

		if (position.HasValue)
		{
			tank.Position = position.Value;
		}

		if (rotation.HasValue)
		{
			tank.Rotation = rotation.Value;
		}
		
		tanks[peerId] = tank;
	}

	private void DespawnTank(int peerId)
	{
		GD.Print($"DespawnTank {peerId}");
		
		if (tanks.TryGetValue(peerId, out var tank))
		{
			tank.QueueFree();
			tanks.Remove(peerId);
			
			GD.Print($"Despawned: {peerId}");
		}
	}
}
