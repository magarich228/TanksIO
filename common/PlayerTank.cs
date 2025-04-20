using System.Collections.Generic;
using Godot;

public partial class PlayerTank : CharacterBody2D
{
	[Export] public float MoveSpeed = 300f;
	[Export] public float RotateSpeed = 1.5f;
	
	private Dictionary<string, float> currentInput = new();

	public override void _PhysicsProcess(double delta)
	{
		if (IsMultiplayerAuthority())
		{
			var direction = new Vector2(0, -1).Rotated(Rotation);
			Velocity = direction * currentInput.GetValueOrDefault("move", 0f) * MoveSpeed;
			Rotation += currentInput.GetValueOrDefault("rotate", 0f) * RotateSpeed * (float)delta;
			
			MoveAndSlide();
			
			Rpc(nameof(UpdateState), Position, Rotation);
		}
	}

	public void ApplyInput(Dictionary<string, float> input)
	{
		currentInput = input;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void UpdateState(Vector2 position, float rotation)
	{
		if (!IsMultiplayerAuthority())
		{
			Position = Position.Lerp(position, 0.2f);
			Rotation = Mathf.LerpAngle(Rotation, rotation, 0.2f);
		}
	}
}
