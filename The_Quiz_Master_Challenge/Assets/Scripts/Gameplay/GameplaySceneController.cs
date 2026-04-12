using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gameplay: teams from menu prefs, category label, timer, hardcoded questions with typewriter,
/// option hover/answer colours, time-up panel, back to menu.
/// </summary>
[DisallowMultipleComponent]
public class GameplaySceneController : MonoBehaviour
{
    static Sprite s_whiteSprite;

    [Header("Scenes")]
    [SerializeField] string menuSceneName = "Menu";

    [Header("Timer")]
    [SerializeField] float questionTimeSeconds = 30f;
    [SerializeField] bool startTimerAfterIntroTypewriter = true;

    [Header("Typewriter")]
    [SerializeField] TypewriterStepMode questionTypewriterMode = TypewriterStepMode.Words;
    [SerializeField] float questionStepDelay = 0.18f;
    [SerializeField] TypewriterStepMode optionTypewriterMode = TypewriterStepMode.Characters;
    [SerializeField] float optionStepDelay = 0.07f;
    [Tooltip("If 0–4, always use that slot from the category pool (debug). -1 = random each round.")]
    [SerializeField] int forceQuestionSlot = -1;

    QuizQuestionData _activeQuestion;

    [Header("Colours")]
    [SerializeField] Color optionHoverHighlight = new(1f, 0.92f, 0.35f, 1f);
    [SerializeField] Color optionCorrectTint = new(0.35f, 0.88f, 0.45f, 1f);
    [SerializeField] Color optionWrongTint = new(0.95f, 0.32f, 0.32f, 1f);

    [Header("Optional refs (auto-resolve when empty)")]
    [SerializeField] Transform teamsDetailsRoot;
    [SerializeField] TMP_Text categoryLabel;
    [SerializeField] TMP_Text timerLabel;
    [SerializeField] TMP_Text questionText;
    [SerializeField] Button[] optionButtons = new Button[4];
    [SerializeField] TMP_Text[] optionLabels = new TMP_Text[4];
    [SerializeField] RawImage[] optionImages = new RawImage[4];
    [SerializeField] Button backButton;

    GameObject _timeUpRoot;
    TMP_Text _timeUpMessage;
    Button _timeUpMenuButton;

    Color[] _optionBaseImageColors = new Color[4];
    bool _roundEnded;
    bool _introComplete;
    Coroutine _timerCoroutine;

    void Awake()
    {
        ResolveReferences();
        WireBackButton();
        ApplyTeamBarsVisibility();
        FillCategoryLabel();
    }

    void Start()
    {
        var quizId = PlayerPrefs.GetString(StudentCredentials.PrefsSelectedQuizKey, "Math");
        _activeQuestion = QuizContent.GetRandomForCategory(quizId, forceQuestionSlot);
        if (!startTimerAfterIntroTypewriter)
            _timerCoroutine = StartCoroutine(TimerRoutine());
        StartCoroutine(IntroTypewriterRoutine());
    }

