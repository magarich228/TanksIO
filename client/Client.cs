using Godot;

public partial class Client : Node
{
	[Export]
	public int Port = 9001;

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
		GD.Print($"Peer {id} disconnected (client)");
	}

	private void MultiplayerOnPeerConnected(long id)
	{
		GD.Print($"Peer {id} connected (client)");
	}
}
