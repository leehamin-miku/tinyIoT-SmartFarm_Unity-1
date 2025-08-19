// Assets/Scripts/ConfigPanel.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfigPanel : MonoBehaviour
{
    [Header("Targets")]
    public SensorDisplay sensor;        // 센서 refreshInterval만 사용
    public ActuatorDisplay actuator;    // autoAssignSun + Fetch Settings만 사용
    public UIManager ui;

    [Header("Inputs - Sensor")]
    public TMP_InputField Inp_SensorInterval;

    [Header("Inputs - Actuator")]
    public Toggle Tgl_AutoAssignSun;
    public Toggle Tgl_FetchOnStart;
    public Toggle Tgl_AutoRefresh;
    public TMP_InputField Inp_ActInterval;

    void OnEnable() => LoadFromComponents();

    // 현재 컴포넌트 값 → UI 채우기
    public void LoadFromComponents()
    {
        if (sensor && Inp_SensorInterval)
            Inp_SensorInterval.text = sensor.refreshInterval.ToString("0.##");

        if (actuator)
        {
            if (Tgl_AutoAssignSun) Tgl_AutoAssignSun.isOn = actuator.autoAssignSun;
            if (Tgl_FetchOnStart)  Tgl_FetchOnStart.isOn  = actuator.fetchOnStart;
            if (Tgl_AutoRefresh)   Tgl_AutoRefresh.isOn   = actuator.autoRefresh;
            if (Inp_ActInterval)   Inp_ActInterval.text   = actuator.refreshInterval.ToString("0.##");
        }
    }

    // Apply & Close 버튼
    public void ApplyAndClose()
    {
        // Sensor: refreshInterval만
        if (sensor && Inp_SensorInterval && float.TryParse(Inp_SensorInterval.text, out var sInt))
            sensor.refreshInterval = Mathf.Max(0.1f, sInt);

        // Actuator: autoAssignSun + Fetch Settings만
        if (actuator)
        {
            // autoAssignSun
            if (Tgl_AutoAssignSun)
            {
                bool wantAuto = Tgl_AutoAssignSun.isOn;

                // 처음 ON으로 바뀌었고 sun이 비어있으면 즉시 한번 할당 시도
                if (wantAuto && !actuator.autoAssignSun && actuator.sun == null)
                {
                    if (RenderSettings.sun) actuator.sun = RenderSettings.sun;
                    if (actuator.sun == null)
                    {
                        var lights = FindObjectsOfType<Light>();
                        foreach (var l in lights)
                        {
                            if (l && l.type == LightType.Directional) { actuator.sun = l; break; }
                        }
                    }
                }
                actuator.autoAssignSun = wantAuto;
            }

            // Fetch Settings: fetchOnStart / autoRefresh / refreshInterval
            if (Tgl_FetchOnStart) actuator.fetchOnStart = Tgl_FetchOnStart.isOn;
            if (Tgl_AutoRefresh)  actuator.autoRefresh  = Tgl_AutoRefresh.isOn;
            if (Inp_ActInterval && float.TryParse(Inp_ActInterval.text, out var aInt))
                actuator.refreshInterval = Mathf.Max(0.1f, aInt);

            // (필요 시) 간단 저장
            PlayerPrefs.SetFloat("sensor.interval", sensor ? sensor.refreshInterval : 5f);
            PlayerPrefs.SetInt("act.autoassign", actuator.autoAssignSun ? 1 : 0);
            PlayerPrefs.SetInt("act.fetchOnStart", actuator.fetchOnStart ? 1 : 0);
            PlayerPrefs.SetInt("act.autoRefresh", actuator.autoRefresh ? 1 : 0);
            PlayerPrefs.SetFloat("act.interval", actuator.refreshInterval);
            PlayerPrefs.Save();
        }

        ui?.CloseConfig();
    }

    public void CloseOnly() => ui?.CloseConfig();

    // (선택) 시작 시 저장된 값 적용하고 싶을 때 호출
    public void ApplySavedAtBoot()
    {
        if (sensor && PlayerPrefs.HasKey("sensor.interval"))
            sensor.refreshInterval = PlayerPrefs.GetFloat("sensor.interval");

        if (actuator)
        {
            actuator.autoAssignSun = PlayerPrefs.GetInt("act.autoassign", actuator.autoAssignSun ? 1 : 0) == 1;
            actuator.fetchOnStart  = PlayerPrefs.GetInt("act.fetchOnStart", actuator.fetchOnStart ? 1 : 0) == 1;
            actuator.autoRefresh   = PlayerPrefs.GetInt("act.autoRefresh",  actuator.autoRefresh ? 1 : 0) == 1;
            actuator.refreshInterval = PlayerPrefs.GetFloat("act.interval", actuator.refreshInterval);

            // autoAssignSun이 켜져 있고 아직 sun이 없으면 한 번 더 할당 시도
            if (actuator.autoAssignSun && actuator.sun == null)
            {
                if (RenderSettings.sun) actuator.sun = RenderSettings.sun;
                if (actuator.sun == null)
                {
                    var lights = FindObjectsOfType<Light>();
                    foreach (var l in lights)
                    {
                        if (l && l.type == LightType.Directional) { actuator.sun = l; break; }
                    }
                }
            }
        }
    }
}
