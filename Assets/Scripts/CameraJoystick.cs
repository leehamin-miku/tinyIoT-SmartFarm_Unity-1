using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraJoystick : MonoBehaviour,
    IPointerDownHandler,   // 클릭 시작
    IDragHandler,          // 드래그 중
    IPointerUpHandler      // 클릭 해제
{
    public RectTransform center;
    public CameraRigController crc;
    Vector2 vec;

    // 클릭 시작 (마우스/터치 눌림)
    public void OnPointerDown(PointerEventData eventData)
    {
        vec = eventData.position-(Vector2)transform.position;

        vec = vec.magnitude > 60 ? vec.normalized * 60 : vec;
        center.anchoredPosition = vec;
        crc.JoystickControl(vec);
    }


    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        vec = eventData.position - (Vector2)transform.position;

        vec = vec.magnitude > 60 ? vec.normalized * 60 : vec;
        center.anchoredPosition = vec;
        crc.JoystickControl(vec);
    }


    // 클릭 해제 (드래그 안 해도 동작)
    public void OnPointerUp(PointerEventData eventData)
    {
        vec = Vector2.zero;
        center.anchoredPosition = vec;
        crc.JoystickControl(vec);
    }
}
