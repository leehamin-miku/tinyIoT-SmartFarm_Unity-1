using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IoT; // OneM2M 네임스페이스 사용
using Newtonsoft.Json.Linq;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public GameObject Sensors_panel;
    public GameObject Actuators_panel;
    public GameObject Config_panel;
    public GameObject Log_panel;

    public Slider LED_Slider;
    public TextMeshProUGUI LED_ValueText;

    private bool isDragging = false;

    void Start()
    {
        // Value init
        UpdateSliderText(LED_Slider.value);

        // if Value Change Text Update
        LED_Slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    void Update()
    {
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            int ledValue = Mathf.RoundToInt(LED_Slider.value);
            StartCoroutine(SendLEDValueToServer(ledValue));
        }
    }

    void OnSliderValueChanged(float value)
    {
        UpdateSliderText(value);
        isDragging = true;
    }

    void UpdateSliderText(float value)
    {
        LED_ValueText.text = Mathf.RoundToInt(value) + "";
    }

    IEnumerator SendLEDValueToServer(int ledValue)
    {
        string jsonBody = new JObject
        {
            ["m2m:cin"] = new JObject
            {
                ["con"] = ledValue.ToString()
            }
        }.ToString();

        yield return StartCoroutine(OneM2M.PostDataCoroutine(
            origin: "CAdmin",
            type: 4,
            body: jsonBody,
            url: "TinyFarm/Actuator/LED"
        ));
    }

    public void ShowPanel(string panelName)
    {
        Sensors_panel.SetActive(panelName == "Sensors");
        Actuators_panel.SetActive(panelName == "Actuators");
        Config_panel.SetActive(panelName == "Config");
        Log_panel.SetActive(panelName == "Log");
    }
}
