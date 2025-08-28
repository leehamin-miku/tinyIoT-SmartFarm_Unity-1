using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject Sensors_panel;
    public GameObject Actuators_panel;

    [Header("Config")]
    public GameObject ConfigModal;
    public ConfigPanel configPanel;

    [Header("Camera Rig")]
    public CameraRigController camRig;   // 없으면 이동 스킵

    string _current = ""; // "Sensors" / "Actuators" / ""(모두 닫힘)

    // === Buttons ===
    public void ClickOutside() => camRig?.GoOutside();
    public void ClickInside() => camRig?.GoInside();
    public void ClickSensors() => TogglePanel("Sensors");
    public void ClickActuators() => TogglePanel("Actuators");

    // === Helpers ===

    void TogglePanel(string name)
    {
        _current = (_current == name) ? "" : name;
        if (Sensors_panel) Sensors_panel.SetActive(_current == "Sensors");
        if (Actuators_panel) Actuators_panel.SetActive(_current == "Actuators");
    }

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
