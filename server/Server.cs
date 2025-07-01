using System.Linq;
using Godot;
using Godot.Collections;

public partial class Server : Node
{
	[Export]
	public int Port = 9001;
	
	[Export] 
	private PackedScene TankScene = ResourceLoader.Load<PackedScene>("res://common/PlayerTank.tscn");

	private bool isServer;
	private readonly Dictionary<int, PlayerTank> tanks = new();
	
	private NetworkManager networkManager;

	public override void _EnterTree()
	{
		isServer = GetParent<Main>().IsServer;
		GD.Print($"Server isServer: {isServer}");
		
		if (isServer){
			GetTree().SetMultiplayer(MultiplayerApi.CreateDefaultInterface(), this.GetPath());}
		
		networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		
		base._EnterTree();
	}

	public override void _Ready() => Host(this.Port); // Configuration load

	public override void _Process(double delta)
	{
		var positions = new Dictionary<int, Vector2>();
		var rotations = new Dictionary<int, float>();
		var bulletPositions = new Dictionary<ulong, Vector2>();
		var bulletRotations = new Dictionary<ulong, float>();

		foreach (var tank in tanks)
		{
			positions.Add(tank.Key, tank.Value.Position);
			rotations.Add(tank.Key, tank.Value.Rotation);
		}

		foreach (var bullet in GetNode<Node2D>("/root/Main/EmptyBox")
					 .FindChildren("Bullet_")
					 .Cast<Bullet>())
		{
			var id = bullet.GetInstanceId();
			
			bulletPositions.Add(id, bullet.Position);
			bulletRotations.Add(id, bullet.Rotation);
		}
		
		networkManager.Rpc(
			nameof(NetworkManager.UpdateGameState),
			positions,
			rotations,
			bulletPositions,
			bulletRotations);
		
		base._Process(delta);
	}

	private void Host(int port)
	{
		if (!isServer)
			return;
		
		var multiplayer = new ENetMultiplayerPeer();
		var result = multiplayer.CreateServer(port);

		if (result != Error.Ok)
		{
			GD.PrintErr($"Failed to create server: {result}");
			return;
		}

		Multiplayer.MultiplayerPeer = multiplayer;
		
		Multiplayer.PeerConnected += MultiplayerOnPeerConnected;
		Multiplayer.PeerDisconnected += MultiplayerOnPeerDisconnected;

		GetTree().SetMultiplayer(Multiplayer);
		
		GD.Print(Multiplayer.MultiplayerPeer.TransferMode.ToString());
	}

	private void MultiplayerOnPeerDisconnected(long id)
	{
		GD.Print($"Peer {id} disconnected");
		DespawnTank((int)id);
	}

	private void MultiplayerOnPeerConnected(long id)
	{
		GD.Print($"Peer {id} connected");
		SpawnTank((int)id);
	}
	
	private void SpawnTank(int peerId)
	{
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
	
	public void ReceiveInput(Dictionary<string, float> input)
	{
		int peerId = Multiplayer.GetRemoteSenderId();
		
		if (tanks.TryGetValue(peerId, out var tankNode))
		{
			tankNode.ApplyInput(input);
		}
	}
}
