using Godot;
using System;

public partial class Main : Node2D
{
	public string ServerEnvironment = "tanksio_server";
	
	private PackedScene serverScene = ResourceLoader.Load<PackedScene>("res://server/Server.tscn");
	private PackedScene clientScene = ResourceLoader.Load<PackedScene>("res://client/Client.tscn");
	
	public override void _Ready()
	{
		bool isServer = OS.HasEnvironment(ServerEnvironment);

		GD.Print($"{ServerEnvironment}: {isServer}");
		
		Node node = isServer ? 
			serverScene.Instantiate() : 
			clientScene.Instantiate();
		
		AddChild(node);
	}
}
