using IoT;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ActuatorDisplay : MonoBehaviour
{

    
    // ===== LED =====
    [Header("LED Control (Brightness Steps)")]
    public Slider LED_Slider;                 
    public TextMeshProUGUI LED_ValueText;

    [Header("Door Control")]
    [SerializeField] Door door;
    public ToggleVisual doorToggle;


    [Header("lamp Light (spot)")]
    public List<Light> lamps;                         
    public bool autoAssignSun = true;         

    // ===== FAN =====
    [Header("Fan Control")]
    public ToggleVisual fanToggle;
    [Header("Fan Visual (Spinner)")]
    public FanSpinner fanSpinner;

    
    //[Header("Fan Toggle Visual")]
    //public Image fanBackground;
    //public RectTransform fanHandle;
    //public Color fanOnColor  = new Color32(65,192,83,255);
    //public Color fanOffColor = new Color32(150,150,150,255);
    //public float fanAnimTime = 0.15f;
    //public Vector2 fanOffAnchoredPos = new Vector2(10f, -10f);
    //public Vector2 fanOnAnchoredPos  = new Vector2(30f, -10f);

    // ===== WATER =====
    [Header("Watering Control")]
    public ToggleVisual waterToggle;
    //[Header("Water Toggle Visual")]
    //public Image waterBackground;
    //public RectTransform waterHandle;
    //public Color waterOnColor  = new Color32(65,192,83,255);
    //public Color waterOffColor = new Color32(150,150,150,255);
    //public float waterAnimTime = 0.15f;
    //public Vector2 waterOffAnchoredPos = new Vector2(10f, -10f);
    //public Vector2 waterOnAnchoredPos  = new Vector2(30f, -10f);

    [Header("Water FX (optional)")]
    public WaterSprinklerPS waterFX; // 있으면 파티클 동기화

    bool isDragging = false;
    Vector2 fanOnPos, fanOffPos, waterOnPos, waterOffPos;
    Coroutine fanAnimCo, waterAnimCo, autoCo;

    //void Awake()
    //{
    //    if (autoAssignSun)
    //    {
    //        if (RenderSettings.sun) sun = RenderSettings.sun;
    //        if (!sun)
    //            foreach (var l in FindObjectsOfType<Light>())
    //                if (l && l.type == LightType.Directional) { sun = l; break; }
    //    }
    //}

    void Start()
    {
        // LED
        if (LED_Slider)
        {
            LED_Slider.wholeNumbers = true;
            LED_Slider.minValue = 0f; LED_Slider.maxValue = 10f;
            LED_Slider.onValueChanged.AddListener(LEDFetch);
            OneM2M.AddValueRefreshEvent(LEDMirror);
        }

        // Fan
        if (fanToggle)
        {
            fanToggle.onValueChanged.AddListener(FanFetch);
            OneM2M.AddValueRefreshEvent(FanMirror);
        }

        // Water
        if (waterToggle)
        {
            waterToggle.onValueChanged.AddListener(WaterFetch);
            OneM2M.AddValueRefreshEvent(WaterMirror);
        }

        if (doorToggle)
        {
            doorToggle.onValueChanged.AddListener(DoorFetch);
            OneM2M.AddValueRefreshEvent(DoorMirror);
        }
        //Mirrors();
        Mirrors();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        //Mirrors();
    }

    //void Update()
    //{
    //    if (isDragging && Input.GetMouseButtonUp(0))
    //    {
    //        isDragging = false;
    //        int ledValue = Mathf.RoundToInt(LED_Slider.value);
    //        StartCoroutine(SendLEDValueToServer(ledValue));
    //    }
    //}



    // ===== LED =====
    //void OnSliderValueChanged(float value)
    //{
    //    int step = Mathf.RoundToInt(value);
    //    UpdateSliderText(step);
    //    ApplySunIntensityStep(step);
    //    isDragging = true;
    //}


    // ===== FAN =====
    //void OnFanToggleChanged(bool isOn)
    //{
    //    StartFanAnimate(isOn);
    //    StartCoroutine(SendFanStateToServer(isOn));
    //    if (fanSpinners != null)
    //        foreach (var sp in fanSpinners) if (sp) sp.SetOn(isOn);
    //}
    //IEnumerator SendFanStateToServer(bool isOn)
    //{
    //    string jsonBody = new JObject {
    //        ["m2m:cin"] = new JObject { ["con"] = ToOnOff(isOn) }
    //    }.ToString();

    //    yield return StartCoroutine(OneM2M.PostDataCoroutine(
    //        origin: "CAdmin",
    //        type: 4,
    //        body: jsonBody,
    //        url: "TinyFarm/Actuators/Fan"
    //    ));
    //}



    void FanMirror()
    {
        bool isOn = OneM2M.ParseOnOff(OneM2M.GetFarmParameter(OneM2M.EFarmParameter.Fan));
        fanToggle.SetIsOnWithoutNotify(isOn);
        if (fanSpinner) fanSpinner.SetOn(isOn);
    }

    void FanFetch(bool isOn)
    {
        OneM2M.SetFarmParameter(OneM2M.EFarmParameter.Fan, OneM2M.ToOnOff(isOn));
        if (fanSpinner) fanSpinner.SetOn(isOn);
    }

    // ===== WATER =====



    void WaterMirror()
    {
        bool isOn = OneM2M.ParseOnOff(OneM2M.GetFarmParameter(OneM2M.EFarmParameter.Water));
        //ApplyWaterInstant(isOn);
        waterToggle.SetIsOnWithoutNotify(isOn);
        if (waterFX) waterFX.SetState(isOn); // 파티클
    }

    void WaterFetch(bool isOn)
    {
        OneM2M.SetFarmParameter(OneM2M.EFarmParameter.Water, OneM2M.ToOnOff(isOn));
        if (waterFX) waterFX.SetState(isOn); // 파티클
        //ApplyWaterInstant(isOn); //일단 반영하고 보자!
    }




    void DoorMirror()
    {
        bool isOn = OneM2M.ParseOnOff(OneM2M.GetFarmParameter(OneM2M.EFarmParameter.Door));
        //ApplyWaterInstant(isOn);
        doorToggle.SetIsOnWithoutNotify(isOn);
        door.SetDoor(isOn);
        //if (waterFX) waterFX.SetState(isOn); // 파티클
    }

    void DoorFetch(bool isOn)
    {
        OneM2M.SetFarmParameter(OneM2M.EFarmParameter.Door, OneM2M.ToOnOff(isOn));
        //if (waterFX) waterFX.SetState(isOn); // 파티클
        door.SetDoor(isOn);
        //ApplyWaterInstant(isOn); //일단 반영하고 보자!
    }


    

    void LEDMirror()
    {

        try
        {
            int step = int.Parse(OneM2M.GetFarmParameter(OneM2M.EFarmParameter.LED));
            LED_ValueText.text = step.ToString();
            LED_Slider.SetValueWithoutNotify(step);
            foreach (Light i in lamps)
            {
                i.intensity = Mathf.Clamp(step, 0, 10) * 0.1f;
            }
        }
        catch
        {
            LED_ValueText.text = "--";
        }
        
        
    }

    void LEDFetch(float value)
    {
        int step = Mathf.RoundToInt(value);
        LED_ValueText.text = step.ToString();


        foreach (Light i in lamps)
        {
            i.intensity = Mathf.Clamp(step, 0, 10) * 0.1f;
        }

        if (step != int.Parse(OneM2M.GetFarmParameter(OneM2M.EFarmParameter.LED)))
        {
            OneM2M.SetFarmParameter(OneM2M.EFarmParameter.LED, step.ToString());
        }
    }


    //void ApplyWaterInstant(bool isOn)
    //{
    //    if (!waterBackground || !waterHandle) return;
    //    waterBackground.color = isOn ? waterOnColor : waterOffColor;
    //    waterHandle.anchoredPosition = isOn ? waterOnPos : waterOffPos;



    //    //Start Water Animation
    //    if (waterAnimCo != null) StopCoroutine(waterAnimCo);
    //    waterAnimCo = StartCoroutine(DoToggleAnimate(
    //        waterBackground, waterHandle, isOn, waterOnColor, waterOffColor, waterOnPos, waterOffPos, waterAnimTime
    //    ));
    //}

    //void ApplyFanInstant(bool isOn)
    //{
    //    //if (!fanBackground || !fanHandle) return;
    //    //fanBackground.color = isOn ? fanOnColor : fanOffColor;
    //    //fanHandle.anchoredPosition = isOn ? fanOnPos : fanOffPos;




    //    //Start Fan Animation
    //    //if (fanAnimCo != null) StopCoroutine(fanAnimCo);
    //    //fanAnimCo = StartCoroutine(DoToggleAnimate(
    //    //    fanBackground, fanHandle, isOn, fanOnColor, fanOffColor, fanOnPos, fanOffPos, fanAnimTime
    //    //));
    //}
    //?
    // ===== 공통 토글 애니메이션 =====
    //IEnumerator DoToggleAnimate(Image bg, RectTransform knob, bool isOn,
    //                            Color onColor, Color offColor,
    //                            Vector2 onPos, Vector2 offPos, float secs)
    //{
    //    if (!bg || !knob) yield break;
    //    float t = 0f;
    //    Color c0 = bg.color, c1 = isOn ? onColor : offColor;
    //    Vector2 p0 = knob.anchoredPosition, p1 = isOn ? onPos : offPos;
    //    while (t < secs)
    //    {
    //        t += Time.unscaledDeltaTime;
    //        float u = Mathf.Clamp01(t / secs);
    //        bg.color = Color.Lerp(c0, c1, u);
    //        knob.anchoredPosition = Vector2.Lerp(p0, p1, u);
    //        yield return null;
    //    }
    //    bg.color = c1; knob.anchoredPosition = p1;
    //}

    //void OnRectTransformDimensionsChange()
    //{
    //    if (fanToggle)   ApplyFanInstant(fanToggle.isOn);
    //    if (waterToggle) ApplyWaterInstant(waterToggle.isOn);
    //}

    // ===== FETCH & AUTO REFRESH =====
    void Mirrors()
    {
        ////ToDo 무한반복 안되도록 로직 수정
        //void SubFuction(Toggle toggle, OneM2M.EFarmParameter type)
        //{
        //    toggle.SetIsOnWithoutNotify(OneM2M.ParseOnOff(OneM2M.GetFarmParameter(type)));
        //}
        //SubFuction(fanToggle, OneM2M.EFarmParameter.Fan);
        //SubFuction(waterToggle, OneM2M.EFarmParameter.Water);

        //LED_ValueText.text = Mathf.RoundToInt(float.Parse(OneM2M.GetFarmParameter(OneM2M.EFarmParameter.LED))).ToString();
        //LED_Slider.SetValueWithoutNotify(Mathf.RoundToInt(float.Parse(OneM2M.GetFarmParameter(OneM2M.EFarmParameter.LED))));
        FanMirror();
        WaterMirror();
        LEDMirror();
        DoorMirror();
    }
}
