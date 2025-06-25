using Godot;
using System.Collections.Generic;

public partial class PlayerTank : CharacterBody2D
{
	[Export] public float MoveSpeed = 300f;
	[Export] public float RotateSpeed = 1.5f;
	
	private Godot.Collections.Dictionary<string, float> currentInput = new();

	public override void _PhysicsProcess(double delta)
	{
		GD.Print("PlayerTask physics process.");
		
		if (Multiplayer.IsServer())
		{
			var direction = new Vector2(0, -1).Rotated(Rotation);
			Velocity = direction * currentInput.GetValueOrDefault("move", 0f) * MoveSpeed;
			Rotation += currentInput.GetValueOrDefault("rotate", 0f) * RotateSpeed * (float)delta;
			
			MoveAndSlide();
			
			Rpc(nameof(UpdateState), Position, Rotation);
		}
		
		GD.Print("PlayerTask physics processed.");
	}

	public void ApplyInput(Godot.Collections.Dictionary<string, float> input)
	{
		currentInput = input;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void UpdateState(Vector2 position, float rotation)
	{
		GD.Print($"UpdateState: {position}, {rotation}");
		
		if (!IsMultiplayerAuthority())
		{
			Position = Position.Lerp(position, 0.2f);
			Rotation = Mathf.LerpAngle(Rotation, rotation, 0.2f);
			
			GD.Print($"Updated: {position}, {rotation}");
		}
	}
}
