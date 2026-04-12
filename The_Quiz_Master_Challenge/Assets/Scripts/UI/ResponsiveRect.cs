using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ResponsivePreset
{
    FullStretch,
    TopBar,
    BottomBar,
    LeftColumn,
    RightColumn,
    Center,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

[AddComponentMenu("UI/Responsive Rect")]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class ResponsiveRect : UIBehaviour
{
    [SerializeField] ResponsivePreset preset = ResponsivePreset.FullStretch;

    [SerializeField] float marginLeft;
    [SerializeField] float marginRight;
    [SerializeField] float marginTop;
    [SerializeField] float marginBottom;

    [SerializeField] float bandThickness = 120f;
    [SerializeField] Vector2 fixedSize = new Vector2(320f, 72f);

    [SerializeField] bool applyTmpAutoSize;
    [SerializeField] float tmpFontSizeMin = 12f;
    [SerializeField] float tmpFontSizeMax = 64f;

    bool _tmpConfigured;

    protected override void OnEnable()
    {
        base.OnEnable();
        ApplyLayout();
        ConfigureTmpOnce();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        ApplyLayout();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        ApplyLayout();
    }
#endif

    [ContextMenu("Apply Layout Now")]
    public void ApplyLayout()
    {
        var rt = transform as RectTransform;
        if (rt == null) return;

        switch (preset)
        {
            case ResponsivePreset.FullStretch:
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(marginLeft, marginBottom);
                rt.offsetMax = new Vector2(-marginRight, -marginTop);
                break;

            case ResponsivePreset.TopBar:
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2((marginLeft - marginRight) * 0.5f, -marginTop);
                rt.sizeDelta = new Vector2(-marginLeft - marginRight, bandThickness);
                break;

            case ResponsivePreset.BottomBar:
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2((marginLeft - marginRight) * 0.5f, marginBottom);
                rt.sizeDelta = new Vector2(-marginLeft - marginRight, bandThickness);
                break;

            case ResponsivePreset.LeftColumn:
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(marginLeft, (marginBottom - marginTop) * 0.5f);
                rt.sizeDelta = new Vector2(bandThickness, -marginTop - marginBottom);
                break;

            case ResponsivePreset.RightColumn:
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-marginRight, (marginBottom - marginTop) * 0.5f);
                rt.sizeDelta = new Vector2(bandThickness, -marginTop - marginBottom);
                break;

            case ResponsivePreset.Center:
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = fixedSize;
                break;

            case ResponsivePreset.TopLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(marginLeft, -marginTop);
                rt.sizeDelta = fixedSize;
                break;

            case ResponsivePreset.TopRight:
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-marginRight, -marginTop);
                rt.sizeDelta = fixedSize;
                break;

            case ResponsivePreset.BottomLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(marginLeft, marginBottom);
                rt.sizeDelta = fixedSize;
                break;

            case ResponsivePreset.BottomRight:
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-marginRight, marginBottom);
                rt.sizeDelta = fixedSize;
                break;
        }

        LayoutRebuilder.MarkLayoutForRebuild(rt);
    }

    void ConfigureTmpOnce()
    {
        if (!applyTmpAutoSize || _tmpConfigured) return;
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = tmpFontSizeMin;
            tmp.fontSizeMax = tmpFontSizeMax;
        }
        _tmpConfigured = true;
    }

    void Reset()
    {
        var rt = transform as RectTransform;
        if (rt != null && rt.parent is RectTransform)
            ApplyLayout();
    }
}
