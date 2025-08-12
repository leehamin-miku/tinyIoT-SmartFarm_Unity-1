using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IoT;
using Newtonsoft.Json.Linq;
using System.Collections;

public class ActuatorDisplay : MonoBehaviour
{
    [Header("LED Control")]
    public Slider LED_Slider;
    public TextMeshProUGUI LED_ValueText;
    public Image backgroundPanel; // 전체 배경 어둡게
    public Color baseColor = new Color(0, 0, 0, 0); // 검은색(알파 0)

    [Header("Fan Control")]
    public Toggle fanToggle; // Toggle conponant

    [Header("Fan Toggle Visual")]
    public Image fanBackground;          // 둥근 직사각형 바탕(UISprite/Sliced)
    public RectTransform fanHandle;      // 흰색 원(Handle, Knob)
    public Color fanOnColor  = new Color32(65,192,83,255);   // green
    public Color fanOffColor = new Color32(150,150,150,255);  // grey
    public float fanPadding = 2f;        // 좌우 여백
    public float fanAnimTime = 0.15f;    // 슬라이드 시간(초)

    [Header("Fan Handle Fixed Positions")]
    public Vector2 fanOffAnchoredPos = new Vector2(10f, -10f);
    public Vector2 fanOnAnchoredPos  = new Vector2(30f, -10f);

    private bool isDragging = false;

    // Fan 토글 애니메이션용 내부 상태
    Vector2 fanOnPos, fanOffPos;
    Coroutine fanAnimCo;

    void Start()
    {
        // LED 초기화
        UpdateSliderText(LED_Slider.value);
        LED_Slider.onValueChanged.AddListener(OnSliderValueChanged);

        // Fan 초기화
        if (fanToggle != null)
        {
            fanToggle.onValueChanged.AddListener(OnFanToggleChanged);
            CacheFanPositions();
            ApplyFanInstant(fanToggle.isOn); // 시작 상태 반영
        }
    }

    void Update()
    {
        // LED 값 서버 전송 (마우스 드래그 끝났을 때 딱 한 번)
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            int ledValue = Mathf.RoundToInt(LED_Slider.value);
            StartCoroutine(SendLEDValueToServer(ledValue));
        }
    }

    // ===== LED =====
    void OnSliderValueChanged(float value)
    {
        UpdateSliderText(value);
        UpdateBackgroundBrightness(value / 10f); // 0~10 → 0~1
        isDragging = true;
    }

    void UpdateSliderText(float value)
    {
        LED_ValueText.text = Mathf.RoundToInt(value).ToString();
    }

    void UpdateBackgroundBrightness(float brightness)
    {
        if (backgroundPanel != null)
        {
            Color newColor = baseColor;
            float alpha = Mathf.Lerp(200f / 255f, 0f, brightness);
            newColor.a = alpha;
            backgroundPanel.color = newColor;
        }
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

    // ===== Fan =====
    void OnFanToggleChanged(bool isOn)
    {
        // 1) 비주얼 애니메이션
        StartFanAnimate(isOn);
        // 2) 서버 전송
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

    // 캔버스 리사이징/해상도 변경 시 위치 재계산
    void OnRectTransformDimensionsChange()
    {
        CacheFanPositions();
        if (fanToggle != null) ApplyFanInstant(fanToggle.isOn);
    }
}
