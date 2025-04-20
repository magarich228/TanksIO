using Godot;
using System;

public partial class Server : Node
{
	[Export]
	public int Port = 9001;

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
	}

	private void MultiplayerOnPeerConnected(long id)
	{
		GD.Print($"Peer {id} connected");
	}
}
