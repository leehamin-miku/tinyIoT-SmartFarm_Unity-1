using UnityEngine;
using IoT;
using System.IO;
using System.Text;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json.Linq;

public class Sensors : MonoBehaviour
{
    public float timeSpan = 5;
    private float time = 0;
    public float temp = 24;
    public float lux = 200;
    public float db = 70;
    public float humid = 20;
    public float syncSpan = 600;
    public Light bulb;
    public Slider tempSlider, dbSlider, luxSlider, humidSlider;
    public TMP_Text tempText, dbText, luxText, humidText, info;
    public GameObject server;

    private void SetInfo(string ip, int r, int g, int b)
    {
        string text = $"Bulb Color : ({r}, {g}, {b})\nMN HOST : {ip}\nRestaurant Mood tracker Simulator 1.0.0";
        info.text = text;
    }

    public static string GetLocalIPv4()
    {
        string localIP = "";
        try
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in interfaces)
            {
                if (adapter.OperationalStatus == OperationalStatus.Up &&
                    (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    foreach (UnicastIPAddressInformation ip in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ip.Address))
                        {
                            localIP = ip.Address.ToString();
                            return localIP;
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(localIP))
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    localIP = endPoint.Address.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get local IP: {ex.Message}");
            return "127.0.0.1";
        }

        return localIP;
    }

    private IEnumerator Start()
    {
        FileStream fs = new FileStream("config.ini", FileMode.Open, FileAccess.Read);
        StreamReader streamReader = new StreamReader(fs);
        OneM2M.baseUrl = streamReader.ReadLine().Replace(System.Environment.NewLine, "");
        NotificationServer.port = int.Parse(streamReader.ReadLine().Replace(System.Environment.NewLine, ""));
        SetInfo(OneM2M.baseUrl, (int)bulb.GetComponent<Light>().color.r, (int)bulb.GetComponent<Light>().color.g, (int)bulb.GetComponent<Light>().color.b);

        yield return StartCoroutine(CreateAE("Sensor", "CSensors"));
        yield return StartCoroutine(CreateAE("SmartBulb", "CSmartBulb"));
        yield return StartCoroutine(CreateSubscription("command", $"http://192.168.0.12:{NotificationServer.port}/notifi"));

        StartCoroutine(syncData());
    }

    public static string EscapeJsonString(string rawString)
    {
        if (string.IsNullOrEmpty(rawString)) return rawString;

        return rawString.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\b", "\\b").Replace("\f", "\\f").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    private void Update()
    {
        SetInfo(OneM2M.baseUrl, (int)(bulb.color.r * 255), (int)(bulb.color.g * 255), (int)(bulb.color.b * 255));
        temp = (float)Math.Round(tempSlider.value, 1);
        db = (float)Math.Round(dbSlider.value, 1);
        lux = (float)Math.Round(luxSlider.value, 1);
        humid = (float)Math.Round(humidSlider.value, 1);
        tempText.text = temp + " °C";
        dbText.text = db + " db";
        luxText.text = lux + " lux";
        humidText.text = humid + " %";
        bulb.intensity = lux / 100;
        time += Time.deltaTime;

        if (OneM2M.checkCommand)
        {
            OneM2M.checkCommand = false;
            Debug.Log("Check command");
            StartCoroutine(OneM2M.GetDataCoroutine("CSmartBulb", "osori", "SmartBulb/command/la", (result) =>
            {
                Debug.Log(result);
                JObject json = JObject.Parse(result);
                json = JObject.Parse(json["m2m:cin"]["con"].ToString());
                luxSlider.value = float.Parse(json["lux"].ToString());
                float r = float.Parse(json["r"].ToString()) / 255f;
                float g = float.Parse(json["g"].ToString()) / 255f;
                float b = float.Parse(json["b"].ToString()) / 255f;
                bulb.color = new Color(r, g, b);
            }));
        }
    }

    IEnumerator syncData()
    {
        while (true)
        {
            yield return StartCoroutine(CreateTimeSeriesInstance("temperature", $"{{\"temp\":{temp}}}"));
            yield return StartCoroutine(CreateTimeSeriesInstance("noise", $"{{\"noise\":{db}}}"));
            yield return StartCoroutine(CreateTimeSeriesInstance("humid", $"{{\"humidity\":{humid}}}"));
            yield return new WaitForSeconds(timeSpan);
        }
    }

    IEnumerator CreateAE(string name, string origin)
    {
        string requestBody = "{ \"m2m:ae\": {" +
            $"\"rn\": \"{name}\"," +
            "\"api\": \"N_Sensor_AE\"," +
            "\"rr\": true," +
            "\"srv\": [\"3\"]" +
            "} }";

        yield return OneM2M.PostDataCoroutine(origin, 2, requestBody, "osori", "", (res) => {
            Debug.Log("CreateAE Response: " + res);
        });
    }

    IEnumerator CreateTimeSeriesInstance(string containerName, string content)
    {
        string requestBody = "{ \"m2m:tsi\": {" +
            $"\"con\": \"{EscapeJsonString(content)}\"," +
            $"\"dgt\": \"{DateTime.UtcNow:yyyyMMddTHHmmss}\"" +
            "} }";

        Debug.Log(requestBody);
        yield return OneM2M.PostDataCoroutine("CSensors", 30, requestBody, "osori", $"Sensor/{containerName}", (res) => {
            Debug.Log("TSI Response: " + res);
        });
    }

    IEnumerator CreateSubscription(string containerName, string notificationUrl)
    {
        string requestBody = "{ \"m2m:sub\": {" +
            $"\"rn\": \"SUB_{containerName}\"," +
            "\"enc\": { \"net\": [3] }," +
            $"\"nu\": [\"{notificationUrl}\"]," +
            "\"nct\": 1" +
            "} }";

        Debug.Log(requestBody);
        yield return OneM2M.PostDataCoroutine("CSmartBulb", 23, requestBody, "osori", $"SmartBulb/{containerName}", (res) => {
            Debug.Log("Subscription Response: " + res);
        });
    }
}