using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRigController : MonoBehaviour
{
    public Transform cam;                 // 비우면 Camera.main 사용
    public Transform outsideAnchor;       // 바깥 뷰 기준점
    public Transform internalAnchor;      // 내부 뷰 기준점
    public float moveSeconds = 0;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    Coroutine moveCo;

    public void GoOutside()  => MoveTo(outsideAnchor);
    public void GoInside() => MoveTo(internalAnchor);

    void MoveTo(Transform anchor)
    {
        if (anchor == null) return;
        if (moveCo != null) StopCoroutine(moveCo);
        var c = cam != null ? cam : Camera.main.transform;
        moveCo = StartCoroutine(Move(c, anchor));
    }

    IEnumerator Move(Transform mover, Transform target)
    {
        Vector3 p0 = mover.position; Quaternion r0 = mover.rotation;
        Vector3 p1 = target.position; Quaternion r1 = target.rotation;
        float t = 0f;
        while (t < moveSeconds)
        {
            t += Time.unscaledDeltaTime;
            float u = ease.Evaluate(Mathf.Clamp01(t / moveSeconds));
            mover.position = Vector3.Lerp(p0, p1, u);
            mover.rotation = Quaternion.Slerp(r0, r1, u);
            yield return null;
        }
        mover.SetPositionAndRotation(p1, r1);
    }
}
