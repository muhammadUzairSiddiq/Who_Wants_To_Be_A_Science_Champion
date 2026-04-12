using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ButtonHoverTiltEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float hoverScale = 1.07f;
    [SerializeField] float tiltDegreesZ = 4f;
    [SerializeField] float tiltDegreesY = 2f;
    [SerializeField] float smoothSeconds = 0.11f;

    RectTransform _rt;
    Vector3 _restScale;
    Quaternion _restLocalRot;
    Coroutine _animate;

    void Awake() => CacheRest();
    void OnEnable() => CacheRest();

    void CacheRest()
    {
        _rt = transform as RectTransform;
        if (_rt == null) return;
        _restScale = _rt.localScale;
        _restLocalRot = _rt.localRotation;
    }

    public void CaptureRestState() => CacheRest();

    void OnDisable()
    {
        if (_animate != null)
        {
            StopCoroutine(_animate);
            _animate = null;
        }
        if (_rt != null)
        {
            _rt.localScale = _restScale;
            _rt.localRotation = _restLocalRot;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => QueueAnim(true);
    public void OnPointerExit(PointerEventData eventData) => QueueAnim(false);

    void QueueAnim(bool hoverIn)
    {
        if (_rt == null) return;
        if (_animate != null) StopCoroutine(_animate);
        _animate = StartCoroutine(AnimateHover(hoverIn));
    }

    IEnumerator AnimateHover(bool hoverIn)
    {
        float sign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
        var hoverRot = _restLocalRot * Quaternion.Euler(0f, tiltDegreesY * sign, tiltDegreesZ * -sign);
        var endRot = hoverIn ? hoverRot : _restLocalRot;
        var endScale = hoverIn ? _restScale * hoverScale : _restScale;

        var startRot = _rt.localRotation;
        var startScale = _rt.localScale;
        float dur = Mathf.Max(0.01f, smoothSeconds);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            _rt.localRotation = Quaternion.SlerpUnclamped(startRot, endRot, k);
            _rt.localScale = Vector3.LerpUnclamped(startScale, endScale, k);
            yield return null;
        }

        _rt.localRotation = endRot;
        _rt.localScale = endScale;
        _animate = null;
    }
}
