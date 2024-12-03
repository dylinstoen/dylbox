using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using System;
public class SocketIOManager : MonoBehaviour
{
	private SocketIOUnity socket;

	private void Start()
	{
		InitializeSocket("ws://localhost:8524");
	}

	private void InitializeSocket(string serverUri)
	{
		// Initialize SocketIO with default options
		var options = new SocketIOOptions
		{
			Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
		};

		socket = new SocketIOUnity(serverUri, options);
		
		// Set up event handlers
		socket.OnConnected += (sender, e) =>
		{
			Debug.Log("Connected to WebSocket server");
		};

		socket.OnDisconnected += (sender, e) =>
		{
			Debug.Log("Disconnected from WebSocket server");
		};

		socket.On("player_joined", response =>
		{
			Debug.Log("Player joined the game!");
		});

		socket.On("game_update", response =>
		{
			Debug.Log("Game state updated!");
		});

		// Handle other unknown messages
		socket.OnAny((eventName, data) =>
		{
			Debug.LogWarning($"Unknown message type received: {eventName}");
		});

		// Connect to the server
		socket.Connect();
	}

	public void SendMessage(string eventName, string message)
	{
		if (socket.Connected)
		{
			socket.Emit(eventName, message);
		}
		else
		{
			Debug.LogError("Unable to send message, not connected to WebSocket server.");
		}
	}

	private void OnDestroy()
	{
		if (socket != null)
		{
			socket.Disconnect();
			socket.Dispose();
		}
	}
}
