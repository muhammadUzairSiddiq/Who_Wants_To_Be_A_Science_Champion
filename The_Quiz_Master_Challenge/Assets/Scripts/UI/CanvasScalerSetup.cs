using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Canvas Scaler (Web defaults)")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class CanvasScalerSetup : MonoBehaviour
{
    [SerializeField] Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] [Range(0f, 1f)] float matchWidthOrHeight = 0.5f;
    [SerializeField] bool overwriteExistingScaler;

    void Awake()
    {
        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        else if (!overwriteExistingScaler)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = matchWidthOrHeight;
    }
}
