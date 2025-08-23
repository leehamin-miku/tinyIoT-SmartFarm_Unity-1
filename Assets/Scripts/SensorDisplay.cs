using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;
using System.Collections;
using IoT;
using System.Diagnostics; // Stopwatch
using Debug = UnityEngine.Debug;

public class SensorDisplay : MonoBehaviour
{
    public TMP_Text tempText;
    public TMP_Text humidText;
    public TMP_Text CO2Text;
    public TMP_Text SoilText;

    [Header("Fetch Settings")]
    public bool fetchOnStart = true;   // 시작 시 1회 가져오기
    public bool autoRefresh  = true;   // 주기 갱신 켜기/끄기
    public float refreshInterval = 5f; // 초

    Coroutine autoCo;

    void Start()
    {
        if (fetchOnStart) StartCoroutine(FetchSensorsOnce());
        if (autoRefresh)  autoCo = StartCoroutine(AutoRefreshLoop());
    }

    // === 외부에서 즉시 1회 갱신하고 싶을 때 호출 ===
    public void RefreshNow()
    {
        StartCoroutine(FetchSensorsOnce());
    }

    // === 액추에이터와 동일 스타일: 런타임에 설정 적용 ===
    public void ApplyFetchSettings(bool newFetchOnStart, bool newAutoRefresh, float newInterval)
    {
        fetchOnStart    = newFetchOnStart;
        refreshInterval = Mathf.Max(0.1f, newInterval);

        if (autoRefresh != newAutoRefresh)
        {
            autoRefresh = newAutoRefresh;
            if (autoCo != null) { StopCoroutine(autoCo); autoCo = null; }
            if (autoRefresh) autoCo = StartCoroutine(AutoRefreshLoop());
        }
        // 주기만 바뀐 경우: 다음 사이클부터 자연 반영
    }

    IEnumerator AutoRefreshLoop()
    {
        while (autoRefresh)
        {
            yield return StartCoroutine(FetchSensorsOnce());
            yield return new WaitForSeconds(refreshInterval);
        }
        autoCo = null;
    }

    // === 4종 센서를 한 번에 갱신 ===
    IEnumerator FetchSensorsOnce()
    {
        // Temperature
        yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/Temperature/la", (raw) =>
        {
            try {
                var json = JObject.Parse(raw);
                float v = float.Parse(json["m2m:cin"]["con"].ToString());
                tempText.text = $"Temperature: {v:0.0} °C";
            } catch { tempText.text = "Temperature: -- °C"; }
        }));

        // Humidity
        yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/Humid/la", (raw) =>
        {
            try {
                var json = JObject.Parse(raw);
                float v = float.Parse(json["m2m:cin"]["con"].ToString());
                humidText.text = $"Humidity: {v:0.0} %";
            } catch { humidText.text = "Humidity: -- %"; }
        }));

        // CO2
        yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/CO2/la", (raw) =>
        {
            try {
                var json = JObject.Parse(raw);
                float v = float.Parse(json["m2m:cin"]["con"].ToString());
                CO2Text.text = $"CO2: {v:0.0} %";
            } catch { CO2Text.text = "CO2: -- %"; }
        }));

        // Soil Moisture
        yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/Soil/la", (raw) =>
        {
            try {
                var json = JObject.Parse(raw);
                float v = float.Parse(json["m2m:cin"]["con"].ToString());
                SoilText.text = $"Soil Moisture: {v:0.0} %";
            } catch { SoilText.text = "Soil Moisture: -- %"; }
        }));
    }

    // 요청~응답 시간 측정 공통 루틴
    IEnumerator GetDataWithTiming(string url, System.Action<string> onSuccess)
    {
        Stopwatch sw = Stopwatch.StartNew();

        yield return StartCoroutine(OneM2M.GetDataCoroutine(
            origin: "CAdmin",
            url: url,
            callback: (result) =>
            {
                sw.Stop();
                Debug.Log($"[Timing] {url} 응답: {sw.ElapsedMilliseconds} ms");
                onSuccess?.Invoke(result);
            }
        ));
    }
}
