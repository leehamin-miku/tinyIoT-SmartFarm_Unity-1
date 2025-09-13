using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IoT;
using Newtonsoft.Json.Linq;

public class ActuatorDisplay : MonoBehaviour
{
    // ===== LED =====
    [Header("LED Control (Brightness Steps)")]
    public Slider LED_Slider;                 
    public TextMeshProUGUI LED_ValueText;

    [Header("Sun Light (Directional)")]
    public Light sun;                         
    public bool autoAssignSun = true;         

    // ===== FAN =====
    [Header("Fan Control")]
    public Toggle fanToggle;
    [Header("Fan Visual (Spinner)")]
    public FanSpinner[] fanSpinners;
    [Header("Fan Toggle Visual")]
    public Image fanBackground;
    public RectTransform fanHandle;
    public Color fanOnColor  = new Color32(65,192,83,255);
    public Color fanOffColor = new Color32(150,150,150,255);
    public float fanAnimTime = 0.15f;
    public Vector2 fanOffAnchoredPos = new Vector2(10f, -10f);
    public Vector2 fanOnAnchoredPos  = new Vector2(30f, -10f);

    // ===== WATER =====
    [Header("Watering Control")]
    public Toggle waterToggle;
    [Header("Water Toggle Visual")]
    public Image waterBackground;
    public RectTransform waterHandle;
    public Color waterOnColor  = new Color32(65,192,83,255);
    public Color waterOffColor = new Color32(150,150,150,255);
    public float waterAnimTime = 0.15f;
    public Vector2 waterOffAnchoredPos = new Vector2(10f, -10f);
    public Vector2 waterOnAnchoredPos  = new Vector2(30f, -10f);

    [Header("Water FX (optional)")]
    public WaterSprinklerPS waterFX; // 있으면 파티클 동기화

    // ===== Fetch 공통 =====
    [Header("Fetch Settings")]
    public bool fetchOnStart = true;
    public bool autoRefresh  = false;
    public float refreshInterval = 5f;

    bool isDragging = false;
    Vector2 fanOnPos, fanOffPos, waterOnPos, waterOffPos;
    Coroutine fanAnimCo, waterAnimCo, autoCo;

    void Awake()
    {
        if (autoAssignSun && !sun)
        {
            if (RenderSettings.sun) sun = RenderSettings.sun;
            if (!sun)
                foreach (var l in FindObjectsOfType<Light>())
                    if (l && l.type == LightType.Directional) { sun = l; break; }
        }
    }

