using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu: profile from login, coins, Quick/Team flows, quiz pick + loading overlay to Gameplay.
/// Attach to Canvas; references optional (resolved by hierarchy names).
/// </summary>
[DisallowMultipleComponent]
public class MenuSceneController : MonoBehaviour
{
    const char TeamA = 'A';

    [Header("Scenes")]
    [SerializeField] string gameplaySceneName = "Gameplay";
    [SerializeField] string loginSceneName = "Login";
    [SerializeField] float loadingDelaySeconds = 0.65f;

    [Header("Optional UI refs")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject quizSelectionPanel;
    [SerializeField] GameObject teamSelectionPanel;
    [SerializeField] TMP_Text studentNameText;
    [SerializeField] TMP_Text studentRollText;
    [SerializeField] TMP_Text studentClassText;
    [SerializeField] TMP_Text coinsText;
    [SerializeField] Button quickPlayButton;
    [SerializeField] Button teamPlayButton;
    [SerializeField] Button teamContinueButton;
    [SerializeField] Button quizBackButton;
    [SerializeField] Button teamBackButton;
    [SerializeField] Button logoutButton;

    [Header("Runtime dialogs (empty = auto-build)")]
    [SerializeField] GameObject messageDialogRoot;
    [SerializeField] TMP_Text messageDialogText;
    [SerializeField] Button messageDialogOkButton;

    GameObject _loadingRoot;
    TMP_Text _loadingLabel;
    readonly List<Toggle> _teamTogglesOrdered = new();
    static Sprite s_whiteSprite;

    void Awake()
    {
        ResolveReferences();
        EnsureMessageDialog();
        WireNavigationButtons();
        WireQuizCategoryButtons();
        ApplyInitialPanelState();
    }

    void Start()
    {
        RefreshProfileUi();
    }

    void OnDestroy()
    {
        if (quickPlayButton != null) quickPlayButton.onClick.RemoveAllListeners();
        if (teamPlayButton != null) teamPlayButton.onClick.RemoveAllListeners();
        if (teamContinueButton != null) teamContinueButton.onClick.RemoveAllListeners();
        if (quizBackButton != null) quizBackButton.onClick.RemoveAllListeners();
        if (teamBackButton != null) teamBackButton.onClick.RemoveAllListeners();
        if (logoutButton != null) logoutButton.onClick.RemoveAllListeners();
        if (messageDialogOkButton != null) messageDialogOkButton.onClick.RemoveAllListeners();
    }

    void ResolveReferences()
    {
        if (mainMenuPanel == null) mainMenuPanel = transform.Find("Main Menu")?.gameObject;
        if (quizSelectionPanel == null) quizSelectionPanel = transform.Find("Quiz Selection")?.gameObject;
        if (teamSelectionPanel == null) teamSelectionPanel = transform.Find("Team Selection")?.gameObject;

        if (studentNameText == null)
            studentNameText = transform.Find("Main Menu/Top Bar/Student Name")?.GetComponent<TMP_Text>();
        if (studentRollText == null)
            studentRollText = transform.Find("Main Menu/Top Bar/Student Roll Number")?.GetComponent<TMP_Text>();
        if (studentClassText == null)
            studentClassText = transform.Find("Main Menu/Top Bar/Student Class")?.GetComponent<TMP_Text>();
        if (coinsText == null)
            coinsText = transform.Find("Main Menu/Top Bar/Coins Earn")?.GetComponent<TMP_Text>();

        if (quickPlayButton == null)
            quickPlayButton = transform.Find("Main Menu/Lower Panel/QuicPlay Button")?.GetComponent<Button>();
        if (teamPlayButton == null)
            teamPlayButton = transform.Find("Main Menu/Lower Panel/Team Play Button")?.GetComponent<Button>();

        if (teamContinueButton == null)
            teamContinueButton = transform.Find("Team Selection/Continue Button")?.GetComponent<Button>();
        if (quizBackButton == null)
            quizBackButton = transform.Find("Quiz Selection/Back Button")?.GetComponent<Button>();
        if (teamBackButton == null)
            teamBackButton = transform.Find("Team Selection/Back Button")?.GetComponent<Button>();
        if (logoutButton == null)
            logoutButton = transform.Find("Main Menu/Top Bar/Logout Button")?.GetComponent<Button>();

        CollectTeamToggles();
    }

    void CollectTeamToggles()
    {
        _teamTogglesOrdered.Clear();
        var panel = transform.Find("Team Selection/Choose TEAM TYPE Panel/Button Panel");
        if (panel == null || panel.childCount < 4) return;
        for (var i = 0; i < 4; i++)
        {
            var tgl = panel.GetChild(i).GetComponentInChildren<Toggle>(true);
            if (tgl != null) _teamTogglesOrdered.Add(tgl);
        }
    }

    void ApplyInitialPanelState()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (teamSelectionPanel != null) teamSelectionPanel.SetActive(false);
        if (quizSelectionPanel != null) quizSelectionPanel.SetActive(false);
    }

