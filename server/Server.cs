using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TanksIO;

public partial class Server : Node
{
	[Export] public int Port = 9001;

	[Export] private PackedScene TankScene = ResourceLoader.Load<PackedScene>("res://common/PlayerTank.tscn");

	private bool isServer;
	private readonly Queue<(int tankId, SceneTreeTimer timer)> tanksToSpawn = new();
	private readonly Godot.Collections.Dictionary<int, PlayerTank> tanks = new();

	private NetworkManager networkManager;

	public override void _EnterTree()
	{
		isServer = GetParent<Main>().IsServer;
		GD.Print($"Server isServer: {isServer}");

		if (isServer)
		{
			GetTree().SetMultiplayer(MultiplayerApi.CreateDefaultInterface(), this.GetPath());
		}

		networkManager = GetNode<NetworkManager>("/root/NetworkManager");

		base._EnterTree();
	}

	public override void _Ready()
	{
		var configuration = new Configuration();
		configuration.Load();

		Host(configuration.Port);
	}

	public override void _Process(double delta)
	{
		if (this.tanksToSpawn.TryPeek(out var tankToSpawn) &&
			tankToSpawn.timer.TimeLeft == 0 &&
			TrySpawnTank(tankToSpawn.tankId))
		{
			_ = this.tanksToSpawn.Dequeue();
		}
		
		var positions = new Godot.Collections.Dictionary<int, Vector2>();
		var rotations = new Godot.Collections.Dictionary<int, float>();
		var bulletPositions = new Godot.Collections.Dictionary<ulong, Vector2>();
		var bulletRotations = new Godot.Collections.Dictionary<ulong, float>();

		foreach (var tank in tanks)
		{
			positions.Add(tank.Key, tank.Value.Position);
			rotations.Add(tank.Key, tank.Value.Rotation);
		}

		foreach (var bullet in GetNode<Node2D>("/root/Main/Map")
					 .GetChildren()
					 .OfType<Bullet>())
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
		DespawnTank((int) id);
	}

	private void MultiplayerOnPeerConnected(long id)
	{
		GD.Print($"Peer {id} connected");
		this.tanksToSpawn.Enqueue(((int) id, GetTree().CreateTimer(1.0)));
	}

	private bool TrySpawnTank(int peerId)
	{
		var map = GetParent<Node2D>()
			.GetNode<Node2D>("Map");

		var spawns = map.GetNode<Node2D>("Spawns")
			.GetChildren()
			.OfType<Spawn>()
			.ToImmutableArray();

		Spawn spawn;
		var attempts = 0;

		do
		{
			spawn = spawns[Random.Shared.Next(spawns.Length)];
			attempts++;
		} while (spawn.IsOccupied && attempts < 10);

		if (spawn.IsOccupied)
		{
			return false;
		}

		var tank = TankScene.Instantiate<PlayerTank>();
		GD.Print($"SpawnTank {peerId}");

		tank.Name = $"Tank_{peerId}";
		tank.Id = peerId;
		tank.SetMultiplayerAuthority(peerId);

		tank.Position = spawn.Position;
		tank.Killed += OnKilled;

		GD.Print("Tank configured.");

		map.AddChild(tank);

		GD.Print("Tank added.");

		tanks[peerId] = tank;

		return true;
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

	private void OnKilled(PlayerTank tank)
	{
		DespawnTank(tank.Id);

		this.tanksToSpawn.Enqueue((tank.Id, GetTree().CreateTimer(3.0)));
	}

	public void ReceiveInput(Godot.Collections.Dictionary<string, float> input)
	{
		int peerId = Multiplayer.GetRemoteSenderId();

		if (tanks.TryGetValue(peerId, out var tankNode))
		{
			tankNode.ApplyInput(input);
		}
	}
}
