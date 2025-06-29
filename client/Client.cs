using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Client : Node
{
	[Export]
	public int Port = 9001;
	
	[Export] 
	private PackedScene TankScene = ResourceLoader.Load<PackedScene>("res://common/PlayerTank.tscn");
	private readonly Godot.Collections.Dictionary<int, PlayerTank> tanks = new();
	
	private NetworkManager networkManager;
	private readonly List<int> processedTanks = new();

	public override void _EnterTree()
	{
		// GetTree().SetMultiplayer(mu, this.GetPath());
		
		networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		networkManager.OnUpdateGameState += OnUpdateGameState;
		
		base._EnterTree();
	}

	private void OnUpdateGameState(Godot.Collections.Dictionary<int, Vector2> tankPositions, Godot.Collections.Dictionary<int, float> rotations)
	{
		this.networkManager.ResetTimeout();
		
		foreach (var tankPosition in tankPositions)
		{
			if (tanks.TryGetValue(tankPosition.Key, out var tankNode))
			{
				tankNode.UpdateState(tankPosition.Value, rotations[tankPosition.Key]);
			}
			else
			{
				SpawnTank(tankPosition.Key, tankPosition.Value, rotations[tankPosition.Key]);
			}
			
			processedTanks.Add(tankPosition.Key);
		}

		foreach (var tankToDespawn in tanks.Select(t => t.Key)
					 .Except(processedTanks))
		{
			DespawnTank(tankToDespawn);
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
		tank.SetMultiplayerAuthority(peerId);

		tank.Position = new Vector2(200, 200);
		GD.Print("Tank configured.");
		
		base.GetParent<Node2D>()
			.GetNode<Node2D>("EmptyBox")
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
