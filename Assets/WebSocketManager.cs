using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class WebSocketManager : MonoBehaviour
{
    private ClientWebSocket webSocket;

    private async void Start()
    {
        await ConnectToServer("ws://localhost:8524");
    }

    private async Task ConnectToServer(string serverUri)
    {
        webSocket = new ClientWebSocket();
        try
        {
            await webSocket.ConnectAsync(new Uri(serverUri), CancellationToken.None);
            Debug.Log("Connected to WebSocket server");
            await ReceiveMessages();
        }
        catch (Exception e)
        {
            Debug.LogError($"WebSocket connection failed: {e.Message}");
        }
    }

    private async Task ReceiveMessages()
    {
        var buffer = new byte[1024];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleMessage(message);
            }
        }
    }

    private void HandleMessage(string message)
    {
        try
        {
            var json = JObject.Parse(message);
            string messageType = json["type"]?.ToString();

            switch (messageType)
            {
                case "player_joined":
                    Debug.Log("Player joined the game!");
                    break;

                case "game_update":
                    Debug.Log("Game state updated!");
                    break;

                default:
                    Debug.LogWarning("Unknown message type received");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse message: {e.Message}");
        }
    }

    public async Task SendMessage(string message)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            var encodedMessage = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(encodedMessage), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private async void OnDestroy()
    {
        if (webSocket != null)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            webSocket.Dispose();
        }
    }
}
