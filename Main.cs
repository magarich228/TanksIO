using Godot;

public partial class Main : Node2D
{
	private bool isServer;
	
	public const string ServerEnvironment = "tanksio_server";

	public bool IsServer => isServer;
	
	public override void _Ready()
	{
		isServer = OS.HasEnvironment(ServerEnvironment) || OS.HasFeature(ServerEnvironment);

		GD.Print($"{ServerEnvironment}: {isServer}");
		
		Node node = isServer ? 
			ResourceLoader.Load<PackedScene>("res://server/Server.tscn").Instantiate() : 
			ResourceLoader.Load<PackedScene>("res://client/Client.tscn").Instantiate();
		
		AddChild(node);
	}
}
