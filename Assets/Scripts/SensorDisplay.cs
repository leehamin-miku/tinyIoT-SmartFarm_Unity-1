using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;
using System.Collections;
using IoT;

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
            yield return StartCoroutine(OneM2M.GetDataCoroutine("CAdmin", "", "TinyFarm/Sensors/Temperature/la", (result) =>
            {
                try
                {
                    JObject json = JObject.Parse(result);
                    string raw = json["m2m:cin"]["con"].ToString();
                    float temp = float.Parse(raw);
                    tempText.text = $"Temperature: {temp:0.0} °C";
                }
                catch
                {
                    tempText.text = "Temperature: -- °C";
                }
            }));

            // Humidity
            yield return StartCoroutine(OneM2M.GetDataCoroutine("CAdmin", "", "TinyFarm/Sensors/Humid/la", (result) =>
            {
                try
                {
                    JObject json = JObject.Parse(result);
                    string raw = json["m2m:cin"]["con"].ToString();
                    float humid = float.Parse(raw);
                    humidText.text = $"Humidity: {humid:0.0} %";
                }
                catch
                {
                    humidText.text = "Humidity: -- %";
                }
            }));

            // CO2
            yield return StartCoroutine(OneM2M.GetDataCoroutine("CAdmin", "", "TinyFarm/Sensors/CO2/la", (result) =>
            {
                try
                {
                    JObject json = JObject.Parse(result);
                    string raw = json["m2m:cin"]["con"].ToString();
                    float CO2 = float.Parse(raw);
                    CO2Text.text = $"CO2: {CO2:0.0} %";
                }
                catch
                {
                    CO2Text.text = "CO2: -- %";
                }
            }));

            // Soil Moisture
            yield return StartCoroutine(OneM2M.GetDataCoroutine("CAdmin", "", "TinyFarm/Sensors/Soil/la", (result) =>
            {
                try
                {
                    JObject json = JObject.Parse(result);
                    string raw = json["m2m:cin"]["con"].ToString();
                    float Soil = float.Parse(raw);
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
}
