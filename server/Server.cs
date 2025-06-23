using Godot;
using Godot.Collections;

public partial class Server : Node
{
	[Export]
	public int Port = 9001;
	
	[Export] 
	private PackedScene TankScene = ResourceLoader.Load<PackedScene>("res://common/PlayerTank.tscn");

	private bool isServer;
	private readonly Dictionary<int, Node2D> tanks = new();

	public override void _EnterTree()
	{
		isServer = OS.HasFeature(Main.ServerEnvironment);
		GD.Print($"Server isServer: {isServer}");
		
		if (isServer)
			GetTree().SetMultiplayer(MultiplayerApi.CreateDefaultInterface(), this.GetPath());
		
		base._EnterTree();
	}

	public override void _Ready() => Host(this.Port); // Configuration load

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
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void ReceiveInput(Dictionary<string, float> input)
	{
		GD.Print($"InputLength: {input.Count}");
		int peerId = Multiplayer.GetRemoteSenderId();
		
		if (tanks.TryGetValue(peerId, out var tankNode))
		{
			 var tank = tankNode.GetNode<PlayerTank>("CharacterBody2D");
			 GD.Print($"tank received. {tank is not null}");
			 tank.ApplyInput(input);
		}
	}
}
