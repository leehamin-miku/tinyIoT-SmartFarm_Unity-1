using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;
using IoT;
using System.Diagnostics; // Stopwatch
using Debug = UnityEngine.Debug;
using UnityEngine.UI;     // ← 버튼 연결용

public class SensorDisplay : MonoBehaviour
{
    public TextMeshProUGUI tempText;
    public TextMeshProUGUI humidText;
    public TextMeshProUGUI CO2Text;
    public TextMeshProUGUI SoilText;

    [Header("Manual Retrieve (optional)")]
    public Button retrieveButton;      // 버튼(선택)
    public TextMeshProUGUI retrieveBtnText;   // 버튼 라벨(TextMeshPro, 선택)

    bool _fetching = false;            // 중복 호출 방지

    void Start()
    {
        OneM2M.AddValueRefreshEvent(MirrorAll);
        MirrorAll();
        retrieveButton.onClick.AddListener(OneM2M.FarmParameterUpToDate);

    }

    // === [버튼] 즉시 재조회 ===
    public void OnClickRetrieve()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_fetching) return;                 // 연타 방지
        StartCoroutine(ManualRetrieve());
    }

    IEnumerator ManualRetrieve()
    {
        ToggleRetrieveUI(true);
        OneM2M.FarmParameterUpToDate();
        yield return new WaitForSeconds(0.3f);
        ToggleRetrieveUI(false);
    }

    void ToggleRetrieveUI(bool busy)
    {
        if (retrieveButton) retrieveButton.interactable = !busy;
        if (retrieveBtnText) retrieveBtnText.text = busy ? "Refreshing..." : "Retrieve";
    }

    // === 외부에서 즉시 1회 갱신하고 싶을 때 호출 (기존) ===
    //public void RefreshNow()
    //{
    //    if (!_fetching) StartCoroutine(FetchSensorsOnce());
    //}

    // === 액추에이터와 동일 스타일: 런타임에 설정 적용 ===
    //public void ApplyFetchSettings(bool newFetchOnStart, bool newAutoRefresh, float newInterval)
    //{
    //    fetchOnStart    = newFetchOnStart;
    //    refreshInterval = Mathf.Max(0.1f, newInterval);

    //    if (autoRefresh != newAutoRefresh)
    //    {
    //        autoRefresh = newAutoRefresh;
    //        if (autoCo != null) { StopCoroutine(autoCo); autoCo = null; }
    //        if (autoRefresh) autoCo = StartCoroutine(AutoRefreshLoop());
    //    }
    //    // 주기만 바뀐 경우: 다음 사이클부터 자연 반영
    //}

    //IEnumerator AutoRefreshLoop()
    //{
    //    while (autoRefresh)
    //    {
    //        yield return StartCoroutine(FetchSensorsOnce());
    //        yield return new WaitForSeconds(refreshInterval);
    //    }
    //    autoCo = null;
    //}

    // === 4종 센서를 한 번에 갱신 ===
    //IEnumerator FetchSensorsOnce()
    //{
    //    if (_fetching) yield break;
    //    _fetching = true;
    //    try
    //    {
    //        // Temperature
    //        yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/Temperature/la", (raw) =>
    //        {
    //            try {
    //                var json = JObject.Parse(raw);
    //                float v = float.Parse(json["m2m:cin"]["con"].ToString());
    //                tempText.text = $"Temperature: {v:0.0} °C";
    //            } catch { tempText.text = "Temperature: -- °C"; }
    //        }));

    //        // Humidity
    //        yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/Humidity/la", (raw) =>
    //        {
    //            try {
    //                var json = JObject.Parse(raw);
    //                float v = float.Parse(json["m2m:cin"]["con"].ToString());
    //                humidText.text = $"Humidity: {v:0.0} %";
    //            } catch { humidText.text = "Humidity: -- %"; }
    //        }));

    //        // CO2
    //        yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/CO2/la", (raw) =>
    //        {
    //            try {
    //                var json = JObject.Parse(raw);
    //                float v = float.Parse(json["m2m:cin"]["con"].ToString());
    //                CO2Text.text = $"CO2: {v:0.0} %";
    //            } catch { CO2Text.text = "CO2: -- %"; }
    //        }));

    //        // Soil Moisture
    //        yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/Soil/la", (raw) =>
    //        {
    //            try {
    //                var json = JObject.Parse(raw);
    //                float v = float.Parse(json["m2m:cin"]["con"].ToString());
    //                SoilText.text = $"Soil Moisture: {v:0.0} %";
    //            } catch { SoilText.text = "Soil Moisture: -- %"; }
    //        }));
    //    }
    //    finally
    //    {
    //        _fetching = false;
    //    }
    //}

    void MirrorAll()
    {

        void SubFuction(TextMeshProUGUI textMesh, OneM2M.EFarmParameter type, string suffix, string name = "",string defaultValue ="--")
        {
            try
            {
                float v = float.Parse(OneM2M.GetFarmParameter(type));
                string a = name == "" ? type.ToString() : name;
                textMesh.text = $"{a}: {v:0.0} {suffix}";
            }
            catch { textMesh.text = $"{type.ToString()}: {defaultValue} {suffix}"; }
        }
        SubFuction(tempText, OneM2M.EFarmParameter.Temperature, "°C");
        SubFuction(CO2Text, OneM2M.EFarmParameter.CO2, "%");
        SubFuction(SoilText, OneM2M.EFarmParameter.Soil, "%", "Soil Moisture");
        SubFuction(humidText, OneM2M.EFarmParameter.Humidity, "%");
    } 

    //IEnumerator GetDataWithTiming(string url, System.Action<string> onSuccess)
    //{
    //    Stopwatch sw = Stopwatch.StartNew();

    //    yield return StartCoroutine(OneM2M.GetDataCoroutine(
    //        origin: "CAdmin",
    //        url: url,
    //        callback: (result) =>
    //        {
    //            sw.Stop();
    //            Debug.Log($"[Timing] {url} 응답: {sw.ElapsedMilliseconds} ms");
    //            onSuccess?.Invoke(result);
    //        }
    //    ));
    //}
}
