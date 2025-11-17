using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IoT;

public class ConfigPanel : MonoBehaviour
{
    [Header("Targets")]
    public UIManager ui;

    [Header("Inputs")]
    public TMP_InputField Inp_SensorInterval; // seconds
    public Toggle Tgl_AutoRefresh;            // global toggle (sensor + actuator)
    public TMP_Dropdown protocolSelect;
    public Button makeConnection;
    //public Button close;

    



    void OnEnable() => LoadFromComponents();

    const float minimum = 0.1f;
    const float defaultValue = 5f;

    // 현재 컴포넌트 값 → UI 채우기
    public void LoadFromComponents()
    {

        Inp_SensorInterval.text = OneM2M.RefreshInterval.ToString("0.##");
        // 토글은 센서를 기준으로 보여주되, 센서가 없으면 액추에이터 값을 사용
        Tgl_AutoRefresh.isOn = OneM2M.AutoRefresh;
        protocolSelect.value = OneM2M.ProtocolIndex;
        
    }


    
    //public void Close()
    //{
    //    ui.ConfigCloseButtonClick();
    //}

    public void Start()
    {
        OneM2M.AddConnectedEvent(Connectioned);
        OneM2M.AddDisconnectedEvent(Disconnectioned);
        OneM2M.AddConnectingEvent(Connecting);
        //close.onClick.AddListener(Close);
        makeConnection.onClick.AddListener(OneM2M.MakeConnectionOrDisconnect);
        protocolSelect.onValueChanged.AddListener(OneM2M.ConnectionChange);
        Tgl_AutoRefresh.onValueChanged.AddListener(autoRefreshChanged);
        Inp_SensorInterval.onEndEdit.AddListener(OnIntervalValueEndEdit);
    }


    void autoRefreshChanged(bool isOn)
    {
        OneM2M.AutoRefresh = isOn;
    }

    void OnIntervalValueEndEdit(string input)
    {
        // 숫자 판별
        if (float.TryParse(input, out float number))
        {
            if(number > minimum)
            {
                Inp_SensorInterval.text = number.ToString();
                OneM2M.RefreshInterval = number;
            } else
            {
                Inp_SensorInterval.text = minimum.ToString();
                OneM2M.RefreshInterval = minimum;
            }
        } else
        {
            Inp_SensorInterval.text = defaultValue.ToString();
            OneM2M.RefreshInterval = defaultValue;
        }
    }

    void OnProtocolValueChanged(int index)
    {
        OneM2M.ConnectionChange(index);
    }


    public void Connectioned()
    {
        makeConnection.GetComponentInChildren<TextMeshProUGUI>().text = "connected";

    }

    public void Connecting()
    {
        makeConnection.GetComponentInChildren<TextMeshProUGUI>().text = "connecting";
    }

    public void Disconnectioned()
    {
        makeConnection.GetComponentInChildren<TextMeshProUGUI>().text = "disconnected";
    }




    // Apply & Close 버튼
    //public void ApplyAndClose()
    //{
    //    // 1) Interval: 센서만 적용 (UI 라벨이 Sensor Refresh Interval 이므로)
    //    float sInt = sensor ? sensor.refreshInterval : 5f;
    //    if (sensor && Inp_SensorInterval && float.TryParse(Inp_SensorInterval.text, out var parsed))
    //        sInt = Mathf.Max(0.1f, parsed);

    //    // 2) AutoRefresh: 센서/액추에이터 둘 다 동일하게 적용
    //    bool auto = Tgl_AutoRefresh ? Tgl_AutoRefresh.isOn : false;

    //    // --- 센서 반영 ---
    //    if (sensor)
    //    {
    //        // fetchOnStart 기존 값 보존, 주기/자동갱신만 반영
    //        sensor.ApplyFetchSettings(sensor.fetchOnStart, auto, sInt);
    //    }

    //    // --- 액추에이터 반영 ---
    //    if (actuator)
    //    {
    //        // 액추에이터는 주기 입력 칸이 없으므로 기존 주기 유지
    //        actuator.ApplyFetchSettings(actuator.fetchOnStart, auto, actuator.refreshInterval);
    //    }

    //    // (선택) 간단 저장
    //    //PlayerPrefs.SetFloat("sensor.interval", sInt);
    //    //PlayerPrefs.SetInt("global.autoRefresh", auto ? 1 : 0);
    //    //PlayerPrefs.Save();

    //    ui?.CloseConfig();
    //}
}
