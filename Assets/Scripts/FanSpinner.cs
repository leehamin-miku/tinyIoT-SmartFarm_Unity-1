using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FanSpinner : MonoBehaviour
{
    [Header("Targets")]
    public Transform[] blades;            // 회전시킬 허브/날개

    [Header("Speed")]
    public float rpmOn = 900f;            // ON일 때 목표 RPM
    public float accelRPMperSec = 1500f;  // 가감속량(부드럽게)

    [Header("Axis")]
    public Vector3 localAxis = Vector3.forward; // 모델 축에 맞게(Z/Y 등)

    float _targetRPM = 0f;
    float _currentRPM = 0f;

    void Update()
    {
        _currentRPM = Mathf.MoveTowards(_currentRPM, _targetRPM, accelRPMperSec * Time.deltaTime);
        float degPerSec = _currentRPM * 360f / 60f;
        float delta = degPerSec * Time.deltaTime;
        if (Mathf.Abs(delta) > 0.001f && blades != null)
            foreach (var t in blades) if (t) t.Rotate(localAxis, delta, Space.Self);
    }

    // 토글에서 직접 호출할 함수
    public void SetOn(bool on) => _targetRPM = on ? rpmOn : 0f;
}
