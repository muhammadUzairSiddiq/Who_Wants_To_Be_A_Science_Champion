using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginSceneController : MonoBehaviour
{
    public const string PrefsStudentNameKey = "QuizMaster_Login_StudentName";
    public const string PrefsRollNumberKey = "QuizMaster_Login_RollNumber";

    [Header("UI (optional — auto-filled if empty)")]
    [SerializeField] TMP_InputField nameInputField;
    [SerializeField] TMP_InputField rollInputField;
    [SerializeField] Button proceedButton;

    [Header("Navigation")]
    [SerializeField] string menuSceneName = "Menu";

    [Header("Teacher portal")]
    [SerializeField] Button teacherPortalButton;
    [SerializeField] string teachersLoginSceneName = "TeachersLogin";

    [Header("Name rules")]
    [SerializeField] int minNameLength = 3;
    [SerializeField] int maxNameLength = 64;
    [Tooltip("Letters (any language), spaces, apostrophe, period, hyphen.")]
    [SerializeField] string nameValidationPattern = @"^[\p{L}][\p{L}\s'.-]{2,}$";

    [Header("Roll number (configure in editor)")]
    [Tooltip("Primary format e.g. 2024-09-005")]
    [SerializeField] string strictRollRegex = @"^\d{4}-\d{2}-\d{3}$";
    [SerializeField] bool allowSimpleRollFormat = true;
    [Tooltip("Fallback: digits-only roll, e.g. 2409005 or 0915")]
    [SerializeField] string simpleRollRegex = @"^\d{3,15}$";

    [Header("Dialog (leave empty to build at runtime)")]
    [SerializeField] GameObject validationDialogRoot;
    [SerializeField] TMP_Text validationMessageText;
    [SerializeField] Button validationOkButton;

    Regex _nameRx;
    Regex _strictRollRx;
    Regex _simpleRollRx;

    void Awake()
    {
        TryCompileRegexes();
        AutoWireReferences();
        EnsureValidationDialog();
        if (proceedButton == null)
        {
            Debug.LogError("LoginSceneController: Proceed Button not found. Assign it or name the object \"Proceed Button\".");
            enabled = false;
            return;
        }
        proceedButton.onClick.AddListener(OnProceedClicked);
        if (validationOkButton != null)
            validationOkButton.onClick.AddListener(HideValidationDialog);
        WireTeacherPortalButton();
    }

    void Start()
    {
        LoadSavedCredentialsIntoFields();
    }

    void OnDestroy()
    {
        if (proceedButton != null)
            proceedButton.onClick.RemoveListener(OnProceedClicked);
        if (validationOkButton != null)
            validationOkButton.onClick.RemoveListener(HideValidationDialog);
        if (teacherPortalButton != null)
            teacherPortalButton.onClick.RemoveListener(OnTeacherPortalClicked);
    }

    void TryCompileRegexes()
    {
        try { _nameRx = new Regex(nameValidationPattern, RegexOptions.CultureInvariant); }
        catch { _nameRx = null; Debug.LogWarning("LoginSceneController: invalid nameValidationPattern."); }

        try { _strictRollRx = new Regex(strictRollRegex, RegexOptions.CultureInvariant); }
        catch { _strictRollRx = null; Debug.LogWarning("LoginSceneController: invalid strictRollRegex."); }

        try { _simpleRollRx = new Regex(simpleRollRegex, RegexOptions.CultureInvariant); }
        catch { _simpleRollRx = null; Debug.LogWarning("LoginSceneController: invalid simpleRollRegex."); }
    }

    void AutoWireReferences()
    {
        if (nameInputField == null)
        {
            var go = GameObject.Find("Name InputField");
            if (go != null) nameInputField = go.GetComponent<TMP_InputField>();
        }
        if (rollInputField == null)
        {
            var go = GameObject.Find("Roll Number  InputField");
            if (go != null) rollInputField = go.GetComponent<TMP_InputField>();
        }
        if (proceedButton == null)
        {
            var go = GameObject.Find("Proceed Button");
            if (go != null) proceedButton = go.GetComponent<Button>();
        }
        if (teacherPortalButton == null)
        {
            var go = GameObject.Find("Teacher portal Button");
            if (go != null) teacherPortalButton = go.GetComponent<Button>();
        }
    }

    void WireTeacherPortalButton()
    {
        if (teacherPortalButton != null)
            teacherPortalButton.onClick.AddListener(OnTeacherPortalClicked);
    }

    void OnTeacherPortalClicked()
    {
        if (string.IsNullOrEmpty(teachersLoginSceneName))
        {
            ShowValidationDialog("Teachers login scene name is not set on LoginSceneController.");
            return;
        }

        SceneManager.LoadScene(teachersLoginSceneName);
    }

    void LoadSavedCredentialsIntoFields()
    {
        if (nameInputField == null || rollInputField == null) return;

        var savedName = PlayerPrefs.GetString(PrefsStudentNameKey, string.Empty);
        var savedRoll = PlayerPrefs.GetString(PrefsRollNumberKey, string.Empty);
        if (!string.IsNullOrEmpty(savedName))
            nameInputField.text = savedName;
        if (!string.IsNullOrEmpty(savedRoll))
            rollInputField.text = savedRoll;
    }

    void OnProceedClicked()
    {
        var name = nameInputField != null ? nameInputField.text.Trim() : string.Empty;
        var roll = rollInputField != null ? rollInputField.text.Trim() : string.Empty;

        if (!TryValidateName(name, out var nameError))
        {
            ShowValidationDialog(nameError);
            return;
        }
        if (!TryValidateRoll(roll, out var rollError))
        {
            ShowValidationDialog(rollError);
            return;
        }

        PlayerPrefs.SetString(PrefsStudentNameKey, name);
        PlayerPrefs.SetString(PrefsRollNumberKey, roll);
        PlayerPrefs.Save();

        if (string.IsNullOrEmpty(menuSceneName))
        {
            ShowValidationDialog("Menu scene name is not set on LoginSceneController.");
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    bool TryValidateName(string name, out string error)
    {
        error = null;
        if (string.IsNullOrEmpty(name))
        {
            error = "Please enter your name.";
            return false;
        }
        if (name.Length < minNameLength)
        {
            error = $"Name must be at least {minNameLength} characters.";
            return false;
        }
        if (name.Length > maxNameLength)
        {
            error = $"Name must be at most {maxNameLength} characters.";
            return false;
        }
        if (_nameRx != null && !_nameRx.IsMatch(name))
        {
            error = "Name can only contain letters, spaces, and . ' - (and must start with a letter).";
            return false;
        }
        return true;
    }

    bool TryValidateRoll(string roll, out string error)
    {
        error = null;
        if (string.IsNullOrEmpty(roll))
        {
            error = "Please enter your roll number.";
            return false;
        }

        if (_strictRollRx != null && _strictRollRx.IsMatch(roll))
            return true;

        if (allowSimpleRollFormat && _simpleRollRx != null && _simpleRollRx.IsMatch(roll))
            return true;

        error = BuildRollFormatHint();
        return false;
    }

    string BuildRollFormatHint()
    {
        return "Roll number format is invalid.\n\n" +
               "Use the full format: Year-Class-Roll (example: 2024-09-005),\n" +
               "or a simple numeric roll (" + (simpleRollRegex ?? "") + ").\n\n" +
               "You can change the allowed patterns on the LoginSceneController in the Inspector.";
    }

    void EnsureValidationDialog()
    {
        if (validationDialogRoot != null && validationMessageText != null && validationOkButton != null)
            return;

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("LoginSceneController: No Canvas found — cannot create validation dialog.");
            return;
        }

        var white = RuntimeGeneratedUiStyle.WhiteFallbackSprite();

        var overlay = new GameObject("ValidationOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
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
        RuntimeGeneratedUiStyle.ApplyPanel(panel);
        var panelRt = panel.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(560f, 300f);
        panelRt.anchoredPosition = Vector2.zero;

        if (!RuntimeGeneratedUiStyle.UsePremiumChrome())
        {
            var outline = panelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.7f, 0.25f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        var titleGo = new GameObject("Title", typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panelGo.transform, false);
        var title = titleGo.GetComponent<TextMeshProUGUI>();
        title.text = "Check your details";
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
        RuntimeGeneratedUiStyle.ApplyButton(btnImg);
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
