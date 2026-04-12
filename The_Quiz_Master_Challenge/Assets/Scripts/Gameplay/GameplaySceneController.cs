using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gameplay: teams, category, timer, questions with typewriter, lifelines, continuous play until time up.
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

    [Header("Answer feedback")]
    [SerializeField] float pauseBeforeNextQuestionSeconds = 0.55f;

    [Header("Typewriter")]
    [SerializeField] TypewriterStepMode questionTypewriterMode = TypewriterStepMode.Words;
    [SerializeField] float questionStepDelay = 0.18f;
    [SerializeField] TypewriterStepMode optionTypewriterMode = TypewriterStepMode.Characters;
    [SerializeField] float optionStepDelay = 0.07f;
    [Tooltip("If 0–4, always use that slot from the category pool (debug). -1 = random each round.")]
    [SerializeField] int forceQuestionSlot = -1;

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
    [SerializeField] Button lifeline5050Button;
    [SerializeField] Button lifelineAudienceButton;
    [SerializeField] Button lifelinePhoneButton;

    QuizQuestionData _activeQuestion;
    string _quizCategoryId;

    GameObject _timeUpRoot;
    TMP_Text _timeUpMessage;
    Button _timeUpMenuButton;

    GameObject _lifelinePromptRoot;
    TMP_Text _lifelinePromptBody;
    Button _lifelinePromptOkButton;

    Color[] _optionBaseImageColors = new Color[4];
    bool _timeUpEnded;
    bool _introComplete;
    Coroutine _timerCoroutine;
    Coroutine _introCoroutine;
    Coroutine _advanceCoroutine;

    bool _fiftyUsedThisQuestion;
    bool _audienceUsedThisQuestion;
    bool _phoneUsedThisQuestion;

    void Awake()
    {
        ResolveReferences();
        WireBackButton();
        WireLifelineButtons();
        ApplyTeamBarsVisibility();
        FillCategoryLabel();
    }

    void Start()
    {
        _quizCategoryId = PlayerPrefs.GetString(StudentCredentials.PrefsSelectedQuizKey, "Math");
        _activeQuestion = QuizContent.GetRandomForCategory(_quizCategoryId, forceQuestionSlot);
        _introCoroutine = StartCoroutine(IntroTypewriterRoutine());
    }

    void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(GoToMenu);
        if (_timeUpMenuButton != null) _timeUpMenuButton.onClick.RemoveListener(GoToMenu);
        if (lifeline5050Button != null) lifeline5050Button.onClick.RemoveListener(OnLifeline5050);
        if (lifelineAudienceButton != null) lifelineAudienceButton.onClick.RemoveListener(OnLifelineAudience);
        if (lifelinePhoneButton != null) lifelinePhoneButton.onClick.RemoveListener(OnLifelinePhone);
        if (_lifelinePromptOkButton != null) _lifelinePromptOkButton.onClick.RemoveListener(HideLifelinePrompt);
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

        var lifeRoot = transform.Find("Main/LIFELINE BUTTONS");
        if (lifeRoot != null)
        {
            if (lifeline5050Button == null)
                lifeline5050Button = lifeRoot.Find("Lifeline 50 50 Button")?.GetComponent<Button>();
            if (lifelineAudienceButton == null)
                lifelineAudienceButton = lifeRoot.Find("Ask audience Button")?.GetComponent<Button>();
            if (lifelinePhoneButton == null)
                lifelinePhoneButton = lifeRoot.Find("Call A Friend Button")?.GetComponent<Button>();
        }
    }

    void WireBackButton()
    {
        if (backButton != null)
            backButton.onClick.AddListener(GoToMenu);
    }

    void WireLifelineButtons()
    {
        if (lifeline5050Button != null)
            lifeline5050Button.onClick.AddListener(OnLifeline5050);
        if (lifelineAudienceButton != null)
            lifelineAudienceButton.onClick.AddListener(OnLifelineAudience);
        if (lifelinePhoneButton != null)
            lifelinePhoneButton.onClick.AddListener(OnLifelinePhone);
    }

    void ApplyTeamBarsVisibility()
    {
        if (teamsDetailsRoot == null) return;

        var viaTeam = PlayerPrefs.GetInt(StudentCredentials.PrefsViaTeamPlayKey, 0) == 1;
        var selected = ParseTeamLetters(PlayerPrefs.GetString(StudentCredentials.PrefsSelectedTeamsKey, string.Empty));

        for (var i = 0; i < 4 && i < teamsDetailsRoot.childCount; i++)
        {
            bool show;
            if (!viaTeam)
                show = false;
            else
                show = selected.Count == 0 || selected.Contains((char)('A' + i));
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
        _introComplete = false;
        SetLifelineButtonsInteractable(false);
        StopTimerCoroutine();

        if (!startTimerAfterIntroTypewriter && !_timeUpEnded)
            _timerCoroutine = StartCoroutine(TimerRoutine());

        for (var i = 0; i < 4; i++)
        {
            if (optionLabels[i] != null)
                optionLabels[i].gameObject.SetActive(true);
        }

        ResetLifelineUsageFlags();
        UpdateLifelineButtonStates();

        PrepareOptionsForRound();
        for (var i = 0; i < 4; i++)
        {
            if (optionImages[i] != null)
                optionImages[i].color = _optionBaseImageColors[i];
        }
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
            if (!optionLabels[i].gameObject.activeSelf) continue;
            var line = $"{(char)('A' + i)}: {data.Options[i]}";
            yield return TypewriterTMP.Animate(optionLabels[i], line, optionTypewriterMode, optionStepDelay);
        }

        for (var i = 0; i < 4; i++)
        {
            if (optionButtons[i] != null) optionButtons[i].interactable = !_timeUpEnded;
        }

        _introComplete = true;

        if (startTimerAfterIntroTypewriter && !_timeUpEnded)
            _timerCoroutine = StartCoroutine(TimerRoutine());

        if (!_timeUpEnded)
            SetLifelineButtonsInteractable(true);
    }

    QuizQuestionData GetQuestionData()
    {
        return _activeQuestion ?? QuizContent.FallbackQuestion;
    }

    void ResetLifelineUsageFlags()
    {
        _fiftyUsedThisQuestion = false;
        _audienceUsedThisQuestion = false;
        _phoneUsedThisQuestion = false;
    }

    void UpdateLifelineButtonStates()
    {
        if (lifeline5050Button != null)
            lifeline5050Button.interactable = !_fiftyUsedThisQuestion;
        if (lifelineAudienceButton != null)
            lifelineAudienceButton.interactable = !_audienceUsedThisQuestion;
        if (lifelinePhoneButton != null)
            lifelinePhoneButton.interactable = !_phoneUsedThisQuestion;
    }

    void SetLifelineButtonsInteractable(bool value)
    {
        if (!value)
        {
            if (lifeline5050Button != null) lifeline5050Button.interactable = false;
            if (lifelineAudienceButton != null) lifelineAudienceButton.interactable = false;
            if (lifelinePhoneButton != null) lifelinePhoneButton.interactable = false;
            return;
        }

        UpdateLifelineButtonStates();
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

    void OnLifeline5050()
    {
        if (_timeUpEnded || !_introComplete || _fiftyUsedThisQuestion) return;
        var data = GetQuestionData();
        var correct = data.CorrectOptionIndex;
        var wrong = new List<int>(3);
        for (var i = 0; i < 4; i++)
        {
            if (i != correct) wrong.Add(i);
        }

        if (wrong.Count < 2) return;

        var a = UnityEngine.Random.Range(0, wrong.Count);
        var idxA = wrong[a];
        wrong.RemoveAt(a);
        var idxB = wrong[UnityEngine.Random.Range(0, wrong.Count)];

        foreach (var idx in new[] { idxA, idxB })
        {
            if (optionLabels[idx] != null)
                optionLabels[idx].gameObject.SetActive(false);
            if (optionButtons[idx] != null)
                optionButtons[idx].interactable = false;
        }

        _fiftyUsedThisQuestion = true;
        if (lifeline5050Button != null) lifeline5050Button.interactable = false;
    }

    void OnLifelineAudience()
    {
        if (_timeUpEnded || !_introComplete || _audienceUsedThisQuestion) return;
        _audienceUsedThisQuestion = true;
        if (lifelineAudienceButton != null) lifelineAudienceButton.interactable = false;
        ShowLifelinePrompt(
            "Ask the audience for their vote.\n\nWhen everyone has answered, press OK to return to the question.");
    }

    void OnLifelinePhone()
    {
        if (_timeUpEnded || !_introComplete || _phoneUsedThisQuestion) return;
        _phoneUsedThisQuestion = true;
        if (lifelinePhoneButton != null) lifelinePhoneButton.interactable = false;
        ShowLifelinePrompt(
            "Phone a friend.\n\nDiscuss the question briefly, then press OK when you are ready to continue.");
    }

    void ShowLifelinePrompt(string message)
    {
        EnsureLifelinePromptPanel();
        if (_lifelinePromptBody != null) _lifelinePromptBody.text = message;
        if (_lifelinePromptRoot != null) _lifelinePromptRoot.SetActive(true);
    }

    void HideLifelinePrompt()
    {
        if (_lifelinePromptRoot != null) _lifelinePromptRoot.SetActive(false);
    }

    void EnsureLifelinePromptPanel()
    {
        if (_lifelinePromptRoot != null) return;

        var white = GetWhiteSprite();
        var root = new GameObject("LifelinePromptOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 11000;
        StretchFull(root.GetComponent<RectTransform>());

        var dim = new GameObject("Dim", typeof(Image));
        dim.transform.SetParent(root.transform, false);
        var dimImg = dim.GetComponent<Image>();
        dimImg.sprite = white;
        dimImg.color = new Color(0.05f, 0.02f, 0.12f, 0.82f);
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
        prt.sizeDelta = new Vector2(520f, 300f);
        prt.anchoredPosition = Vector2.zero;
        panel.AddComponent<Outline>().effectColor = new Color(0.88f, 0.72f, 0.22f, 1f);

        var bodyGo = new GameObject("Body", typeof(TextMeshProUGUI));
        bodyGo.transform.SetParent(panel.transform, false);
        _lifelinePromptBody = bodyGo.GetComponent<TextMeshProUGUI>();
        _lifelinePromptBody.fontSize = 22f;
        _lifelinePromptBody.alignment = TextAlignmentOptions.Center;
        _lifelinePromptBody.color = Color.white;
        _lifelinePromptBody.textWrappingMode = TextWrappingModes.Normal;
        var brt = _lifelinePromptBody.rectTransform;
        brt.anchorMin = new Vector2(0.08f, 0.28f);
        brt.anchorMax = new Vector2(0.92f, 0.88f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;

        var btnGo = new GameObject("OK Button", typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panel.transform, false);
        var bImg = btnGo.GetComponent<Image>();
        bImg.sprite = white;
        bImg.color = new Color(0.42f, 0.28f, 0.62f, 1f);
        var okRt = btnGo.GetComponent<RectTransform>();
        okRt.anchorMin = new Vector2(0.32f, 0.08f);
        okRt.anchorMax = new Vector2(0.68f, 0.22f);
        okRt.offsetMin = okRt.offsetMax = Vector2.zero;
        _lifelinePromptOkButton = btnGo.GetComponent<Button>();

        var lblGo = new GameObject("Label", typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(btnGo.transform, false);
        var lbl = lblGo.GetComponent<TextMeshProUGUI>();
        lbl.text = "OK";
        lbl.fontSize = 24f;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = Color.white;
        lbl.fontStyle = FontStyles.Bold;
        StretchFull(lbl.rectTransform);

        _lifelinePromptOkButton.onClick.AddListener(HideLifelinePrompt);
        _lifelinePromptRoot = root;
        root.SetActive(false);
    }

    IEnumerator TimerRoutine()
    {
        var remaining = questionTimeSeconds;
        while (remaining > 0f && !_timeUpEnded)
        {
            if (timerLabel != null)
                timerLabel.text = Mathf.CeilToInt(remaining).ToString();
            yield return null;
            remaining -= Time.unscaledDeltaTime;
        }

        if (_timeUpEnded) yield break;

        if (timerLabel != null)
            timerLabel.text = "0";

        OnTimeUp();
    }

    void OnTimeUp()
    {
        if (_timeUpEnded) return;
        _timeUpEnded = true;
        StopTimerCoroutine();
        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
            _introCoroutine = null;
        }
        if (_advanceCoroutine != null)
        {
            StopCoroutine(_advanceCoroutine);
            _advanceCoroutine = null;
        }

        SetAllOptionsInteractable(false);
        SetLifelineButtonsInteractable(false);
        HideLifelinePrompt();

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
        if (_timeUpEnded || !_introComplete) return;
        if (_advanceCoroutine != null) return;

        StopTimerCoroutine();
        SetAllOptionsInteractable(false);
        SetLifelineButtonsInteractable(false);

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

        _advanceCoroutine = StartCoroutine(AdvanceToNextQuestionAfterFeedbackRoutine());
    }

    IEnumerator AdvanceToNextQuestionAfterFeedbackRoutine()
    {
        yield return new WaitForSecondsRealtime(pauseBeforeNextQuestionSeconds);

        for (var i = 0; i < 4; i++)
        {
            if (optionImages[i] != null)
                optionImages[i].color = _optionBaseImageColors[i];
        }

        _activeQuestion = QuizContent.GetRandomForCategoryAvoiding(_quizCategoryId, forceQuestionSlot, _activeQuestion);
        _introComplete = false;

        if (_introCoroutine != null)
            StopCoroutine(_introCoroutine);
        _introCoroutine = StartCoroutine(IntroTypewriterRoutine());
        _advanceCoroutine = null;
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
