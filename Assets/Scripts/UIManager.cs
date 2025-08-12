using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject Sensors_panel;
    public GameObject Actuators_panel;
    public GameObject Config_panel;
    public GameObject Log_panel;

    public void ShowPanel(string panelName)
    {
        Log_panel.SetActive(panelName == "MAIN");
        Sensors_panel.SetActive(panelName == "Sensors");
        Actuators_panel.SetActive(panelName == "Actuators");
        Config_panel.SetActive(panelName == "Config");
    }
}
