using Godot;
using Godot.Collections;

public partial class ClientPlayer : CharacterBody2D
{
	private Server server;
	
	public override void _Ready()
	{
		this.server = ResourceLoader.Load<PackedScene>("res://server/Server.tscn").Instantiate<Server>();
		AddChild(this.server);
	}
	
	public override void _PhysicsProcess(double delta)
	{
		GD.Print("Client player physics process.");
		
		if (!Multiplayer.IsServer() || Multiplayer.GetUniqueId() != 1)
		{
			var connectionStatus = Multiplayer.MultiplayerPeer.GetConnectionStatus();

			if (connectionStatus != MultiplayerPeer.ConnectionStatus.Connected)
			{
				GD.Print($"Connection status: {connectionStatus}");
				return;
			}
			
			var input = new Dictionary<string, float>
			{
				["move"] = Input.GetAxis("move_backward", "move_forward"),
				["rotate"] = Input.GetAxis("rotate_left", "rotate_right")
			};
			
			this.server.RpcId(1, nameof(Server.ReceiveInput), input);
			GD.Print("Receive input sended.");
		}
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void UpdateState(Vector2 position, float rotation)
	{
		Position = this.Position.Lerp(position, 0.2f);
		Rotation = Mathf.LerpAngle(this.Rotation, rotation, 0.2f);
	}
}
