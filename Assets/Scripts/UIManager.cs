using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject Sensors_panel;
    public GameObject Actuators_panel;
    
    // 좌상단 기준 여백과 패널 간 간격
    public float dockLeft = 10f;
    public float dockTop = 10f;
    public float dockSpacing = 8f;

    [Header("Config")]
    public GameObject ConfigModal;
    public ConfigPanel configPanel;

    [Header("Camera Rig")]
    public CameraRigController camRig;   // 없으면 이동 스킵

    // === Buttons ===
    public void ClickOutside() => camRig?.GoOutside();
    public void ClickInside() => camRig?.GoInside();
    public void ClickSensors() => TogglePanel("Sensors");
    public void ClickActuators() => TogglePanel("Actuators");

    // === Helpers ===

    void TogglePanel(string name)
    {
        if (name == "Sensors" && Sensors_panel)
            Sensors_panel.SetActive(!Sensors_panel.activeSelf);
        if (name == "Actuators" && Actuators_panel)
            Actuators_panel.SetActive(!Actuators_panel.activeSelf);

        UpdatePanelLayout(); // ← 여기만 추가
    }
    
    void UpdatePanelLayout()
    {
        var sOn = Sensors_panel && Sensors_panel.activeSelf;
        var aOn = Actuators_panel && Actuators_panel.activeSelf;
        if (!sOn && !aOn) return;

        var srt = Sensors_panel ? Sensors_panel.GetComponent<RectTransform>() : null;
        var art = Actuators_panel ? Actuators_panel.GetComponent<RectTransform>() : null;

        // 앵커/피벗을 좌상단으로 통일
        void TopLeft(RectTransform rt) { if (!rt) return; rt.anchorMin = rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1); }
        TopLeft(srt); TopLeft(art);

        Vector2 topLeft = new Vector2(dockLeft, -dockTop);

        if (sOn && !aOn)
        {
            srt.anchoredPosition = topLeft;
        }
        else if (!sOn && aOn)
        {
            art.anchoredPosition = topLeft;
        }
        else // 둘 다 ON → 센서는 위, 액츄에이터는 아래
        {
            Canvas.ForceUpdateCanvases();
            if (srt) LayoutRebuilder.ForceRebuildLayoutImmediate(srt);

            float sh = srt ? Mathf.Max(LayoutUtility.GetPreferredHeight(srt), srt.rect.height) : 0f;

            if (srt) srt.anchoredPosition = topLeft;
            if (art) art.anchoredPosition = topLeft + new Vector2(0, -(sh + dockSpacing));
        }
    }

    void OnEnable() => UpdatePanelLayout();

    public void OpenConfig()
    {
        if (ConfigModal) ConfigModal.SetActive(true);
        configPanel?.LoadFromComponents();
    }
    public void CloseConfig()
    {
        if (ConfigModal) ConfigModal.SetActive(false);
    }
}
