using System;
using Best.SignalR;
using Best.SignalR.Encoders;
using UnityEngine;


public class SignalRManager : MonoBehaviour
{
	private HubConnection hub;

	void Start()
	{
		HubOptions options = new HubOptions();
		
		hub = new HubConnection(new Uri("https://server/hub"), new JsonProtocol(new LitJsonEncoder()), options);
		hub.ReconnectPolicy = new DefaultRetryPolicy(new TimeSpan?[] {
			TimeSpan.FromSeconds(5),
			TimeSpan.FromSeconds(15),
			TimeSpan.FromSeconds(45),
			TimeSpan.FromSeconds(90),
			null
		});
		hub.OnConnected += (hub) => Debug.Log("Connected!");
		hub.StartConnect();
	}


}
