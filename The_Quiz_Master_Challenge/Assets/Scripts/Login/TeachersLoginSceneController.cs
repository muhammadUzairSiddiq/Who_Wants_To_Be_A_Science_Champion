using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TeachersLoginSceneController : MonoBehaviour
{
    [Header("UI (optional — auto-filled if empty)")]
    [SerializeField] TMP_InputField usernameField;
    [SerializeField] TMP_InputField passwordField;
    [SerializeField] Button proceedButton;
    [SerializeField] Button studentPortalButton;

    [Header("Navigation")]
    [SerializeField] string loginSceneName = "Login";
    [SerializeField] string dashboardSceneName = "Dashboard";

    [Header("Credentials (case-insensitive)")]
    [SerializeField] string validUsername = "admin";
    [SerializeField] string validPassword = "admin";

    [Header("Dialog (leave empty to build at runtime)")]
    [SerializeField] GameObject validationDialogRoot;
    [SerializeField] TMP_Text validationMessageText;
    [SerializeField] Button validationOkButton;

    void Awake()
    {
        AutoWireReferences();
        EnsureValidationDialog();
        if (proceedButton == null)
        {
            Debug.LogError("TeachersLoginSceneController: Proceed Button not found. Assign it or name the object \"Proceed Button\".");
            enabled = false;
            return;
        }

        proceedButton.onClick.AddListener(OnProceedClicked);
        if (studentPortalButton != null)
            studentPortalButton.onClick.AddListener(OnStudentPortalClicked);
        if (validationOkButton != null)
            validationOkButton.onClick.AddListener(HideValidationDialog);
    }

    void OnDestroy()
    {
        if (proceedButton != null)
            proceedButton.onClick.RemoveListener(OnProceedClicked);
        if (studentPortalButton != null)
            studentPortalButton.onClick.RemoveListener(OnStudentPortalClicked);
        if (validationOkButton != null)
            validationOkButton.onClick.RemoveListener(HideValidationDialog);
    }

    void AutoWireReferences()
    {
        if (usernameField == null)
        {
            var go = GameObject.Find("Email InputField");
            if (go != null) usernameField = go.GetComponent<TMP_InputField>();
        }

        if (passwordField == null)
        {
            var go = GameObject.Find("Password  InputField");
            if (go == null) go = GameObject.Find("Password InputField");
            if (go != null) passwordField = go.GetComponent<TMP_InputField>();
        }

        if (proceedButton == null)
        {
            var go = GameObject.Find("Proceed Button");
            if (go != null) proceedButton = go.GetComponent<Button>();
        }

        if (studentPortalButton == null)
        {
            var go = GameObject.Find("Student portal Button");
            if (go != null) studentPortalButton = go.GetComponent<Button>();
        }
    }

    void OnStudentPortalClicked()
    {
        if (string.IsNullOrEmpty(loginSceneName))
        {
            ShowValidationDialog("Login scene name is not set on TeachersLoginSceneController.");
            return;
        }

        SceneManager.LoadScene(loginSceneName);
    }

    void OnProceedClicked()
    {
        var user = usernameField != null ? usernameField.text.Trim() : string.Empty;
        var pass = passwordField != null ? passwordField.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(user))
        {
            ShowValidationDialog("Please enter your username.");
            return;
        }

        if (string.IsNullOrEmpty(pass))
        {
            ShowValidationDialog("Please enter your password.");
            return;
        }

        if (!string.Equals(user, validUsername?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(pass, validPassword ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            ShowValidationDialog("Invalid username or password.");
            return;
        }

        if (string.IsNullOrEmpty(dashboardSceneName))
        {
            ShowValidationDialog("Dashboard scene name is not set on TeachersLoginSceneController.");
            return;
        }

        SceneManager.LoadScene(dashboardSceneName);
    }

    void EnsureValidationDialog()
    {
        if (validationDialogRoot != null && validationMessageText != null && validationOkButton != null)
            return;

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("TeachersLoginSceneController: No Canvas found — cannot create validation dialog.");
            return;
        }

        var white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);

        var overlay = new GameObject("TeacherValidationOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        overlay.transform.SetParent(canvas.transform, false);
        overlay.SetActive(false);

        var overlayCanvas = overlay.GetComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 5000;

        var overlayRt = overlay.GetComponent<RectTransform>();
        StretchFull(overlayRt);

        var dimGo = new GameObject("Dim", typeof(Image));
        dimGo.transform.SetParent(overlay.transform, false);
        var dim = dimGo.GetComponent<Image>();
        dim.sprite = white;
        dim.color = new Color(0.05f, 0.02f, 0.12f, 0.82f);
        dim.raycastTarget = true;
        StretchFull(dim.rectTransform);

        var panelGo = new GameObject("Panel", typeof(Image));
        panelGo.transform.SetParent(overlay.transform, false);
        var panel = panelGo.GetComponent<Image>();
        panel.sprite = white;
        panel.color = new Color(0.18f, 0.1f, 0.28f, 1f);
        var panelRt = panel.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(520f, 280f);
        panelRt.anchoredPosition = Vector2.zero;

        var outline = panelGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.7f, 0.25f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        var titleGo = new GameObject("Title", typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panelGo.transform, false);
        var title = titleGo.GetComponent<TextMeshProUGUI>();
        title.text = "Teacher portal";
        title.fontSize = 26f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.92f, 0.65f);
        title.fontStyle = FontStyles.Bold;
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.08f, 0.62f);
        titleRt.anchorMax = new Vector2(0.92f, 0.92f);
        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;

        var msgGo = new GameObject("Message", typeof(TextMeshProUGUI));
        msgGo.transform.SetParent(panelGo.transform, false);
        var msg = msgGo.GetComponent<TextMeshProUGUI>();
        msg.text = string.Empty;
        msg.fontSize = 20f;
        msg.alignment = TextAlignmentOptions.Center;
        msg.color = Color.white;
        msg.enableWordWrapping = true;
        var msgRt = msg.rectTransform;
        msgRt.anchorMin = new Vector2(0.08f, 0.28f);
        msgRt.anchorMax = new Vector2(0.92f, 0.58f);
        msgRt.offsetMin = msgRt.offsetMax = Vector2.zero;

        var btnGo = new GameObject("OK Button", typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panelGo.transform, false);
        var btnImg = btnGo.GetComponent<Image>();
        btnImg.sprite = white;
        btnImg.color = new Color(0.45f, 0.28f, 0.65f, 1f);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.35f, 0.08f);
        btnRt.anchorMax = new Vector2(0.65f, 0.22f);
        btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;

        var btn = btnGo.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.55f, 0.38f, 0.78f);
        colors.pressedColor = new Color(0.35f, 0.2f, 0.5f);
        btn.colors = colors;

        var labelGo = new GameObject("Label", typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(btnGo.transform, false);
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = "OK";
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        StretchFull(label.rectTransform);

        validationDialogRoot = overlay;
        validationMessageText = msg;
        validationOkButton = btn;
        canvas.GetComponent<UIButtonClickFeedback>()?.RegisterNewButtonsInHierarchy();
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void ShowValidationDialog(string message)
    {
        if (validationMessageText != null)
            validationMessageText.text = message;
        if (validationDialogRoot != null)
            validationDialogRoot.SetActive(true);
    }

    void HideValidationDialog()
    {
        if (validationDialogRoot != null)
            validationDialogRoot.SetActive(false);
    }
}