    void WireNavigationButtons()
    {
        if (quickPlayButton != null)
            quickPlayButton.onClick.AddListener(OnQuickPlay);
        if (teamPlayButton != null)
            teamPlayButton.onClick.AddListener(OnTeamPlay);
        if (teamContinueButton != null)
            teamContinueButton.onClick.AddListener(OnTeamContinue);
        if (quizBackButton != null)
            quizBackButton.onClick.AddListener(ShowMainMenu);
        if (teamBackButton != null)
            teamBackButton.onClick.AddListener(ShowMainMenu);
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogout);
    }

    void OnLogout()
    {
        PlayerPrefs.DeleteKey(LoginSceneController.PrefsStudentNameKey);
        PlayerPrefs.DeleteKey(LoginSceneController.PrefsRollNumberKey);
        PlayerPrefs.DeleteKey(StudentCredentials.PrefsSelectedQuizKey);
        PlayerPrefs.DeleteKey(StudentCredentials.PrefsSelectedTeamsKey);
        PlayerPrefs.DeleteKey(StudentCredentials.PrefsViaTeamPlayKey);
        PlayerPrefs.Save();
        if (!string.IsNullOrEmpty(loginSceneName))
            SceneManager.LoadScene(loginSceneName);
    }

    void WireQuizCategoryButtons()
    {
        var panel = transform.Find("Quiz Selection/Choose Quiz TYPE Panel/Button Panel");
        if (panel == null) return;

        var map = new (string objectName, string quizId, string display)[]
        {
            ("Math Button", "Math", "Math"),
            ("Phyics Button", "Physics", "Physics"),
            ("Chemistry Button", "Chemistry", "Chemistry"),
            ("Biology Button", "Biology", "Biology"),
            ("Mixed Button", "Mixed", "Mixed"),
        };

        foreach (var entry in map)
        {
            var tr = panel.Find(entry.objectName);
            if (tr == null) continue;
            var btn = tr.GetComponent<Button>();
            if (btn == null) continue;
            var id = entry.quizId;
            var label = entry.display;
            btn.onClick.AddListener(() => OnQuizChosen(id, label));
        }
    }

    void RefreshProfileUi()
    {
        var name = StudentCredentials.GetSavedStudentName();
        var roll = StudentCredentials.GetSavedRollRaw();

        if (studentNameText != null)
            studentNameText.text = string.IsNullOrEmpty(name) ? "—" : name;

        if (studentRollText != null)
            studentRollText.text = string.IsNullOrEmpty(roll) ? "—" : roll.Trim();

        if (studentClassText != null)
            studentClassText.text = StudentCredentials.GetClassDisplayLine(roll);

        if (coinsText != null)
            coinsText.text = StudentCredentials.GetCoins().ToString();
    }

    void OnQuickPlay()
    {
        PlayerPrefs.SetInt(StudentCredentials.PrefsViaTeamPlayKey, 0);
        PlayerPrefs.Save();
        ShowQuizSelectionOnly();
    }

    void OnTeamPlay()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (quizSelectionPanel != null) quizSelectionPanel.SetActive(false);
        if (teamSelectionPanel != null) teamSelectionPanel.SetActive(true);
    }

    void OnTeamContinue()
    {
        var selected = GetSelectedTeamLetters();
        if (selected.Count < 2)
        {
            ShowMessageDialog("Please select at least two teams (A, B, C, or D) before continuing.");
            return;
        }

        var encoded = string.Join(",", selected.OrderBy(c => c));
        PlayerPrefs.SetString(StudentCredentials.PrefsSelectedTeamsKey, encoded);
        PlayerPrefs.SetInt(StudentCredentials.PrefsViaTeamPlayKey, 1);
        PlayerPrefs.Save();

        ShowQuizSelectionOnly();
    }

    List<char> GetSelectedTeamLetters()
    {
        var list = new List<char>();
        for (var i = 0; i < _teamTogglesOrdered.Count; i++)
        {
            if (_teamTogglesOrdered[i] != null && _teamTogglesOrdered[i].isOn)
                list.Add((char)(TeamA + i));
        }
        return list;
    }

    void ShowQuizSelectionOnly()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (teamSelectionPanel != null) teamSelectionPanel.SetActive(false);
        if (quizSelectionPanel != null) quizSelectionPanel.SetActive(true);
    }

    void ShowMainMenu()
    {
        if (quizSelectionPanel != null) quizSelectionPanel.SetActive(false);
        if (teamSelectionPanel != null) teamSelectionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    void OnQuizChosen(string quizId, string displayName)
    {
        PlayerPrefs.SetString(StudentCredentials.PrefsSelectedQuizKey, quizId);
        PlayerPrefs.Save();
        StartCoroutine(LoadGameplayRoutine(displayName));
    }

    IEnumerator LoadGameplayRoutine(string quizDisplayName)
    {
        EnsureLoadingOverlay();
        if (_loadingRoot != null) _loadingRoot.SetActive(true);
        if (_loadingLabel != null)
            _loadingLabel.text = $"{quizDisplayName} quiz — please wait…";

        yield return new WaitForSecondsRealtime(loadingDelaySeconds);

        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            if (_loadingRoot != null) _loadingRoot.SetActive(false);
            ShowMessageDialog("Gameplay scene name is not set on MenuSceneController.");
            yield break;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    void EnsureLoadingOverlay()
    {
        if (_loadingRoot != null) return;

        var white = GetWhiteSprite();
        var overlay = new GameObject("LoadingOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        var oc = overlay.GetComponent<Canvas>();
        oc.overrideSorting = true;
        oc.sortingOrder = 8000;

        var ort = overlay.GetComponent<RectTransform>();
        StretchFull(ort);

        var dim = new GameObject("Blur", typeof(Image));
        dim.transform.SetParent(overlay.transform, false);
        var dimImg = dim.GetComponent<Image>();
        dimImg.sprite = white;
        dimImg.color = new Color(0.04f, 0.02f, 0.1f, 0.88f);
        dimImg.raycastTarget = true;
        StretchFull(dim.GetComponent<RectTransform>());

        var glass = new GameObject("Panel", typeof(Image));
        glass.transform.SetParent(overlay.transform, false);
        var gImg = glass.GetComponent<Image>();
        gImg.sprite = white;
        gImg.color = new Color(0.12f, 0.08f, 0.2f, 0.95f);
        var grt = glass.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.5f, 0.5f);
        grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(560f, 160f);
        grt.anchoredPosition = Vector2.zero;
        glass.AddComponent<Outline>().effectColor = new Color(0.85f, 0.7f, 0.25f);

        var txtGo = new GameObject("Label", typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(glass.transform, false);
        _loadingLabel = txtGo.GetComponent<TextMeshProUGUI>();
        _loadingLabel.fontSize = 24f;
        _loadingLabel.alignment = TextAlignmentOptions.Center;
        _loadingLabel.color = Color.white;
        _loadingLabel.textWrappingMode = TextWrappingModes.Normal;
        StretchFull(_loadingLabel.rectTransform);

        _loadingRoot = overlay;
        overlay.SetActive(false);
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Sprite GetWhiteSprite()
    {
        if (s_whiteSprite == null)
            s_whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return s_whiteSprite;
    }

    void EnsureMessageDialog()
    {
        if (messageDialogRoot != null && messageDialogText != null && messageDialogOkButton != null)
        {
            messageDialogOkButton.onClick.AddListener(HideMessageDialog);
            return;
        }

        var white = GetWhiteSprite();
        var root = new GameObject("MenuMessageDialog", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var c = root.GetComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 9000;
        StretchFull(root.GetComponent<RectTransform>());

        var dim = new GameObject("Dim", typeof(Image));
        dim.transform.SetParent(root.transform, false);
        var dimImg = dim.GetComponent<Image>();
        dimImg.sprite = white;
        dimImg.color = new Color(0.02f, 0f, 0.08f, 0.78f);
        dimImg.raycastTarget = true;
        StretchFull(dim.GetComponent<RectTransform>());

        var panel = new GameObject("Box", typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var pImg = panel.GetComponent<Image>();
        pImg.sprite = white;
        pImg.color = new Color(0.2f, 0.12f, 0.3f, 1f);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(480f, 220f);
        prt.anchoredPosition = Vector2.zero;
        panel.AddComponent<Outline>().effectColor = new Color(0.9f, 0.75f, 0.3f);

        var msgGo = new GameObject("Msg", typeof(TextMeshProUGUI));
        msgGo.transform.SetParent(panel.transform, false);
        messageDialogText = msgGo.GetComponent<TextMeshProUGUI>();
        messageDialogText.fontSize = 22f;
        messageDialogText.alignment = TextAlignmentOptions.Center;
        messageDialogText.color = Color.white;
        messageDialogText.textWrappingMode = TextWrappingModes.Normal;
        var mrt = messageDialogText.rectTransform;
        mrt.anchorMin = new Vector2(0.08f, 0.28f);
        mrt.anchorMax = new Vector2(0.92f, 0.88f);
        mrt.offsetMin = mrt.offsetMax = Vector2.zero;

        var okGo = new GameObject("OK", typeof(Image), typeof(Button));
        okGo.transform.SetParent(panel.transform, false);
        var okImg = okGo.GetComponent<Image>();
        okImg.sprite = white;
        okImg.color = new Color(0.45f, 0.3f, 0.65f);
        var okRt = okGo.GetComponent<RectTransform>();
        okRt.anchorMin = new Vector2(0.35f, 0.08f);
        okRt.anchorMax = new Vector2(0.65f, 0.22f);
        okRt.offsetMin = okRt.offsetMax = Vector2.zero;
        messageDialogOkButton = okGo.GetComponent<Button>();

        var okTxtGo = new GameObject("Txt", typeof(TextMeshProUGUI));
        okTxtGo.transform.SetParent(okGo.transform, false);
        var okTxt = okTxtGo.GetComponent<TextMeshProUGUI>();
        okTxt.text = "OK";
        okTxt.fontSize = 22f;
        okTxt.alignment = TextAlignmentOptions.Center;
        okTxt.color = Color.white;
        okTxt.fontStyle = FontStyles.Bold;
        StretchFull(okTxt.rectTransform);

        messageDialogRoot = root;
        root.SetActive(false);
        messageDialogOkButton.onClick.AddListener(HideMessageDialog);
    }

    void ShowMessageDialog(string msg)
    {
        if (messageDialogText != null) messageDialogText.text = msg;
        if (messageDialogRoot != null) messageDialogRoot.SetActive(true);
    }

    void HideMessageDialog()
    {
        if (messageDialogRoot != null) messageDialogRoot.SetActive(false);
    }
}
