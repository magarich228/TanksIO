using System;
using Godot;
using Godot.Collections;

public partial class NetworkManager : Node
{
	private Server _server;
	private Label _connectionStatusLabel;
	
	private float _connectionTimeout = 0f;
	private const float CONNECTION_TIMEOUT = 10.0f;
	
	private ulong _lastPingTime;
	private float _pingTimer;
	private float _currentPing;
	
	public bool IsServerConnected { get; private set; }
	public event Action<Dictionary<int, Vector2>, Dictionary<int, float>> OnUpdateGameState; 

	public override void _Ready()
	{
		GD.Print("NetworkManager loaded.");
		_connectionStatusLabel = GetNode<Label>("/root/Main/UI/PingLabel");
		
		base._Ready();
	}

	public override void _Process(double delta)
	{
		if (!Multiplayer.IsServer())
		{
			if (IsConnectedToServer())
			{
				_connectionTimeout += (float) delta;

				if (_connectionTimeout > CONNECTION_TIMEOUT)
				{
					GD.Print("Connection timeout! Disconnecting...");
					Multiplayer.MultiplayerPeer.Close();
					_connectionTimeout = 0f;
				}
			}
			
			_pingTimer += (float) delta;

			if (_pingTimer > 5.0f)
			{
				_pingTimer = 0;
				SendPing();
			}
			
			UpdateConnectionStatus();
		}
	}

	public bool IsConnectedToServer()
	{
		if (Multiplayer.MultiplayerPeer == null)
			return false;
	
		return Multiplayer.MultiplayerPeer.GetConnectionStatus() == 
			   MultiplayerPeer.ConnectionStatus.Connected;
	}
	
	public void ResetTimeout()
	{
		_connectionTimeout = 0f;
	}
	
	private void SendPing()
	{
		if (!IsConnectedToServer()) 
			return;
	
		_lastPingTime = Time.GetTicksMsec();
		RpcId(1, nameof(Ping), _lastPingTime);
	}
	
	private void UpdateConnectionStatus()
	{
		if (IsConnectedToServer())
		{
			_connectionStatusLabel.Text = $"Connected | Ping: {_currentPing:F1}ms";
			_connectionStatusLabel.Modulate = Colors.Green;
			IsServerConnected = true;
		}
		else
		{
			_connectionStatusLabel.Text = "Disconnected";
			_connectionStatusLabel.Modulate = Colors.Red;
			IsServerConnected = false;
		}
	}
	
	[Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void UpdateGameState(Dictionary<int, Vector2> positions, Dictionary<int, float> rotations)
	{
		OnUpdateGameState?.Invoke(positions, rotations);
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void ReceiveInput(Dictionary<string, float> input)
	{
		if (Multiplayer.IsServer())
		{
			if (this._server is null)
			{
				this._server = GetNode<Server>("/root/Main/Server");
				GD.Print("server network manager.");
			}
			
			this._server.ReceiveInput(input);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void Ping(ulong timestamp)
	{
		long peerId = Multiplayer.GetRemoteSenderId();
		RpcId(peerId, nameof(Pong), timestamp);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void Pong(ulong timestamp)
	{
		if (timestamp == _lastPingTime)
		{
			_currentPing = (Time.GetTicksMsec() - timestamp) / 1000.0f;
			GD.Print($"Ping: {_currentPing:F3} seconds");
		}
		
		ResetTimeout();
	}
}