    void Start()
    {
        // LED
        if (LED_Slider)
        {
            LED_Slider.wholeNumbers = true;
            LED_Slider.minValue = 0f; LED_Slider.maxValue = 10f;
            UpdateSliderText(LED_Slider.value);
            LED_Slider.onValueChanged.AddListener(OnSliderValueChanged);
            ApplySunIntensityStep(Mathf.RoundToInt(LED_Slider.value));
        }

        // Fan
        if (fanToggle)
        {
            fanToggle.onValueChanged.AddListener(OnFanToggleChanged);
            fanOffPos = fanOffAnchoredPos; fanOnPos = fanOnAnchoredPos;
            ApplyFanInstant(fanToggle.isOn);
        }

        // Water
        if (waterToggle)
        {
            waterToggle.onValueChanged.AddListener(OnWaterToggleChanged);
            waterOffPos = waterOffAnchoredPos; waterOnPos = waterOnAnchoredPos;
            ApplyWaterInstant(waterToggle.isOn);
        }

        if (fetchOnStart) StartCoroutine(FetchActuatorsOnce());
        if (autoRefresh)  autoCo = StartCoroutine(AutoRefreshLoop());
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

    // === 공용 헬퍼 추가 ===
    public static bool ParseOnOff(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return false;
        raw = raw.Trim();
        return raw.Equals("ON", System.StringComparison.OrdinalIgnoreCase)
            || raw.Equals("1")
            || raw.Equals("true", System.StringComparison.OrdinalIgnoreCase);
    }
    static string ToOnOff(bool isOn) => isOn ? "ON" : "OFF";

    // ===== LED =====
    void OnSliderValueChanged(float value)
    {
        int step = Mathf.RoundToInt(value);
        UpdateSliderText(step);
        ApplySunIntensityStep(step);
        isDragging = true;
    }
    void UpdateSliderText(float v) => LED_ValueText.text = Mathf.RoundToInt(v).ToString();
    void ApplySunIntensityStep(int step)
    {
        if (!sun) return;
        sun.intensity = Mathf.Clamp(step, 0, 10) * 0.1f;
    }
    IEnumerator SendLEDValueToServer(int ledValue)
    {
        string jsonBody = new JObject { ["m2m:cin"] = new JObject { ["con"] = ledValue.ToString() } }.ToString();

        // 원래 형식 (named args)
        yield return StartCoroutine(OneM2M.PostDataCoroutine(
            origin: "CAdmin",
            type: 4,
            body: jsonBody,
            url: "TinyFarm/Actuators/LED"
        ));
    }

    // ===== FAN =====
    void OnFanToggleChanged(bool isOn)
    {
        StartFanAnimate(isOn);
        StartCoroutine(SendFanStateToServer(isOn));
        if (fanSpinners != null)
            foreach (var sp in fanSpinners) if (sp) sp.SetOn(isOn);
    }
    IEnumerator SendFanStateToServer(bool isOn)
    {
        string jsonBody = new JObject {
            ["m2m:cin"] = new JObject { ["con"] = ToOnOff(isOn) }
        }.ToString();

        yield return StartCoroutine(OneM2M.PostDataCoroutine(
            origin: "CAdmin",
            type: 4,
            body: jsonBody,
            url: "TinyFarm/Actuators/Fan"
        ));
    }
    void ApplyFanInstant(bool isOn)
    {
        if (!fanBackground || !fanHandle) return;
        fanBackground.color = isOn ? fanOnColor : fanOffColor;
        fanHandle.anchoredPosition = isOn ? fanOnPos : fanOffPos;
    }
    void StartFanAnimate(bool isOn)
    {
        if (fanAnimCo != null) StopCoroutine(fanAnimCo);
        fanAnimCo = StartCoroutine(DoToggleAnimate(
            fanBackground, fanHandle, isOn, fanOnColor, fanOffColor, fanOnPos, fanOffPos, fanAnimTime
        ));
    }

    // ===== WATER =====
    void OnWaterToggleChanged(bool isOn)
    {
        StartWaterAnimate(isOn);
        if (waterFX) waterFX.SetState(isOn); // 파티클
        StartCoroutine(SendWaterStateToServer(isOn));
    }
    IEnumerator SendWaterStateToServer(bool isOn)
    {
        string jsonBody = new JObject {
            ["m2m:cin"] = new JObject { ["con"] = ToOnOff(isOn) }
        }.ToString();

        yield return StartCoroutine(OneM2M.PostDataCoroutine(
            origin: "CAdmin",
            type: 4,
            body: jsonBody,
            url: "TinyFarm/Actuators/Water"
        ));
    }
    void ApplyWaterInstant(bool isOn)
    {
        if (!waterBackground || !waterHandle) return;
        waterBackground.color = isOn ? waterOnColor : waterOffColor;
        waterHandle.anchoredPosition = isOn ? waterOnPos : waterOffPos;
    }
    void StartWaterAnimate(bool isOn)
    {
        if (waterAnimCo != null) StopCoroutine(waterAnimCo);
        waterAnimCo = StartCoroutine(DoToggleAnimate(
            waterBackground, waterHandle, isOn, waterOnColor, waterOffColor, waterOnPos, waterOffPos, waterAnimTime
        ));
    }

    // ===== 공통 토글 애니메이션 =====
    IEnumerator DoToggleAnimate(Image bg, RectTransform knob, bool isOn,
                                Color onColor, Color offColor,
                                Vector2 onPos, Vector2 offPos, float secs)
    {
        if (!bg || !knob) yield break;
        float t = 0f;
        Color c0 = bg.color, c1 = isOn ? onColor : offColor;
        Vector2 p0 = knob.anchoredPosition, p1 = isOn ? onPos : offPos;
        while (t < secs)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / secs);
            bg.color = Color.Lerp(c0, c1, u);
            knob.anchoredPosition = Vector2.Lerp(p0, p1, u);
            yield return null;
        }
        bg.color = c1; knob.anchoredPosition = p1;
    }

    void OnRectTransformDimensionsChange()
    {
        if (fanToggle)   ApplyFanInstant(fanToggle.isOn);
        if (waterToggle) ApplyWaterInstant(waterToggle.isOn);
    }

    // ===== FETCH & AUTO REFRESH =====
    IEnumerator FetchActuatorsOnce()
    {
        yield return FetchLEDOnce();
        yield return FetchFanOnce();
        yield return FetchWaterOnce();
    }
    IEnumerator AutoRefreshLoop()
    {
        while (autoRefresh)
        {
            if (!isDragging) yield return FetchLEDOnce();
            yield return FetchFanOnce();
            yield return FetchWaterOnce();
            yield return new WaitForSeconds(refreshInterval);
        }
        autoCo = null;
    }

    IEnumerator FetchLEDOnce()
    {
        yield return StartCoroutine(OneM2M.GetDataCoroutine(
            origin: "CAdmin",
            url: "TinyFarm/Actuators/LED/la",
            callback: (res) =>
            {
                try {
                    var json = JObject.Parse(res);
                    string raw = json["m2m:cin"]?["con"]?.ToString();
                    if (!string.IsNullOrEmpty(raw))
                    {
                        int step = Mathf.Clamp(int.Parse(raw), 0, 10);
                        LED_Slider.SetValueWithoutNotify(step);
                        UpdateSliderText(step);
                        ApplySunIntensityStep(step);
                    }
                } catch { Debug.LogWarning("FetchLEDOnce parse failed"); }
            }
        ));
    }

    IEnumerator FetchFanOnce()
    {
        yield return StartCoroutine(OneM2M.GetDataCoroutine(
            origin: "CAdmin",
            url: "TinyFarm/Actuators/Fan/la",
            callback: (res) =>
            {
                try {
                    var json = JObject.Parse(res);
                    string raw = json["m2m:cin"]?["con"]?.ToString();
                    bool isOn = ParseOnOff(raw);
                    fanToggle.SetIsOnWithoutNotify(isOn);
                    ApplyFanInstant(isOn);
                } catch { Debug.LogWarning("FetchFanOnce parse failed"); }
            }
        ));
    }

    IEnumerator FetchWaterOnce()
    {
        yield return StartCoroutine(OneM2M.GetDataCoroutine(
            origin: "CAdmin",
            url: "TinyFarm/Actuators/Water/la",
            callback: (res) =>
            {
                try {
                    var json = JObject.Parse(res);
                    string raw = json["m2m:cin"]?["con"]?.ToString();
                    bool isOn = ParseOnOff(raw);
                    if (waterToggle)
                    {
                        waterToggle.SetIsOnWithoutNotify(isOn);
                        ApplyWaterInstant(isOn);
                    }
                    if (waterFX) waterFX.SetState(isOn);
                } catch { Debug.LogWarning("FetchWaterOnce parse failed"); }
            }
        ));
    }

    // 런타임에 Fetch 설정 변경 (Config 패널에서 호출)
    public void ApplyFetchSettings(bool newFetchOnStart, bool newAutoRefresh, float newInterval)
    {
        fetchOnStart    = newFetchOnStart;
        refreshInterval = Mathf.Max(0.1f, newInterval);

        if (autoRefresh != newAutoRefresh)
        {
            autoRefresh = newAutoRefresh;

            // 기존 루프 정지/재시작
            if (autoCo != null) { StopCoroutine(autoCo); autoCo = null; }
            if (autoRefresh) autoCo = StartCoroutine(AutoRefreshLoop());
        }
    }
}
