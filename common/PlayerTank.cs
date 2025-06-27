using Godot;
using Godot.Collections;

// TODO: пофиксить игру при нескольких игроках
public partial class PlayerTank : CharacterBody2D
{
	[Export] public float MoveSpeed = 300f;
	[Export] public float RotateSpeed = 1.5f;

	private Dictionary<string, float> currentInput = new();

	private NetworkManager networkManager;

	public override void _EnterTree()
	{
		this.networkManager = GetNode<NetworkManager>("/root/NetworkManager");

		base._EnterTree();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Multiplayer.IsServer())
		{
			GD.Print("Server PlayerTank physics process.");
			
			var direction = new Vector2(0, -1).Rotated(Rotation);

			currentInput.TryGetValue("move", out var move);
			currentInput.TryGetValue("rotate", out var rotate);
			
			Velocity = direction * move * MoveSpeed;
			Rotation += rotate * RotateSpeed * (float) delta;

			MoveAndSlide();

			Rpc(nameof(UpdateState), Position, Rotation);
			
			GD.Print("Server PlayerTank physics processed.");
		}
		else
		{
			GD.Print("client player physics process.");
			
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

			networkManager.RpcId(1, nameof(NetworkManager.ReceiveInput), input);

			GD.Print("Receive input sended.");
		}
	}

	public void ApplyInput(Dictionary<string, float> input)
	{
		currentInput = input;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void UpdateState(Vector2 position, float rotation)
	{
		GD.Print($"UpdateState: {position}, {rotation}");

		if (!Multiplayer.IsServer())
		{
			Position = Position.Lerp(position, 0.2f);
			Rotation = Mathf.LerpAngle(Rotation, rotation, 0.2f);

			this.networkManager.ResetTimeout();
			GD.Print($"Updated: {position}, {rotation}");
		}
	}
}
