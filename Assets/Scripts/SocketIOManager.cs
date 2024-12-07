using System;
using UnityEngine;
using Best.SocketIO; // Namespace from the Best SocketIO plugin
using System.Collections.Generic;
using Best.SocketIO.Events;
using TMPro;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Pkcs;

public class GameNetworkManager : MonoBehaviour
{
	public string serverURL = "http://localhost:3000"; // Adjust to your server URL
	public TMP_Text roomCodeText;
	public TMP_Text playersListText;
	private SocketManager manager;
	void Start()
	{
		ConnectToServer();
	}

	void ConnectToServer()
	{
		SocketOptions options = new SocketOptions { AutoConnect = false };
		manager = new SocketManager(new Uri(serverURL), options);

		manager.Socket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
		manager.Socket.On<string>("room_created", (arg1) => roomCodeText.text = "Room Code: " + arg1);
		manager.Socket.On<RoomUpdateData>("room_update", OnRoomUpdate);
		
		manager.Open();
	}
	private void OnConnected(ConnectResponse resp)
	{
		manager.Socket.Emit("host_room");
	}

	private void OnRoomUpdate(RoomUpdateData data)
	{
		playersListText.text = "Players in Room:\n";
		foreach (var playerName in data.players)
		{
			playersListText.text += playerName + "\n";
		}
	}

	void OnDestroy()
	{
		manager?.Close();
		manager = null;
	}
}