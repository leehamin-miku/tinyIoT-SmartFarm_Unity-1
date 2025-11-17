using NativeWebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


public class WebSocketDemo : MonoBehaviour {

	// Use this for initialization
        static WebSocket websocket;

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
                Debug.Log("Connection closed!"+e);
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
        Debug.Log("Ä¿³ØÆ® ¿Ï");
            
        }

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
#endif
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SendWebSocketMessage();
            SendWebSocketMessage();
            SendWebSocketMessage();
            SendWebSocketMessage();
        }
    }

        static void SendWebSocketMessage()
        {
            
            //// Sending bytes
            //await websocket.Send(new byte[] { 10, 20, 30 });

            // Sending plain text
            JObject jobject = new JObject
            {
                ["op"] = 1,
                ["to"] = "/TinyIoT/TinyFarm",
                ["fr"] = "CAdmin",
                ["rqi"] = "req12345",
                ["ty"] = 3,
                ["rvi"] = "3",
                ["pc"] = new JObject
                {
                    ["m2m:cnt"]= new JObject{
                    ["rn"] = "Sensors",
                    ["mni"]= 5
                }
        }
            };
            websocket.SendText(jobject.ToString());
        
        }

  private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}
