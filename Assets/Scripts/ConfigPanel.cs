using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfigPanel : MonoBehaviour
{
    [Header("Targets")]
    public SensorDisplay sensor;        // refreshInterval & autoRefresh 적용
    public ActuatorDisplay actuator;    // autoRefresh만 동기화(주기는 그대로)
    public UIManager ui;

    [Header("Inputs")]
    public TMP_InputField Inp_SensorInterval; // seconds
    public Toggle Tgl_AutoRefresh;            // global toggle (sensor + actuator)

    void OnEnable() => LoadFromComponents();

    // 현재 컴포넌트 값 → UI 채우기
    public void LoadFromComponents()
    {
        if (sensor && Inp_SensorInterval)
            Inp_SensorInterval.text = sensor.refreshInterval.ToString("0.##");

        // 토글은 센서를 기준으로 보여주되, 센서가 없으면 액추에이터 값을 사용
        if (Tgl_AutoRefresh)
        {
            if (sensor) Tgl_AutoRefresh.isOn = sensor.autoRefresh;
            else if (actuator) Tgl_AutoRefresh.isOn = actuator.autoRefresh;
            else Tgl_AutoRefresh.isOn = false;
        }
    }

    // Apply & Close 버튼
    public void ApplyAndClose()
    {
        // 1) Interval: 센서만 적용 (UI 라벨이 Sensor Refresh Interval 이므로)
        float sInt = sensor ? sensor.refreshInterval : 5f;
        if (sensor && Inp_SensorInterval && float.TryParse(Inp_SensorInterval.text, out var parsed))
            sInt = Mathf.Max(0.1f, parsed);

        // 2) AutoRefresh: 센서/액추에이터 둘 다 동일하게 적용
        bool auto = Tgl_AutoRefresh ? Tgl_AutoRefresh.isOn : false;

        // --- 센서 반영 ---
        if (sensor)
        {
            // fetchOnStart 기존 값 보존, 주기/자동갱신만 반영
            sensor.ApplyFetchSettings(sensor.fetchOnStart, auto, sInt);
        }

        // --- 액추에이터 반영 ---
        if (actuator)
        {
            // 액추에이터는 주기 입력 칸이 없으므로 기존 주기 유지
            actuator.ApplyFetchSettings(actuator.fetchOnStart, auto, actuator.refreshInterval);
        }

        // (선택) 간단 저장
        //PlayerPrefs.SetFloat("sensor.interval", sInt);
        //PlayerPrefs.SetInt("global.autoRefresh", auto ? 1 : 0);
        //PlayerPrefs.Save();

        ui?.CloseConfig();
    }

    public void CloseOnly() => ui?.CloseConfig();

    // (선택) 시작 시 저장된 값 적용하고 싶을 때 호출
    public void Start()
    {
        //float savedInterval = PlayerPrefs.GetFloat("sensor.interval",
        //    sensor ? sensor.refreshInterval : 5f);
        //bool savedAuto = PlayerPrefs.GetInt("global.autoRefresh",
        //    (sensor && sensor.autoRefresh) || (actuator && actuator.autoRefresh) ? 1 : 0) == 1;

        if (sensor)
            sensor.ApplyFetchSettings(sensor.fetchOnStart, sensor.autoRefresh, sensor.refreshInterval);

        if (actuator)
            actuator.ApplyFetchSettings(actuator.fetchOnStart, actuator.autoRefresh, actuator.refreshInterval);
    }
}
