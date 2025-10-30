using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraJoystick : MonoBehaviour,
    IPointerDownHandler,   // Ŭ�� ����
    IDragHandler,          // �巡�� ��
    IPointerUpHandler      // Ŭ�� ����
{
    public RectTransform center;
    public CameraRigController crc;
    Vector2 vec;

    // Ŭ�� ���� (���콺/��ġ ����)
    public void OnPointerDown(PointerEventData eventData)
    {
        vec = eventData.position-(Vector2)transform.position;

        vec = vec.magnitude > 60 ? vec.normalized * 60 : vec;
        center.anchoredPosition = vec;
        crc.JoystickControl(vec);
    }


    // �巡�� ��
    public void OnDrag(PointerEventData eventData)
    {
        vec = eventData.position - (Vector2)transform.position;

        vec = vec.magnitude > 60 ? vec.normalized * 60 : vec;
        center.anchoredPosition = vec;
        crc.JoystickControl(vec);
    }


    // Ŭ�� ���� (�巡�� �� �ص� ����)
    public void OnPointerUp(PointerEventData eventData)
    {
        vec = Vector2.zero;
        center.anchoredPosition = vec;
        crc.JoystickControl(vec);
    }
}
