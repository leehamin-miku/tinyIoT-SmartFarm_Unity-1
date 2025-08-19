using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IoT;
using Newtonsoft.Json.Linq;
using System.Collections;

public class ActuatorDisplay : MonoBehaviour
{
    [Header("LED Control (Brightness Steps)")]
    public Slider LED_Slider;                 // min=0, max=10, wholeNumbers=true 권장
    public TextMeshProUGUI LED_ValueText;

    [Header("Sun Light (Directional)")]
    public Light sun;                         // 씬의 Directional Light
    public bool autoAssignSun = true;         // 자동 할당 사용

    [Header("Fan Control")]
    public Toggle fanToggle;

    [Header("Fan Toggle Visual")]
    public Image fanBackground;
    public RectTransform fanHandle;
    public Color fanOnColor  = new Color32(65,192,83,255);
    public Color fanOffColor = new Color32(150,150,150,255);
    public float fanPadding = 2f;
    public float fanAnimTime = 0.15f;

    [Header("Fan Handle Fixed Positions")]
    public Vector2 fanOffAnchoredPos = new Vector2(10f, -10f);
    public Vector2 fanOnAnchoredPos  = new Vector2(30f, -10f);

    [Header("Fetch Settings")]
    public bool fetchOnStart = true;
    public bool autoRefresh  = false;
    public float refreshInterval = 5f;

    private bool isDragging = false;

    // Fan 토글 애니메이션용 내부 상태
    Vector2 fanOnPos, fanOffPos;
    Coroutine fanAnimCo;
    Coroutine autoCo;

    void Awake()
    {
        // Directional Light 자동 할당
        if (autoAssignSun && !sun)
        {
            if (RenderSettings.sun) sun = RenderSettings.sun;
            if (!sun)
            {
                var lights = FindObjectsOfType<Light>();
                foreach (var l in lights)
                {
                    if (l && l.type == LightType.Directional) { sun = l; break; }
                }
            }
        }
    }

    public void TryAutoAssignSun()
    {
        if (autoAssignSun && !sun)
        {
            if (RenderSettings.sun) sun = RenderSettings.sun;
            if (!sun)
            {
                var lights = FindObjectsOfType<Light>();
                foreach (var l in lights)
                    if (l && l.type == LightType.Directional) { sun = l; break; }
            }
        }
    }

    void Start()
    {
        // LED 슬라이더 기본 설정(권장값)
        if (LED_Slider)
        {
            LED_Slider.wholeNumbers = true;
            if (LED_Slider.minValue != 0f)  LED_Slider.minValue = 0f;
            if (LED_Slider.maxValue != 10f) LED_Slider.maxValue = 10f;

            UpdateSliderText(LED_Slider.value);
            LED_Slider.onValueChanged.AddListener(OnSliderValueChanged);

            // 시작 시 슬라이더 값으로 태양 밝기 반영
            ApplySunIntensityStep(Mathf.RoundToInt(LED_Slider.value));
        }

        // Fan 초기화
        if (fanToggle != null)
        {
            fanToggle.onValueChanged.AddListener(OnFanToggleChanged);
            CacheFanPositions();
            ApplyFanInstant(fanToggle.isOn);
        }

        if (fetchOnStart) StartCoroutine(FetchActuatorsOnce());
        if (autoRefresh) autoCo = StartCoroutine(AutoRefreshLoop());
    }

