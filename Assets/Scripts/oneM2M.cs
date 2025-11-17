using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace IoT
{
    public class OneM2M : MonoBehaviour
    {
        static OneM2M _instance;
        string IP;
        string baseHTTP
        {
            get
            {
                return "http://" + IP + ":3000";
            }
        }
        string baseWS
        {
            get
            {
                return "ws://" + IP + ":8081";
            }
        }
        //public static string baseUrl = "http://127.0.0.1:3000/TinyIoT";
        public bool checkCommand = false;
        
        OneM2MConnection connection;
        FarmParameter farmParameter;
        [SerializeField] float refreshInterval;
        [SerializeField] bool useAutoRefresh;
        [SerializeField] int protocolIndex; //0 : http, 1 : ws
        public static float RefreshInterval
        {
            get => _instance.refreshInterval; set => _instance.refreshInterval = value;
        }
        public static bool AutoRefresh
        {
            get => _instance.useAutoRefresh; set => _instance.useAutoRefresh = value;
        }
        public static int ProtocolIndex
        {
            get => _instance.protocolIndex;

        }




        UnityEvent refreshEvent;

        

        //명명백백한 외부용
        static public string GetFarmParameter(EFarmParameter type)
        {
            return _instance.farmParameter.parameterString[(int)type];
        }

        static public void FarmParameterUpToDate()
        {
            if (_instance.connection != null)
            {
                _instance.connection.GetAllParameter();
            }
        }

        #region 외부용인터페이스
        static public void SetFarmParameter(EFarmParameter type, string value)
        {
            _instance.connection.SetParameter(type, value);
        }

        static public void AddValueRefreshEvent(UnityAction action)
        {
            _instance.refreshEvent.AddListener(action);
        }
        static public void AddConnectedEvent(UnityAction action)
        {
            _instance.connectedEvent.AddListener(action);
        }
        static public void AddDisconnectedEvent(UnityAction action)
        {
            _instance.disconnectedEvent.AddListener(action);
        }

        static public void AddConnectingEvent(UnityAction action)
        {
            _instance.connectingEvent.AddListener(action);
        }


        public static void MakeConnectionOrDisconnect()
        {
            if (_instance.connection != null)
            {

                if (_instance.connection.IsConnected)
                {
                    _instance.connection.Disconnect();
                }
                else
                {
                    _instance.connection.TryMakeConnection();
                }

            }
        }

        public static void ConnectionChange(int index)
        {
            if (_instance.protocolIndex == index) return;


            if (_instance.connection != null)
            {
                _instance.connection.Disconnect();
            }

            switch (index)
            {
                case 0:
                    _instance.protocolIndex = index;
                    _instance.connection = new OneM2MHttps();
                    break;
                case 1:
                    _instance.protocolIndex = index;
                    _instance.connection = new OneM2MWebSocket();
                    break;
                default: return;
            }
        }
        #endregion


        private void Awake()
        {
            _instance = this;
            _instance.refreshEvent = new UnityEvent();
            _instance.connectedEvent = new UnityEvent();
            _instance.disconnectedEvent = new UnityEvent();
            _instance.connectingEvent = new UnityEvent();
            _instance.connection = new OneM2MHttps();
            _instance.farmParameter = new FarmParameter();

            string yourDefaultIP = "your_Tiny_IoT_IP";

#if !UNITY_WEBGL || UNITY_EDITOR
            IP = yourDefaultIP;
#else
            string url = Application.absoluteURL;

            // 예: 쿼리 파라미터 추출
            if (url.Contains("?"))
            {
                string queryString = url.Substring(url.IndexOf("?") + 1);
                string[] parameters = queryString.Split('&');
                foreach (string param in parameters)
                {
                    string[] keyValue = param.Split('=');
                    if (keyValue.Length == 2)
                    {
                        if (keyValue[0] == "ip" || keyValue[0] == "IP")
                        {
                            IP = keyValue[1];

                            return;
                        }
                        
                    }
                }
            }


            Debug.LogError("IP를 찾을 수 없음. 기본 아이피로 설정 :"+yourDefaultIP);
            IP = yourDefaultIP;
#endif



        }


        bool isInited = false;
        public void LateUpdate()
        {

            if (isInited) return;
            //연결 시작
            isInited = true;
            OneM2M.MakeConnectionOrDisconnect();
        }

        private void Update()
        {
            connection?.Update();
        }






        UnityEvent disconnectedEvent;
        UnityEvent connectedEvent;
        UnityEvent connectingEvent;

        public enum EFarmParameter
        {
            Temperature,
            Humidity,
            CO2,
            Soil,

            Water,
            LED,
            Fan,
            Door,

            health
        }

        //파라미터
        class FarmParameter
        {


            public string[] parameterString = {
                "",
                "",
                "",
                "",

                "",
                "",
                "",
                "",

                ""
            };

            public string[] ResourcePathString = {
                "Sensors/Temperature",
                "Sensors/Humidity",
                "Sensors/CO2",
                "Sensors/Soil",

                "Actuators/Water",
                "Actuators/LED",
                "Actuators/Fan",
                "Actuators/Door",

                "inference/health"
            };
        }



        public class OneM2MWebSocket : OneM2MConnection
        {
            WebSocket connection;
            Queue<string> sendQueue;
            Queue<string> receiveQueue;


            override public void SetParameter(EFarmParameter type, string value)
            {
                JObject jobject = new JObject
                {
                    ["op"] = 1,
                    ["to"] = "/TinyIoT/TinyFarm/" + _instance.farmParameter.ResourcePathString[(int)type],
                    ["fr"] = "CAdmin",
                    ["rqi"] = "req12345",
                    ["ty"] = 4,
                    ["rvi"] = "3",
                    ["pc"] = new JObject
                    {
                        ["m2m:cin"] = new JObject
                        {
                            ["con"] = value
                        }
                    }
                };
                if (connection.State == WebSocketState.Open)
                {
                    string jsonString = jobject.ToString(Formatting.Indented);
                    sendQueue.Enqueue(jsonString);
                }

            }
            override public void GetParameter(EFarmParameter type)
            {
                JObject jobject = new JObject
                {
                    ["op"] = 2,
                    ["to"] = "/TinyIoT/TinyFarm/" + _instance.farmParameter.ResourcePathString[(int)type] + "/la",
                    ["fr"] = "CAdmin",
                    ["rqi"] = "req12345",
                    ["ty"] = 4,
                    ["rvi"] = "3",
                };
                if (connection.State == WebSocketState.Open)
                {
                    string jsonString = jobject.ToString(Formatting.Indented);
                    sendQueue.Enqueue(jsonString);
                }
            }

            TaskCompletionSource<bool> connectTask;
            override protected async Task ConnectSubFunction()
            {
                if (connection.State == WebSocketState.Connecting || connection.State == WebSocketState.Open)
                {
                    throw new Exception("Already Connected");
                }

                connectTask = new TaskCompletionSource<bool>();

                connection.Connect();
                //Debug.Log("응?");
                // 코루틴이 완료될 때까지 대기
                await connectTask.Task;
                //Debug.Log("ConnectSubFunction 완료");
            }


            async override protected Task DisconnectSubFuction()
            {
                if (connection.State == WebSocketState.Closed || connection.State == WebSocketState.Closing)
                {
                    throw new Exception("Already Disconnected");
                }


                await connection.Close();
            }

            void OnMessageNotRespone(string recive)
            {
                JObject json = JObject.Parse(recive);
                
                JToken pc;
                JToken token;


                if (json["m2m:sgn"] != null)
                {
                    Debug.Log("구독알림"+recive);
                    token = json["m2m:sgn"];
                    if (token != null)
                    {
                        //응?


                        //내 구독자에 대한 응답이라면 변환
                        if (token["sur"] == null)
                        {
                            //구독 만료에 대한 응답 아마도
                            return;
                        }

                        string link = token["sur"].ToString();
                        int idx = link.LastIndexOf('/');
                        string result = (idx >= 0) ? link.Substring(0, idx) : link;


                        for (int i = 0; i < Enum.GetValues(typeof(EFarmParameter)).Length; i++)
                        {
                            if (result.Contains(_instance.farmParameter.ResourcePathString[i]))
                            {
                                token = token["nev"]["rep"]["m2m:cin"];

                                if (token != null)
                                {
                                    string content = token["con"].ToString();
                                    if(content == "data")
                                    {
                                        //inference의 경우를 예외처리하여 파싱
                                        content = token["lbl"][0].ToString();
                                    }
                                    _instance.farmParameter.parameterString[i] = content;
                                    _instance.refreshEvent?.Invoke();
                                }
                                break;
                            }
                        }



                    }


                    return;
                }











                int rsc = (int.Parse((json["rsc"].ToString())));

                //Debug.Log(recive);
                switch (rsc)
                {
                    case 2000: //RETRIEVE, NOTIFY에 대한 응답
                        pc = json["pc"];
                        token = pc["m2m:dbg"];
                        if (token != null)
                        {
                            Debug.LogError(token.ToString());
                            break;
                        }

                        token = pc["m2m:cin"];
                        if (token != null)
                        {
                            //RETRIEVE
                            string to = json["to"].ToString();

                            for (int i = 0; i < Enum.GetValues(typeof(EFarmParameter)).Length; i++)
                            {
                                if (to.Contains(_instance.farmParameter.ResourcePathString[i]))
                                {
                                    _instance.farmParameter.parameterString[i] = token["con"].ToString();
                                    _instance.refreshEvent?.Invoke();
                                    //Debug.Log(_instance.farmParameter.ResourceString[i] + " = " + _instance.farmParameter.parameterString[i]);
                                    return;
                                }
                            }




                            Debug.LogError("가정되지 못한 to 값");

                            break;
                        }

                        

                        break;
                    case 2001: //cin Create 요청에 대한 응답
                        pc = json["pc"];
                        token = pc["m2m:dbg"];
                        if (token != null)
                        {
                            Debug.LogError(token.ToString());
                            break;
                        }

                        token = pc["m2m:cin"];
                        if (token != null)
                        {
                            //RETRIEVE
                            string to = json["to"].ToString();

                            for (int i = 0; i < Enum.GetValues(typeof(EFarmParameter)).Length; i++)
                            {
                                if (to.Contains(_instance.farmParameter.ResourcePathString[i]))
                                {
                                    _instance.farmParameter.parameterString[i] = token["con"].ToString();
                                    _instance.refreshEvent?.Invoke();
                                    Debug.Log(_instance.farmParameter.ResourcePathString[i] + " = " + _instance.farmParameter.parameterString[i]);
                                    return;
                                }
                            }




                            Debug.LogError("가정되지 못한 to 값");

                            break;
                        }

                        Debug.LogError(pc.ToString());
                        break;

                    case 2004: //UPDATE 요청에 대한 응답

                        break;
                    default:
                        Debug.LogError("?"+ rsc+"\n"+ json.ToString());
                        return;

                }


            }
            bool taskIsRunning = false;
            override public void Update()
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                connection.DispatchMessageQueue();
#endif

                if (sendQueue.Count > 0 && !taskIsRunning)
                {
                    //어서 일해라!
                    taskIsRunning = true;
                    ResolveQuque();
                }


                if (receiveQueue.Count > 0)
                {
                    string json = receiveQueue.Dequeue();
                    OnMessageNotRespone(json);
                }
            }

            async void ResolveQuque()
            {

                if(connection.State == WebSocketState.Open)
                {
                    string json = sendQueue.Dequeue();
                    
                    await connection.SendText(json);
                    while (sendQueue.Count > 0)
                    {
                        await Task.Delay(100);
                        if (connection.State != WebSocketState.Open) break;
                        json = sendQueue.Dequeue();
                        await connection.SendText(json);
                    }
                    
                } else
                {
                    sendQueue.Clear();
                }

                taskIsRunning = false;
            }

            public OneM2MWebSocket()
            {
                connection = new WebSocket(_instance.baseWS);
                connection.OnOpen += () =>
                {
                    Debug.Log("Connection open!");
                    connectTask.SetResult(true);
                    //sendQueue.Enqueue(
                    //    "{\r\n    \"op\": 1,\r\n    \"to\": \"/TinyIoT/TinyFarm/Sensors/Soil\",\r\n    \"fr\": \"CAdmin\",\r\n    \"rqi\": \"req-0001\",\r\n    \"ty\": 23,\r\n    \"pc\": {\r\n        \"m2m:sub\": {\r\n            \"rn\": \"Unity-AutoSub\",\r\n            \"enc\": {\r\n                \"net\": [\r\n                    3,4\r\n                ]\r\n            },\r\n            \"nu\": [\r\n                \"ws://localhost:8081\"\r\n            ],\r\n            \"exc\": 10\r\n        }\r\n    },\r\n    \"rvi\": \"3\"\r\n}\r\n"
                    //    );
                };

                connection.OnError += (e) =>
                {
                    Debug.LogError("Error! " + e);
                    Disconnect();
                    connectTask.SetException(new Exception("WebSocket Error"));

                };

                connection.OnClose += (e) =>
                {
                    Debug.Log("Connection closed! : " + e);
                    Disconnect();

                };

                connection.OnMessage += (bytes) =>
                {

                    //getting the message as a string
                    var message = System.Text.Encoding.UTF8.GetString(bytes);
                    //Debug.Log("On string Message! " + message);
                    receiveQueue.Enqueue(message);

                    

                };


                sendQueue = new Queue<string>();
                receiveQueue = new Queue<string>();
            }

        }
        // === 공용 헬퍼 추가 ===

        public static string ToOnOff(bool isOn) => isOn ? "ON" : "OFF";
        public static bool ParseOnOff(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return false;
            raw = raw.Trim();
            return raw.Equals("ON", System.StringComparison.OrdinalIgnoreCase)
                || raw.Equals("1")
                || raw.Equals("true", System.StringComparison.OrdinalIgnoreCase);
        }



        public class OneM2MHttps : OneM2MConnection
        {
            //Coroutine을 await 하기위한 유틸
            public static Task RunCoroutineAsTask(MonoBehaviour owner, IEnumerator coroutine)
            {
                var tcs = new TaskCompletionSource<bool>();
                owner.StartCoroutine(Wrap());
                IEnumerator Wrap()
                {
                    while (coroutine.MoveNext())
                        yield return coroutine.Current;

                    tcs.SetResult(true);
                }
                return tcs.Task;
            }


            override protected async Task ConnectSubFunction()
            {
                exception = new Exception("Http Connect Fail");
                //http 하나 보내서 반응보기
                await RunCoroutineAsTask(OneM2M._instance, ConnectionCheck());
                //에러가 리턴되려면?
                if(exception != null)
                {
                    throw exception;
                }
            }


            async override protected Task DisconnectSubFuction()
            {

            }

            Exception exception;
            IEnumerator ConnectionCheck()
            {
                exception = null;
                yield return _instance.StartCoroutine(GetDataCoroutine(
                    origin: "CAdmin",
                    url: _instance.farmParameter.ResourcePathString[(int)EFarmParameter.Soil] + "/la",
                    callback: (respone) =>
                    {
                        //응?
                        exception = null;
                    }
                ));
            }
            override public void SetParameter(EFarmParameter type, string value)
            {
                string jsonBody = new JObject { ["m2m:cin"] = new JObject { ["con"] = value } }.ToString();

                // 원래 형식 (named args)
                _instance.StartCoroutine(PostDataCoroutine(
                    origin: "CAdmin",
                    type: 4,
                    body: jsonBody,
                    url: _instance.farmParameter.ResourcePathString[(int)type],
                    callback: (respone) =>
                    {
                        var json = JObject.Parse(respone);
                        string raw = json["m2m:cin"]?["con"]?.ToString();
                        if (!string.IsNullOrEmpty(raw))
                        {
                            _instance.farmParameter.parameterString[(int)type] = raw;
                        }
                        _instance.refreshEvent?.Invoke();
                    }
                ));
            }
            override public void GetParameter(EFarmParameter type)
            {
                _instance.StartCoroutine(GetDataCoroutine(
                    origin: "CAdmin",
                    url: _instance.farmParameter.ResourcePathString[(int)type] + "/la",
                    callback: (respone) =>
                    {
                        var json = JObject.Parse(respone);
                        string raw = json["m2m:cin"]?["con"]?.ToString();

                        if (raw == "data")
                        {
                            //inference의 경우를 예외처리하여 파싱
                            raw = json["m2m:cin"]?["lbl"][0].ToString();
                        }


                        if (!string.IsNullOrEmpty(raw))
                        {
                            _instance.farmParameter.parameterString[(int)type] = raw;
                        }
                        _instance.refreshEvent?.Invoke();
                    }
                ));
            }

            private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            }

            //private static void ConfigureHttps()
            //{
            //    if (baseUrl.StartsWith("https"))
            //    {
            //        ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            //    }
            //}
            Exception httpException;
            public IEnumerator PostDataCoroutine(string origin, int type, string body, string token = "", string url = "", Action<string> callback = null)
            {
                string endpoint = url == "" ? _instance.baseHTTP + "/TinyIoT/TinyFarm/" : $"{_instance.baseHTTP}/TinyIoT/TinyFarm/{url}";
                Debug.Log(endpoint);
                System.Random rand = new System.Random();
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

                using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", $"application/json;ty={type}");
                    request.SetRequestHeader("X-M2M-Origin", origin);
                    request.SetRequestHeader("X-M2M-RI", rand.Next().ToString());
                    request.SetRequestHeader("X-M2M-RVI", "3");

                    if (!string.IsNullOrEmpty(token))
                    {
                        request.SetRequestHeader("Authorization", $"Bearer {token}");
                    }

                    yield return request.SendWebRequest();

                    string jsonResponse = request.downloadHandler.text;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        //Debug.Log($"Server response: {jsonResponse}");
                        callback?.Invoke(jsonResponse);
                    }
                    else
                    {
                        Debug.LogError($"Server error: {jsonResponse}");
                        Disconnect();
                    }

                    
                }
            }

            public IEnumerator GetDataCoroutine(string origin, string token = "", string url = "", Action<string> callback = null)
            {
                string endpoint = url == "" ? _instance.baseHTTP + "/TinyIoT/TinyFarm/" : $"{_instance.baseHTTP}/TinyIoT/TinyFarm/{url}";
                System.Random rand = new System.Random();

                using (UnityWebRequest request = UnityWebRequest.Get(endpoint))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Accept", "application/json");
                    request.SetRequestHeader("X-M2M-Origin", origin);
                    request.SetRequestHeader("X-M2M-RI", rand.Next().ToString());
                    //request.SetRequestHeader("X-M2M-RI", "retrieve_cnt");
                    request.SetRequestHeader("X-M2M-RVI", "3");

                    if (!string.IsNullOrEmpty(token))
                    {
                        request.SetRequestHeader("Authorization", $"Bearer {token}");
                    }

                    yield return request.SendWebRequest();

                    string jsonResponse = request.downloadHandler.text;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        //Debug.Log($"Server response: {url + "\ncon:" + (string)JObject.Parse(jsonResponse)["m2m:cin"]["con"]}");
                        callback?.Invoke(jsonResponse);
                    }
                    else
                    {
                        //Debug.LogError($"Server error: {endpoint}");
                        Debug.LogError($"Server error: {jsonResponse}");
                        //httpException = new Exception("Http Failed");
                        Disconnect();
                    }

                    
                }
            }

            //    public static IEnumerator CreateSubscription(
            //string origin, string targetUrl, string subName, string notiUri, Action<string> callback = null)
            //    {
            //        string endpoint = $"{_instance.baseUrl}/{targetUrl}";
            //        Debug.Log("구독 endpoint:" + endpoint);

            //        var sub = new JObject
            //        {
            //            ["m2m:sub"] = new JObject
            //            {
            //                ["rn"] = subName,
            //                ["nu"] = new JArray(notiUri),
            //                ["enc"] = new JObject { ["net"] = new JArray(3, 4) },
            //                ["exc"] = 10
            //            }
            //        };

            //        print(sub);

            //        byte[] bodyRaw = Encoding.UTF8.GetBytes(sub.ToString());

            //        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
            //        {
            //            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            //            request.downloadHandler = new DownloadHandlerBuffer();

            //            request.SetRequestHeader("Accept", "application/json");
            //            request.SetRequestHeader("Content-Type", "application/json;ty=23");
            //            request.SetRequestHeader("X-M2M-RI", "create_sub");
            //            request.SetRequestHeader("X-M2M-Origin", origin);
            //            request.SetRequestHeader("X-M2M-RVI", "3");

            //            yield return request.SendWebRequest();

            //            string res = request.downloadHandler.text;
            //            long statusCode = request.responseCode;

            //            if (request.result == UnityWebRequest.Result.Success)
            //            {
            //                Debug.Log($"[SUB OK] {targetUrl}: {statusCode} {res}");
            //            }
            //            else
            //            {
            //                // 실패 사유 출력
            //                Debug.LogError(
            //                    $"[SUB FAIL] {targetUrl}\n" +
            //                    $"- Status: {statusCode}\n" +
            //                    $"- Error: {request.error}\n" +
            //                    $"- Response: {res}"
            //                );
            //            }

            //            callback?.Invoke(res);
            //        }
            //    }

        }

        public abstract class OneM2MConnection
        {
            public bool IsConnected
            {
                get { return pulling != null; }
            }

            Coroutine pulling;
            IEnumerator PullingRoutine()
            {
                while (true)
                {
                    if (_instance.useAutoRefresh)
                    {
                        GetAllParameter();
                    }
                    yield return new WaitForSeconds(_instance.refreshInterval);
                }

            }
            abstract public void SetParameter(EFarmParameter type, string value);
            abstract public void GetParameter(EFarmParameter type);


            public void GetAllParameter()
            {
                foreach(EFarmParameter fp in Enum.GetValues(typeof(EFarmParameter)))
                {
                    GetParameter(fp);
                }
            }

            bool isConnecting = false;

            public async void TryMakeConnection()
            {
                if (isConnecting) return;

                isConnecting = true;

                try
                {
                    _instance.connectingEvent?.Invoke();

                    await ConnectSubFunction();
                    //연결 성공?
                    _instance.connectedEvent?.Invoke();
                    pulling = _instance.StartCoroutine(PullingRoutine());

                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                    _instance?.disconnectedEvent?.Invoke();
                }
                finally
                {
                    isConnecting = false;
                }

                
            }

            abstract protected Task ConnectSubFunction();

            async public void Disconnect()
            {
                if (IsConnected)
                {
                    _instance.disconnectedEvent?.Invoke();
                    _instance.StopCoroutine(pulling);
                    pulling = null;
                    await DisconnectSubFuction();
                    
                    
                }
                else
                {
                    //Debug.LogError("is Aleady Disconnected");
                    //_instance.Disconnectioned();
                }


            }
            abstract protected Task DisconnectSubFuction();

            virtual public void Update()
            {

            }
        }
    }



    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}