    void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(GoToMenu);
        if (_timeUpMenuButton != null) _timeUpMenuButton.onClick.RemoveListener(GoToMenu);
    }

    void ResolveReferences()
    {
        if (teamsDetailsRoot == null)
        {
            var t = transform.Find("Main/TEAMS DETAILS");
            if (t != null) teamsDetailsRoot = t;
        }

        if (categoryLabel == null)
            categoryLabel = transform.Find("Main/Quiz Category/Quiz Category Text")?.GetComponent<TMP_Text>();

        if (timerLabel == null)
            timerLabel = transform.Find("Main/TIME/TIMER TEXT")?.GetComponent<TMP_Text>();

        if (questionText == null)
            questionText = transform.Find("Main/Question Bar/Question Text")?.GetComponent<TMP_Text>();

        if (backButton == null)
            backButton = transform.Find("Main/Back Button")?.GetComponent<Button>();

        var optionRoot = transform.Find("Main/OPTION BUTTONS");
        if (optionRoot != null)
        {
            var names = new[] { "Option A", "Option B", "Option C", "Option D" };
            for (var i = 0; i < 4; i++)
            {
                if (optionButtons[i] != null && optionLabels[i] != null && optionImages[i] != null) continue;
                var tr = optionRoot.Find(names[i]);
                if (tr == null) continue;
                if (optionButtons[i] == null) optionButtons[i] = tr.GetComponent<Button>();
                if (optionLabels[i] == null)
                    optionLabels[i] = tr.Find("OPTION TEXT")?.GetComponent<TMP_Text>();
                if (optionImages[i] == null) optionImages[i] = tr.GetComponent<RawImage>();
            }
        }
    }

    void WireBackButton()
    {
        if (backButton != null)
            backButton.onClick.AddListener(GoToMenu);
    }

    void ApplyTeamBarsVisibility()
    {
        if (teamsDetailsRoot == null) return;

        var viaTeam = PlayerPrefs.GetInt(StudentCredentials.PrefsViaTeamPlayKey, 0) == 1;
        var selected = ParseTeamLetters(PlayerPrefs.GetString(StudentCredentials.PrefsSelectedTeamsKey, string.Empty));

        for (var i = 0; i < 4 && i < teamsDetailsRoot.childCount; i++)
        {
            var show = !viaTeam || selected.Count == 0 || selected.Contains((char)('A' + i));
            teamsDetailsRoot.GetChild(i).gameObject.SetActive(show);
        }
    }

    static HashSet<char> ParseTeamLetters(string raw)
    {
        var set = new HashSet<char>();
        if (string.IsNullOrEmpty(raw)) return set;
        foreach (var part in raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim();
            if (t.Length != 1) continue;
            var c = char.ToUpperInvariant(t[0]);
            if (c is >= 'A' and <= 'D') set.Add(c);
        }
        return set;
    }

    void FillCategoryLabel()
    {
        if (categoryLabel == null) return;
        var id = PlayerPrefs.GetString(StudentCredentials.PrefsSelectedQuizKey, "Quiz");
        categoryLabel.text = FormatCategoryTitle(id);
    }

    static string FormatCategoryTitle(string id)
    {
        if (string.IsNullOrEmpty(id)) return "Quiz";
        return id switch
        {
            "Math" => "Mathematics",
            "Physics" => "Physics",
            "Chemistry" => "Chemistry",
            "Biology" => "Biology",
            "Mixed" => "Mixed Science",
            _ => id
        };
    }

    IEnumerator IntroTypewriterRoutine()
    {
        PrepareOptionsForRound();
        var data = GetQuestionData();
        if (questionText != null) questionText.text = string.Empty;
        for (var i = 0; i < 4; i++)
        {
            if (optionLabels[i] != null) optionLabels[i].text = string.Empty;
            if (optionButtons[i] != null) optionButtons[i].interactable = false;
        }

        if (questionText != null)
            yield return TypewriterTMP.Animate(questionText, data.Question, questionTypewriterMode, questionStepDelay);

        for (var i = 0; i < 4; i++)
        {
            if (optionLabels[i] == null) continue;
            var line = $"{(char)('A' + i)}: {data.Options[i]}";
            yield return TypewriterTMP.Animate(optionLabels[i], line, optionTypewriterMode, optionStepDelay);
        }

        for (var i = 0; i < 4; i++)
        {
            if (optionButtons[i] != null) optionButtons[i].interactable = !_roundEnded;
        }

        _introComplete = true;

        if (startTimerAfterIntroTypewriter && !_roundEnded)
            _timerCoroutine = StartCoroutine(TimerRoutine());
    }

    QuizQuestionData GetQuestionData()
    {
        return _activeQuestion ?? QuizContent.FallbackQuestion;
    }

    void PrepareOptionsForRound()
    {
        var data = GetQuestionData();
        for (var i = 0; i < 4; i++)
        {
            if (optionButtons[i] == null) continue;
            var btn = optionButtons[i];
            var img = optionImages[i];
            if (img != null) _optionBaseImageColors[i] = img.color;

            btn.onClick.RemoveAllListeners();
            var captured = i;
            btn.onClick.AddListener(() => OnOptionClicked(captured, data.CorrectOptionIndex));

            var colors = btn.colors;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            colors.normalColor = Color.white;
            colors.highlightedColor = optionHoverHighlight;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.55f);
            btn.colors = colors;
            btn.transition = Selectable.Transition.ColorTint;
        }
    }

    IEnumerator TimerRoutine()
    {
        var remaining = questionTimeSeconds;
        while (remaining > 0f && !_roundEnded)
        {
            if (timerLabel != null)
                timerLabel.text = Mathf.CeilToInt(remaining).ToString();
            yield return null;
            remaining -= Time.unscaledDeltaTime;
        }

        if (_roundEnded) yield break;

        if (timerLabel != null)
            timerLabel.text = "0";

        OnTimeUp();
    }

    void OnTimeUp()
    {
        if (_roundEnded) return;
        _roundEnded = true;
        StopTimerCoroutine();
        SetAllOptionsInteractable(false);
        EnsureTimeUpPanel();
        if (_timeUpMessage != null) _timeUpMessage.text = "TIME'S UP!";
        if (_timeUpRoot != null) _timeUpRoot.SetActive(true);
    }

    void StopTimerCoroutine()
    {
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
    }

    void OnOptionClicked(int index, int correctIndex)
    {
        if (_roundEnded || !_introComplete) return;
        _roundEnded = true;
        StopTimerCoroutine();
        SetAllOptionsInteractable(false);

        var correct = index == correctIndex;
        for (var i = 0; i < 4; i++)
        {
            if (optionImages[i] == null) continue;
            if (i == correctIndex)
                optionImages[i].color = optionCorrectTint;
            else if (i == index && !correct)
                optionImages[i].color = optionWrongTint;
            else
                optionImages[i].color = _optionBaseImageColors[i];
        }
    }

    void SetAllOptionsInteractable(bool value)
    {
        foreach (var b in optionButtons)
        {
            if (b != null) b.interactable = value;
        }
    }

    void EnsureTimeUpPanel()
    {
        if (_timeUpRoot != null) return;

        var white = GetWhiteSprite();
        var root = new GameObject("TimeUpOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 12000;
        StretchFull(root.GetComponent<RectTransform>());

        var dim = new GameObject("Dim", typeof(Image));
        dim.transform.SetParent(root.transform, false);
        var dimImg = dim.GetComponent<Image>();
        dimImg.sprite = white;
        dimImg.color = new Color(0.05f, 0.02f, 0.12f, 0.85f);
        dimImg.raycastTarget = true;
        StretchFull(dim.GetComponent<RectTransform>());

        var panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var pImg = panel.GetComponent<Image>();
        pImg.sprite = white;
        pImg.color = new Color(0.16f, 0.09f, 0.26f, 1f);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(540f, 320f);
        prt.anchoredPosition = Vector2.zero;
        panel.AddComponent<Outline>().effectColor = new Color(0.88f, 0.72f, 0.22f, 1f);

        var titleGo = new GameObject("Title", typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panel.transform, false);
        _timeUpMessage = titleGo.GetComponent<TextMeshProUGUI>();
        _timeUpMessage.text = "TIME'S UP!";
        _timeUpMessage.fontSize = 42f;
        _timeUpMessage.alignment = TextAlignmentOptions.Center;
        _timeUpMessage.color = new Color(1f, 0.85f, 0.35f);
        _timeUpMessage.fontStyle = FontStyles.Bold;
        var trt = _timeUpMessage.rectTransform;
        trt.anchorMin = new Vector2(0.08f, 0.52f);
        trt.anchorMax = new Vector2(0.92f, 0.9f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var subGo = new GameObject("Sub", typeof(TextMeshProUGUI));
        subGo.transform.SetParent(panel.transform, false);
        var sub = subGo.GetComponent<TextMeshProUGUI>();
        sub.text = "The round has ended.";
        sub.fontSize = 22f;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = Color.white;
        var srt = sub.rectTransform;
        srt.anchorMin = new Vector2(0.1f, 0.35f);
        srt.anchorMax = new Vector2(0.9f, 0.5f);
        srt.offsetMin = srt.offsetMax = Vector2.zero;

        var btnGo = new GameObject("Menu Button", typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panel.transform, false);
        var bImg = btnGo.GetComponent<Image>();
        bImg.sprite = white;
        bImg.color = new Color(0.42f, 0.28f, 0.62f, 1f);
        var brt = btnGo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.28f, 0.1f);
        brt.anchorMax = new Vector2(0.72f, 0.26f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        _timeUpMenuButton = btnGo.GetComponent<Button>();

        var lblGo = new GameObject("Label", typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(btnGo.transform, false);
        var lbl = lblGo.GetComponent<TextMeshProUGUI>();
        lbl.text = "Back to menu";
        lbl.fontSize = 24f;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = Color.white;
        lbl.fontStyle = FontStyles.Bold;
        StretchFull(lbl.rectTransform);

        _timeUpMenuButton.onClick.AddListener(GoToMenu);
        _timeUpRoot = root;
        root.SetActive(false);
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

    void GoToMenu()
    {
        if (string.IsNullOrEmpty(menuSceneName)) return;
        SceneManager.LoadScene(menuSceneName);
    }
}