    void Update()
    {
        // LED 값 서버 전송 (드래그 종료 시 1회)
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            int ledValue = Mathf.RoundToInt(LED_Slider.value);
            StartCoroutine(SendLEDValueToServer(ledValue));
        }
    }

    // ===== LED → Sun Intensity (0~10 step → 0.0~1.0) =====
    void OnSliderValueChanged(float value)
    {
        int step = Mathf.RoundToInt(value);
        UpdateSliderText(step);
        ApplySunIntensityStep(step);
        isDragging = true;
    }

    void UpdateSliderText(float valueOrStep)
    {
        LED_ValueText.text = Mathf.RoundToInt(valueOrStep).ToString();
    }

    void ApplySunIntensityStep(int step)
    {
        if (!sun) return;
        step = Mathf.Clamp(step, 0, 10);
        sun.intensity = step * 0.1f; // 0, 0.1, ..., 1.0
    }

    IEnumerator SendLEDValueToServer(int ledValue)
    {
        string jsonBody = new JObject
        {
            ["m2m:cin"] = new JObject
            {
                ["con"] = ledValue.ToString()   // 0~10 단계값 서버로 전송
            }
        }.ToString();

        yield return StartCoroutine(OneM2M.PostDataCoroutine(
            origin: "CAdmin",
            type: 4,
            body: jsonBody,
            url: "TinyFarm/Actuator/LED"
        ));
    }

    // ===== Fan =====
    void OnFanToggleChanged(bool isOn)
    {
        StartFanAnimate(isOn);
        StartCoroutine(SendFanStateToServer(isOn));
    }

    IEnumerator SendFanStateToServer(bool isOn)
    {
        string jsonBody = new JObject
        {
            ["m2m:cin"] = new JObject
            {
                ["con"] = isOn ? "1" : "0"
            }
        }.ToString();

        yield return StartCoroutine(OneM2M.PostDataCoroutine(
            origin: "CAdmin",
            type: 4,
            body: jsonBody,
            url: "TinyFarm/Actuator/Fan"
        ));
    }

    // ===== Fan 토글 비주얼 로직 =====
    void CacheFanPositions()
    {
        if (fanBackground == null || fanHandle == null) return;
        fanOffPos = fanOffAnchoredPos;
        fanOnPos  = fanOnAnchoredPos;
    }

    void ApplyFanInstant(bool isOn)
    {
        if (fanBackground == null || fanHandle == null) return;
        fanBackground.color = isOn ? fanOnColor : fanOffColor;
        fanHandle.anchoredPosition = isOn ? fanOnPos : fanOffPos;
    }

    void StartFanAnimate(bool isOn)
    {
        if (fanAnimCo != null) StopCoroutine(fanAnimCo);
        fanAnimCo = StartCoroutine(FanAnimate(isOn));
    }

    IEnumerator FanAnimate(bool isOn)
    {
        if (fanBackground == null || fanHandle == null) yield break;

        float t = 0f;
        Color c0 = fanBackground.color, c1 = isOn ? fanOnColor : fanOffColor;
        Vector2 p0 = fanHandle.anchoredPosition, p1 = isOn ? fanOnPos : fanOffPos;

        while (t < fanAnimTime)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / fanAnimTime);
            fanBackground.color = Color.Lerp(c0, c1, u);
            fanHandle.anchoredPosition = Vector2.Lerp(p0, p1, u);
            yield return null;
        }
        ApplyFanInstant(isOn);
    }

    void OnRectTransformDimensionsChange()
    {
        CacheFanPositions();
        if (fanToggle != null) ApplyFanInstant(fanToggle.isOn);
    }

    // ====== GET으로 최신값 받아와서 UI에 반영 ======
    IEnumerator FetchActuatorsOnce()
    {
        yield return StartCoroutine(FetchLEDOnce());
        yield return StartCoroutine(FetchFanOnce());
    }

    IEnumerator AutoRefreshLoop()
    {
        while (autoRefresh)
        {
            if (!isDragging) yield return StartCoroutine(FetchLEDOnce());
            yield return StartCoroutine(FetchFanOnce());
            yield return new WaitForSeconds(refreshInterval);
        }
        autoCo = null;
    }

    IEnumerator FetchLEDOnce()
    {
        yield return StartCoroutine(OneM2M.GetDataCoroutine(
            origin: "CAdmin",
            url: "TinyFarm/Actuator/LED/la",
            callback: (res) =>
            {
                try
                {
                    var json = JObject.Parse(res);
                    string raw = json["m2m:cin"]?["con"]?.ToString();
                    if (!string.IsNullOrEmpty(raw))
                    {
                        int step = Mathf.Clamp(int.Parse(raw), 0, 10);
                        LED_Slider.SetValueWithoutNotify(step); // 이벤트 막고 UI만
                        UpdateSliderText(step);
                        ApplySunIntensityStep(step);
                    }
                }
                catch { Debug.LogWarning("FetchLEDOnce parse failed"); }
            }
        ));
    }

    IEnumerator FetchFanOnce()
    {
        yield return StartCoroutine(OneM2M.GetDataCoroutine(
            origin: "CAdmin",
            url: "TinyFarm/Actuator/Fan/la",
            callback: (res) =>
            {
                try
                {
                    var json = JObject.Parse(res);
                    string raw = json["m2m:cin"]?["con"]?.ToString();
                    if (!string.IsNullOrEmpty(raw))
                    {
                        bool isOn = raw == "1" || raw.ToLower() == "true";
                        fanToggle.SetIsOnWithoutNotify(isOn);
                        ApplyFanInstant(isOn);
                    }
                }
                catch { Debug.LogWarning("FetchFanOnce parse failed"); }
            }
        ));
    }

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
        else
        {
            // 주기만 바뀐 경우: 다음 사이클부터 자연히 반영됨
            // (별도 처리 불필요)
        }
    }
}
