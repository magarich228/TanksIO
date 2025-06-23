using Godot;
using System;

public partial class Main : Node2D
{
	public const string ServerEnvironment = "tanksio_server";
	
	public override void _Ready()
	{
		bool isServer = OS.HasEnvironment(ServerEnvironment) || OS.HasFeature(ServerEnvironment);

		GD.Print($"{ServerEnvironment}: {isServer}");
		
		Node node = isServer ? 
			ResourceLoader.Load<PackedScene>("res://server/Server.tscn").Instantiate() : 
			ResourceLoader.Load<PackedScene>("res://client/Client.tscn").Instantiate();

		if (!isServer)
		{
			var clientPlayer = ResourceLoader.Load<PackedScene>("res://client/player/Player.tscn");
			AddChild(clientPlayer.Instantiate());
		}
		
		AddChild(node);
	}
}
