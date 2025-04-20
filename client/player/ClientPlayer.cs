using Godot;
using Godot.Collections;

public partial class ClientPlayer : CharacterBody2D
{
	public override void _PhysicsProcess(double delta)
	{
		// if (!Multiplayer.IsServer())
		{
			var input = new Dictionary<string, float>
			{
				{ "move", Input.GetAxis("move_backward", "move_forward") },
				{ "rotate", Input.GetAxis("rotate_left", "rotate_right") }
			};
			
			RpcId(1, "ReceiveInput", input);
		}
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void UpdateState(Vector2 position, float rotation)
	{
		Position = this.Position.Lerp(position, 0.2f);
		Rotation = Mathf.LerpAngle(this.Rotation, rotation, 0.2f);
	}
}
