using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;
using System.Collections;
using IoT;
using System.Diagnostics; // Stopwatch 사용
using Debug = UnityEngine.Debug;

public class SensorDisplay : MonoBehaviour
{
    public TMP_Text tempText;
    public TMP_Text humidText;
    public TMP_Text CO2Text;
    public TMP_Text SoilText;
    
    public float refreshInterval = 5f;

    private void Start()
    {
        StartCoroutine(UpdateSensorDataLoop());
    }

    private IEnumerator UpdateSensorDataLoop()
    {
        while (true)
        {
            // Temperature
            yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/Temperature/la", (raw) =>
            {
                try
                {
                    JObject json = JObject.Parse(raw);
                    float temp = float.Parse(json["m2m:cin"]["con"].ToString());
                    tempText.text = $"Temperature: {temp:0.0} °C";
                }
                catch
                {
                    tempText.text = "Temperature: -- °C";
                }
            }));

            // Humidity
            yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/Humid/la", (raw) =>
            {
                try
                {
                    JObject json = JObject.Parse(raw);
                    float humid = float.Parse(json["m2m:cin"]["con"].ToString());
                    humidText.text = $"Humidity: {humid:0.0} %";
                }
                catch
                {
                    humidText.text = "Humidity: -- %";
                }
            }));

            // CO2
            yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/CO2/la", (raw) =>
            {
                try
                {
                    JObject json = JObject.Parse(raw);
                    float CO2 = float.Parse(json["m2m:cin"]["con"].ToString());
                    CO2Text.text = $"CO2: {CO2:0.0} %";
                }
                catch
                {
                    CO2Text.text = "CO2: -- %";
                }
            }));

            // Soil Moisture
            yield return StartCoroutine(GetDataWithTiming("TinyFarm/Sensors/Soil/la", (raw) =>
            {
                try
                {
                    JObject json = JObject.Parse(raw);
                    float Soil = float.Parse(json["m2m:cin"]["con"].ToString());
                    SoilText.text = $"Soil Moisture: {Soil:0.0} %";
                }
                catch
                {
                    SoilText.text = "Soil Moisture: -- %";
                }
            }));

            yield return new WaitForSeconds(refreshInterval);
        }
    }

    // 요청 ~ 응답 시간 측정 포함한 공용 메서드
    private IEnumerator GetDataWithTiming(string url, System.Action<string> onSuccess)
    {
        Stopwatch sw = Stopwatch.StartNew();

        yield return StartCoroutine(OneM2M.GetDataCoroutine("CAdmin", "", url, (result) =>
        {
            sw.Stop();
            Debug.Log($"[Timing] {url} 응답 시간: {sw.ElapsedMilliseconds} ms");
            onSuccess?.Invoke(result);
        }));
    }
}
