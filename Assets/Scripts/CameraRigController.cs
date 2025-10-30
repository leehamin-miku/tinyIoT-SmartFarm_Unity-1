using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRigController : MonoBehaviour
{
    public Transform cam;                 // 비우면 Camera.main 사용
    public Transform outsideAnchor;       // 바깥 뷰 기준점
    public Transform internalAnchor;      // 내부 뷰 기준점
    public float moveSeconds = 0.6f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    Coroutine moveCo;
    public Vector2 joystickVec;
    public Quaternion cameraRotation;

    public void GoOutside()  => MoveTo(outsideAnchor);
    public void GoInside() => MoveTo(internalAnchor);


    private void Start()
    {
        GoInside();
    }

    void MoveTo(Transform anchor)
    {
        if (anchor == null) return;
        if (moveCo != null) StopCoroutine(moveCo);
        var c = cam != null ? cam : Camera.main.transform;
        moveCo = StartCoroutine(Move(anchor));
    }

    IEnumerator Move(Transform target)
    {
        Vector3 p0 = cam.position; Quaternion r0 = cam.rotation;
        Vector3 p1 = target.position; Quaternion r1 = target.rotation;
        float t = 0f;
        while (t < moveSeconds)
        {
            t += Time.unscaledDeltaTime;
            float u = ease.Evaluate(Mathf.Clamp01(t / moveSeconds));
            cam.position = Vector3.Lerp(p0, p1, u);
            cameraRotation = Quaternion.Slerp(r0, r1, u);
            yield return null;
        }
        moveCo = null;
        cam.position = p1;
        cameraRotation = r1;
    }

    private void LateUpdate()
    {
        if (moveCo!=null)
        {
            cam.rotation = cameraRotation;
        } else
        {
            cam.rotation = cameraRotation * Quaternion.Euler(joystickVec);
        }
        
    }

    public void JoystickControl(Vector2 vec)
    {
        joystickVec = new Vector3(-vec.y * 0.3f, vec.x * 0.2f);
    }
}
