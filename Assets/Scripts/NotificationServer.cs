using UnityEngine;
using System;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using IoT;

public class NotificationServer : MonoBehaviour
{
    private HttpListener listener;
    private Thread listenerThread;
    public volatile bool isRunning = false;
    public static int port = 6000;

    public string notiUriOverride;

    static readonly ConcurrentQueue<Action> mainThreadJobs = new ConcurrentQueue<Action>();

    public static void EnqueueMain(Action job) => mainThreadJobs.Enqueue(job);

    void Awake()   { Application.quitting += () => StopServer(); }
    void OnEnable(){ if (!isRunning) StartServer(); }
    void OnDisable(){ StopServer(); }
    void OnDestroy(){ StopServer(); }

    private void Start()
    {
        string notiUri = GetNotiUri();
        Debug.Log($"[SUB] notiUri = {notiUri}");
        StartCoroutine(OneM2M.CreateSubscription("CAdmin","TinyFarm/Actuators/LED", "LEDSub", notiUri));
        StartCoroutine(OneM2M.CreateSubscription("CAdmin","TinyFarm/Actuators/Fan", "FanSub", notiUri));
        StartCoroutine(OneM2M.CreateSubscription("CAdmin","TinyFarm/Actuators/Water", "WaterSub", notiUri));
    }

    void Update()
    {
        while (mainThreadJobs.TryDequeue(out var job))
        {
            try { job(); } catch (Exception e) { Debug.LogError(e); }
        }
    }

    private string GetNotiUri()
    {
        if (!string.IsNullOrEmpty(notiUriOverride))
            return notiUriOverride.TrimEnd('/') + "/notifi";
        return $"http://{GetLocalIP()}:{port}/notifi";
    }

    public void StartServer()
    {
        if (isRunning) return;
        isRunning = true;

        listener = new HttpListener();
        listener.Prefixes.Add($"http://+:{port}/");
        listener.Start();

        listenerThread = new Thread(ListenerThread) { IsBackground = true };
        listenerThread.Start();
    }

    private void ListenerThread()
    {
        while (isRunning)
        {
            try
            {
                var context = listener.GetContext();                // blocking
                ThreadPool.QueueUserWorkItem(ProcessRequest, context);
            }
            catch (Exception e)
            {
                if (isRunning) Debug.LogError($"Listener error: {e.Message}");
            }
        }
    }

    private void ProcessRequest(object state)
    {
        var context = (HttpListenerContext)state;
        try
        {
            if (context.Request.HttpMethod=="POST" && context.Request.Url.AbsolutePath.TrimEnd('/')=="/notifi")
            {
                var enc = context.Request.ContentEncoding ?? Encoding.UTF8;
                using var r = new System.IO.StreamReader(context.Request.InputStream, enc);
                var body = r.ReadToEnd();

                JObject jo = null;
                try { jo = JsonConvert.DeserializeObject<JObject>(body); } catch {}

                bool vrq = jo?["m2m:sgn"]?["vrq"]?.Value<bool>() ?? false;

                // 응답
                context.Response.Headers["X-M2M-RSC"]="2000";
                context.Response.Headers["X-M2M-RI"]= context.Request.Headers["X-M2M-RI"] ?? Guid.NewGuid().ToString();
                context.Response.Headers["X-M2M-Origin"]="mn-ae";
                context.Response.Headers["X-M2M-RVI"]="3";
                context.Response.StatusCode=200;
                context.Response.ContentLength64=0;
                context.Response.Close();

                if (!vrq)
                {
                    string con = jo?.SelectToken("m2m:sgn.nev.rep.m2m:cin.con")?.ToString();
                    string sur = jo?["m2m:sgn.sur"]?.ToString();

                    EnqueueMain(() =>
                    {
                        var ui = FindObjectOfType<ActuatorDisplay>();
                        if (ui == null) return;

                        if (sur.Contains("/LED") && int.TryParse(con, out int step))
                        {
                            ui.LED_Slider.SetValueWithoutNotify(Mathf.Clamp(step,0,10));
                            ui.SendMessage("ApplySunIntensityStep", step, SendMessageOptions.DontRequireReceiver);
                        }
                        else if (sur.Contains("/Fan"))
                        {
                            bool on = ActuatorDisplay.ParseOnOff(con);
                            ui.fanToggle.SetIsOnWithoutNotify(on);
                            ui.SendMessage("ApplyFanInstant", on, SendMessageOptions.DontRequireReceiver);
                        }
                        else if (sur.Contains("/Water"))
                        {
                            bool on = ActuatorDisplay.ParseOnOff(con);
                            ui.waterToggle.SetIsOnWithoutNotify(on);
                            ui.SendMessage("ApplyWaterInstant", on, SendMessageOptions.DontRequireReceiver);
                            if (ui.waterFX) ui.waterFX.SetState(on);
                        }
                    });
                }

                return;
            }

            context.Response.StatusCode = 404;
            context.Response.ContentLength64 = 0;
            context.Response.Close();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            try { context.Response.StatusCode = 500; context.Response.ContentLength64 = 0; context.Response.Close(); } catch {}
        }
    }

    public void StopServer()
    {
        if (!isRunning) return;
        isRunning = false;

        try { listener?.Stop(); }  catch {}
        try { listener?.Close(); } catch {}

        if (listenerThread != null && listenerThread.IsAlive)
        {
            try { listenerThread.Join(1000); } catch {}
            if (listenerThread.IsAlive) { try { listenerThread.Interrupt(); } catch {} }
        }
        listenerThread = null;
        listener = null;
    }

    private string GetLocalIP()
    {
        string localIP = "127.0.0.1";
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    { localIP = ip.ToString(); break; }
        } catch {}
        return localIP;
    }
}
