using SocketIOClient;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class ToggleVisual : MonoBehaviour, IPointerClickHandler
{
    // Start is called before the first frame update
    [SerializeField] Image Background;
    [SerializeField] RectTransform Handle;
    Color OnColor = new Color32(65, 192, 83, 255);
    Color OffColor = new Color32(150, 150, 150, 255);
    float AnimTime = 0.15f;
    [SerializeField] Vector2 OnPos;
    [SerializeField] Vector2 OffPos;

    public UnityEvent<bool> onValueChanged;
    UnityEvent<bool> valueChangeAnimation;

    bool _isOn;
    public bool IsOn { get => _isOn;
        set { _isOn = value; onValueChanged.Invoke(_isOn); valueChangeAnimation.Invoke(_isOn); }
    }

    

    void Awake()
    {
        valueChangeAnimation = new UnityEvent<bool>();
        onValueChanged = new UnityEvent<bool>();
        valueChangeAnimation.AddListener(OnValueChanged);
    }


    public void SetIsOnWithoutNotify(bool a)
    {
        _isOn = a;
        valueChangeAnimation.Invoke(_isOn);

    }

    // Update is called once per frame

    Coroutine toggleAnimation;
    void OnValueChanged(bool isOn)
    {
        if (!gameObject.activeInHierarchy) return;

        if (toggleAnimation != null) StopCoroutine(toggleAnimation);
        toggleAnimation = StartCoroutine(DoToggleAnimate(
            Background, Handle, isOn, OnColor, OffColor, OnPos, OffPos, AnimTime
        ));
    }

    public void OnPointerClick(PointerEventData eventdata)
    {
        IsOn = !IsOn;
    }
    //if (fanAnimCo != null) StopCoroutine(fanAnimCo);
    //fanAnimCo = StartCoroutine(DoToggleAnimate(
    //    fanBackground, fanHandle, isOn, fanOnColor, fanOffColor, fanOnPos, fanOffPos, fanAnimTime
    //));

    IEnumerator DoToggleAnimate(Image bg, RectTransform knob, bool isOn,
                                Color onColor, Color offColor,
                                Vector2 onPos, Vector2 offPos, float secs)
    {
        if (!bg || !knob) yield break;
        float t = 0f;
        Color c0 = bg.color, c1 = isOn ? onColor : offColor;
        Vector2 p0 = knob.anchoredPosition, p1 = isOn ? onPos : offPos;
        while (t < secs)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / secs);
            bg.color = Color.Lerp(c0, c1, u);
            knob.anchoredPosition = Vector2.Lerp(p0, p1, u);
            yield return null;
        }
        bg.color = c1; knob.anchoredPosition = p1;
    }

    private void OnEnable()
    {
        Color c1 = IsOn ? OnColor : OffColor;
        Vector2 p1 = IsOn ? OnPos : OffPos;
        Background.color = c1; Handle.anchoredPosition = p1;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
