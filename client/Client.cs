using Godot;
using Godot.Collections;

public partial class Client : Node
{
	[Export]
	public int Port = 9001;
	
	[Export] 
	private PackedScene TankScene = ResourceLoader.Load<PackedScene>("res://common/PlayerTank.tscn");
	private readonly Dictionary<int, Node2D> tanks = new();

	public override void _EnterTree()
	{
		// GetTree().SetMultiplayer(mu, this.GetPath());
		
		base._EnterTree();
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
	
	private void SpawnTank(int peerId)
	{
		var tank = TankScene.Instantiate<Node2D>();
		GD.Print($"SpawnTank {peerId}");
		
		tank.Name = $"Tank_{peerId}";
		tank.SetMultiplayerAuthority(peerId);

		tank.Position = new Vector2(200, 200);
		GD.Print("Tank configured.");
		
		base.GetParent<Node2D>()
			.GetNode<Node2D>("EmptyBox")
			.AddChild(tank); // TODO: Map вместо Конкретной карты
		
		GD.Print("Tank added.");
		
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
