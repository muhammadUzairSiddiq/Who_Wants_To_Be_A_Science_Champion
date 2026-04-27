using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Canvas))]
public class UIButtonClickFeedback : MonoBehaviour
{
    struct FeedbackTheme
    {
        public string[] messages;
        public Color color;
        public bool forceLarge;
    }

    [SerializeField] float punchScale = 1.14f;
    [SerializeField] float punchHalfDuration = 0.075f;
    [SerializeField] float smallPopupFontSize = 42f;
    [SerializeField] float largePopupFontSize = 72f;
    [Range(0f, 1f)]
    [SerializeField] float largeEffectChance = 0.35f;
    [SerializeField] Color popupColor = new Color(1f, 0.95f, 0.4f, 1f);
    [SerializeField] float popupLifetime = 0.85f;
    [SerializeField] float popupRisePixels = 120f;
    [SerializeField] Color flashColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] float flashDuration = 0.14f;

    static readonly string[] DefaultPhrases =
    {
        "Nice!", "Yes!", "Let's go!", "Tap!", "Got it!", "Onward!", "Cool!"
    };

    readonly Vector3[] _cornerScratch = new Vector3[4];

    Canvas _canvas;
    RectTransform _canvasRect;
    readonly Dictionary<Button, Coroutine> _punches = new Dictionary<Button, Coroutine>();
    readonly Dictionary<Button, UnityAction> _registeredActions = new Dictionary<Button, UnityAction>();

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvasRect = _canvas.transform as RectTransform;
    }

    void OnEnable() => RegisterNewButtonsInHierarchy();

    void OnDisable()
    {
        foreach (var kv in _registeredActions)
        {
            if (kv.Key != null)
                kv.Key.onClick.RemoveListener(kv.Value);
        }
        _registeredActions.Clear();
    }

    public void RegisterNewButtonsInHierarchy()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (_registeredActions.ContainsKey(button))
                continue;
            if (button.GetComponent<ButtonHoverTiltEffect>() == null)
                button.gameObject.AddComponent<ButtonHoverTiltEffect>();
            UnityAction action = () => OnButtonClicked(button);
            _registeredActions[button] = action;
            button.onClick.AddListener(action);
        }
    }

    void OnButtonClicked(Button button)
    {
        if (button == null) return;
        PlaySmartFeedback(button, null);
    }

    public void PlaySmartFeedback(Button button, string contextOverride)
    {
        if (button == null) return;
        if (_punches.TryGetValue(button, out var running) && running != null)
        {
            StopCoroutine(running);
            var h = button.GetComponent<ButtonHoverTiltEffect>();
            if (h != null) h.CaptureRestState();
            _punches[button] = null;
        }
        var punch = StartCoroutine(PunchRoutine(button));
        _punches[button] = punch;
        var theme = ResolveTheme(button, contextOverride);
        bool large = theme.forceLarge || UnityEngine.Random.value < largeEffectChance;
        StartCoroutine(PopupRoutine(button, large, theme));
        if (large || theme.forceLarge)
            StartCoroutine(FlashRoutine());
    }

    static bool NameHas(string name, string token) =>
        !string.IsNullOrEmpty(name) && name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

    FeedbackTheme ResolveTheme(Button button, string contextOverride)
    {
        var name = contextOverride ?? button.gameObject.name ?? string.Empty;

        if (NameHas(name, "quiz_correct") || NameHas(name, "correct"))
            return new FeedbackTheme
            {
                messages = new[] { "Great choice!", "Perfect!", "Brilliant!", "You got this!" },
                color = new Color(0.40f, 1.00f, 0.55f, 1f),
                forceLarge = true
            };
        if (NameHas(name, "quiz_wrong") || NameHas(name, "wrong"))
            return new FeedbackTheme
            {
                messages = new[] { "Almost there!", "Try again!", "Keep going!", "You can do this!" },
                color = new Color(1.00f, 0.78f, 0.35f, 1f),
                forceLarge = false
            };
        if (NameHas(name, "retry"))
            return new FeedbackTheme
            {
                messages = new[] { "Reset and rise!", "Let's go again!", "You are improving!" },
                color = new Color(0.58f, 0.92f, 1.00f, 1f),
                forceLarge = false
            };
        if (NameHas(name, "start"))
            return new FeedbackTheme
            {
                messages = new[] { "Strong start!", "You are ready!", "Let's win this!" },
                color = new Color(0.50f, 0.95f, 1.00f, 1f),
                forceLarge = true
            };
        if (NameHas(name, "mission") || NameHas(name, "chapter") || NameHas(name, "level") || NameHas(name, "play"))
            return new FeedbackTheme
            {
                messages = new[] { "Confidence up!", "Mission time!", "You got this!" },
                color = new Color(0.55f, 1.00f, 0.80f, 1f),
                forceLarge = false
            };
        if (NameHas(name, "back"))
            return new FeedbackTheme
            {
                messages = new[] { "No worries!", "Regroup and continue!", "You are in control!" },
                color = new Color(1.00f, 0.92f, 0.50f, 1f),
                forceLarge = false
            };

        return new FeedbackTheme { messages = DefaultPhrases, color = popupColor, forceLarge = false };
    }

    IEnumerator PunchRoutine(Button button)
    {
        var rt = button.transform as RectTransform;
        if (rt == null) yield break;
        var hover = button.GetComponent<ButtonHoverTiltEffect>();
        Vector3 baseline = hover != null ? hover.DesignScale : rt.localScale;
        float up = punchHalfDuration;
        float t = 0f;
        while (t < up)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / up);
            float s = Mathf.SmoothStep(1f, punchScale, k);
            rt.localScale = baseline * s;
            yield return null;
        }
        t = 0f;
        while (t < up)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / up);
            float s = Mathf.SmoothStep(punchScale, 1f, k);
            rt.localScale = baseline * s;
            yield return null;
        }
        rt.localScale = baseline;
        if (hover != null) hover.CaptureRestState();
        _punches[button] = null;
    }

    IEnumerator PopupRoutine(Button button, bool large, FeedbackTheme theme)
    {
        var target = button.transform as RectTransform;
        if (target == null || _canvasRect == null) yield break;

        var go = new GameObject("ClickPopup", typeof(RectTransform));
        go.transform.SetParent(_canvasRect, false);
        var popupRt = (RectTransform)go.transform;

        target.GetWorldCorners(_cornerScratch);
        Vector3 worldCenter = (_cornerScratch[0] + _cornerScratch[2]) * 0.5f;

        Camera cam = null;
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                RectTransformUtility.WorldToScreenPoint(cam, worldCenter),
                cam,
                out Vector2 localPoint))
            localPoint = _canvasRect.InverseTransformPoint(worldCenter);

        popupRt.anchorMin = popupRt.anchorMax = new Vector2(0.5f, 0.5f);
        popupRt.pivot = new Vector2(0.5f, 0.5f);
        popupRt.anchoredPosition = localPoint;
        popupRt.sizeDelta = new Vector2(520f, 140f);
        popupRt.localScale = Vector3.one * (large ? 0.35f : 0.5f);

        var text = go.AddComponent<TextMeshProUGUI>();
        var messages = (theme.messages != null && theme.messages.Length > 0) ? theme.messages : DefaultPhrases;
        text.text = messages[UnityEngine.Random.Range(0, messages.Length)];
        text.fontSize = large ? largePopupFontSize : smallPopupFontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 1f;
        text.fontSizeMax = large ? largePopupFontSize : smallPopupFontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = theme.color;
        text.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = large ? new Vector2(3f, -3f) : new Vector2(2f, -2f);

        float elapsed = 0f;
        Color c = text.color;
        while (elapsed < popupLifetime)
        {
            elapsed += Time.unscaledDeltaTime;
            float u = elapsed / popupLifetime;
            float scaleT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.2f));
            popupRt.localScale = Vector3.one * Mathf.Lerp(large ? 0.35f : 0.5f, 1f, scaleT) * (large ? 1.15f : 1f);
            c.a = Mathf.Lerp(1f, 0f, u * u);
            text.color = c;
            popupRt.anchoredPosition = localPoint + new Vector2(0f, popupRisePixels * u);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator FlashRoutine()
    {
        var go = new GameObject("ClickFlash", typeof(RectTransform));
        go.transform.SetParent(_canvasRect, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();
        var img = go.AddComponent<Image>();
        img.color = flashColor;
        img.raycastTarget = false;
        float t = 0f;
        Color c = flashColor;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            c.a = flashColor.a * (1f - t / flashDuration);
            img.color = c;
            yield return null;
        }
        Destroy(go);
    }
}
