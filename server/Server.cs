using System.Collections.Generic;
using Godot;

public partial class Server : Node
{
	[Export]
	public int Port = 9001;
	
	[Export] 
	private PackedScene TankScene = ResourceLoader.Load<PackedScene>("res://common/PlayerTank.tscn");
	
	private readonly Dictionary<int, Node2D> tanks = new();

	public override void _EnterTree()
	{
		GetTree().SetMultiplayer(MultiplayerApi.CreateDefaultInterface(), this.GetPath());
		
		base._EnterTree();
	}

	public override void _Ready() => Host(this.Port); // Configuration load

	private void Host(int port)
	{
		var multiplayer = new ENetMultiplayerPeer();
		var result = multiplayer.CreateServer(port);

		if (result != Error.Ok)
		{
			GD.PrintErr("Failed to create server");
			return;
		}

		Multiplayer.MultiplayerPeer = multiplayer;
		
		Multiplayer.PeerConnected += MultiplayerOnPeerConnected;
		Multiplayer.PeerDisconnected += MultiplayerOnPeerDisconnected;
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
		tank.Name = $"Tank_{peerId}";
		tank.SetMultiplayerAuthority(peerId);

		tank.Position = new Vector2(200, 200);
		
		base.GetParent<Node2D>()
			.GetNode<Node2D>("EmptyBox")
			.AddChild(tank); // TODO: Map вместо Конкретной карты
		
		tanks[peerId] = tank;
	}

	private void DespawnTank(int peerId)
	{
		if (tanks.TryGetValue(peerId, out var tank))
		{
			tank.QueueFree();
			tanks.Remove(peerId);
		}
	}
	
	// TODO: fix.
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void ReceiveInput(Dictionary<string, float> input)
	{
		int peerId = Multiplayer.GetRemoteSenderId();
		
		if (tanks.TryGetValue(peerId, out var tankNode))
		{
			var tank = tankNode.GetNode<PlayerTank>("CharacterBody2D");
			tank.ApplyInput(input);
		}
	}
}
