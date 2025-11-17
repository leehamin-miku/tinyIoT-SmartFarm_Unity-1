using IoT;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject Sensors_panel;
    public GameObject Actuators_panel;
    public GameObject config_panel;
    public GameObject crops_panel;

    // 좌상단 기준 여백과 패널 간 간격
    public float dockLeft = 10f;
    public float dockTop = 10f;
    public float dockSpacing = 15f;

    [SerializeField] Image onlineIcon;
    [SerializeField] TextMeshProUGUI onlineText;

    [Header("Camera Rig")]
    public CameraRigController camRig;   // 없으면 이동 스킵

    // === Buttons ===
    public void ClickOutside() => camRig?.GoOutside();
    public void ClickInside() => camRig?.GoInside();
    public void ClickSensors() => TogglePanel(Sensors_panel);
    public void ClickActuators() => TogglePanel(Actuators_panel);
    public void ClickCrops() => TogglePanel(crops_panel);
    public void ClickSetting() => TogglePanel(config_panel);

    // === Helpers ===

    void TogglePanel(GameObject go)
    {
        if (go.activeInHierarchy)
        {
            go.SetActive(false);
            return;
        }
        else
        {
            Sensors_panel.SetActive(false);
            Actuators_panel.SetActive(false);
            config_panel.SetActive(false);
            crops_panel.SetActive(false);

            go.SetActive(true);
        }


        //if (name == "Sensors" && Sensors_panel)
        //    Sensors_panel.SetActive(!Sensors_panel.activeSelf);
        //if (name == "Actuators" && Actuators_panel)
        //    Actuators_panel.SetActive(!Actuators_panel.activeSelf);
        //if (name == "Crops" && crops_panel)
        //    crops_panel.SetActive(!crops_panel.activeSelf);

        //UpdatePanelLayout(); // ← 여기만 추가
    }
    
    //void UpdatePanelLayout()
    //{
    //    var sOn = Sensors_panel && Sensors_panel.activeSelf;
    //    var aOn = Actuators_panel && Actuators_panel.activeSelf;
    //    if (!sOn && !aOn) return;

    //    var srt = Sensors_panel ? Sensors_panel.GetComponent<RectTransform>() : null;
    //    var art = Actuators_panel ? Actuators_panel.GetComponent<RectTransform>() : null;

    //    // 앵커/피벗을 좌상단으로 통일
    //    void TopLeft(RectTransform rt) { if (!rt) return; rt.anchorMin = rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1); }
    //    TopLeft(srt); TopLeft(art);

    //    Vector2 topLeft = new Vector2(dockLeft, -dockTop);

    //    if (sOn && !aOn)
    //    {
    //        srt.anchoredPosition = topLeft;
    //    }
    //    else if (!sOn && aOn)
    //    {
    //        art.anchoredPosition = topLeft;
    //    }
    //    else // 둘 다 ON → 센서는 위, 액츄에이터는 아래
    //    {
    //        Canvas.ForceUpdateCanvases();
    //        if (srt) LayoutRebuilder.ForceRebuildLayoutImmediate(srt);

    //        float sh = srt ? Mathf.Max(LayoutUtility.GetPreferredHeight(srt), srt.rect.height) : 0f;

    //        if (srt) srt.anchoredPosition = topLeft;
    //        if (art) art.anchoredPosition = topLeft + new Vector2(0, -(sh + dockSpacing));
    //    }
    //}

    //void OnEnable() => UpdatePanelLayout();

    public void ConfigButtonClick()
    {
        config_panel.SetActive(!config_panel.activeSelf);
    }
    public void ConfigCloseButtonClick()
    {
        config_panel.SetActive(false);
    }

    public void Connectioned()
    {
        onlineIcon.color = Color.green;
        onlineText.color = Color.green;

        if (OneM2M.ProtocolIndex == 0)
        {
            onlineText.text = "http";
        }
        else
        {
            onlineText.text = "websocket";
        }

    }

    public void Connecting()
    {
        onlineIcon.color = Color.yellow;
        onlineText.color = Color.yellow;
        onlineText.text = "connecting";
    }

    public void Disconnectioned()
    {
        onlineIcon.color = Color.red;
        onlineText.text = "offline";
        onlineText.color = Color.red;
    }
    private void Start()
    {
        OneM2M.AddConnectedEvent(Connectioned);
        OneM2M.AddDisconnectedEvent(Disconnectioned);
        OneM2M.AddConnectingEvent(Connecting);
    }
}
