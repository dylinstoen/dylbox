using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using TMPro;
using System.Collections;
using Unity.Collections;

public class UnitySocketManager : MonoBehaviour
{
	private SocketIOUnity socket;
	[SerializeField] private TMP_Text roomCodeText;
	[SerializeField] private TMP_Text playerListText;
	private List<string> players = new List<string>();

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}
	private void Start()
	{
		roomCodeText.text = "Hello";
		InitializeSocket("http://localhost:3003");
	}

	private void InitializeSocket(string serverUri)
	{
		var options = new SocketIOClient.SocketIOOptions
		{
			Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
		};

		socket = new SocketIOUnity(serverUri, options);

		socket.OnConnected += (sender, e) =>
		{
			Debug.Log("Connected to server");
			CreateRoom();
		};

		// Room created event
		socket.On("ROOM_CREATED", response =>
		{
			var data = response.GetValue<Dictionary<string, string>>();
			data.TryGetValue("roomCode", out string roomCode);
			Debug.Log($"Room created with code: {roomCode}");
			if (data.TryGetValue("roomCode", out string code))
			{
				Debug.Log($"Room created with code: {code}");
				UnityThread.executeInUpdate(() => {
					UpdateRoomCodeText(code);
				});
				
			}

		});

		// Player joined event
		socket.On("PLAYER_JOINED", response =>
		{
			var data = response.GetValue<Dictionary<string, string>>();
			data.TryGetValue("nickname", out string nickname);
			Debug.Log($"Player joined: {nickname}");
			UnityThread.executeInUpdate(() =>
			{
				players.Add(nickname);
				UpdatePlayerList();
			});
		});

		// Player left event
		socket.On("PLAYER_LEFT", response =>
		{
			var data = response.GetValue<Dictionary<string, string>>();
			data.TryGetValue("nickname", out string nickname);
			Debug.Log($"Player left: {nickname}");
			UnityThread.executeInUpdate(() =>
			{
				players.Remove(nickname);
				UpdatePlayerList();
			});

		});
		socket.On("PLAYER_JOINED_ACK", response =>
		{
			Debug.Log("Hello");
		});
		socket.Connect();
	}

	private void UpdatePlayerList()
	{
		if (playerListText != null)
		{
			playerListText.text = "Players:\n" + string.Join("\n", players);
		}
		else
		{
			Debug.LogError("Player list Text element is not assigned!");
		}
	}
	private void UpdateRoomCodeText(string roomCode)
	{
		if (roomCodeText != null)
		{
			roomCodeText.text = $"Room Code: {roomCode}";
		}
		else
		{
			Debug.LogError("Room code Text element is not assigned!");
		}
	}
	private void CreateRoom()
	{
		socket.Emit("CREATE_ROOM");
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
