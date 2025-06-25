using Godot;

public partial class NetworkManager : Node
{
	private Server server;

	public override void _EnterTree()
	{
		GD.Print("NetworkManager loaded.");

		base._EnterTree();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void ReceiveInput(Godot.Collections.Dictionary<string, float> input)
	{
		if (Multiplayer.IsServer())
		{
			if (this.server is null)
			{
				this.server = GetNode<Server>("/root/Main/Server");
				GD.Print("server network manager.");
			}
			
			this.server.ReceiveInput(input);
		}
	}
}
