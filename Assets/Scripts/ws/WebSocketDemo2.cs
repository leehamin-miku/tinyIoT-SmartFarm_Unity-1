using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NativeWebSocket;


public class WebSocketDemo : MonoBehaviour {

	// Use this for initialization
        WebSocket websocket;

        // Start is called before the first frame update
        async void Start()
        {
            websocket = new WebSocket("ws://192.168.82.234:8081");

            websocket.OnOpen += () =>
            {
                Debug.Log("Connection open!");
            };

            websocket.OnError += (e) =>
            {
                Debug.Log("Error! " + e);
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("Connection closed!");
            };

            websocket.OnMessage += (bytes) =>
            {
                //getting the message as a string
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("On string Message! " + message);
            };

            // Keep sending messages at every 0.3s
            //InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);

            // waiting for messages
            await websocket.Connect();
        }

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
#endif
        }

        async void SendWebSocketMessage()
        {
            if (websocket.State == WebSocketState.Open)
            {
                //// Sending bytes
                //await websocket.Send(new byte[] { 10, 20, 30 });

                //// Sending plain text
                //await websocket.SendText("plain text message");
            }
        }

  private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}
