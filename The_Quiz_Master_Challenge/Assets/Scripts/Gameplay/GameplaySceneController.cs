using System;

using System.Collections;

using System.Collections.Generic;

using TMPro;

using UnityEngine;

using UnityEngine.SceneManagement;

using UnityEngine.UI;



[DisallowMultipleComponent]

public class GameplaySceneController : MonoBehaviour

{

    [Header("Scenes")]

    [SerializeField] string menuSceneName = "Menu";



    [Header("Timer")]

    [SerializeField] float questionTimeSeconds = 30f;

    [SerializeField] bool startTimerAfterIntroTypewriter = true;



    [Header("Answer feedback")]

    [SerializeField] float pauseBeforeNextQuestionSeconds = 0.55f;

    [SerializeField] float optionRevealHoldSeconds = 2f;

    [SerializeField] float optionBlinkPulseSeconds = 0.14f;

    [SerializeField] int optionBlinkPulseCount = 10;



    [Header("Team play")]

    [Tooltip("Delay after all options finish revealing before the choose-team popup appears.")]

    [SerializeField] float teamPlayPostOptionsDelaySeconds = 5f;

    [Tooltip("Assign your authored popup under Canvas/Main, or leave empty to auto-find / generate.")]

    [SerializeField] GameObject teamSelectPopupRoot;



    [Header("Question transition")]

    [SerializeField] float questionTransitionHalfSeconds = 0.32f;

    [SerializeField] float transitionFadeOutAlpha = 0.08f;

    [SerializeField] float transitionMinScale = 0.94f;

    [SerializeField] float transitionPeakScale = 1.03f;



    [Header("Round banners (Easy / Medium / Hard blocks)")]

    [SerializeField] float roundBannerDisplaySeconds = 2f;



    [Header("Highlighter colours (options)")]

    [SerializeField] Color optionHighlightYellow = new(1f, 0.82f, 0.15f, 1f);

    [SerializeField] Color optionCorrectBright = new(0.25f, 0.95f, 0.45f, 1f);

    [SerializeField] Color optionCorrectDim = new(0.15f, 0.55f, 0.28f, 1f);

    [SerializeField] Color optionWrongBright = new(1f, 0.28f, 0.28f, 1f);

    [SerializeField] Color optionWrongDim = new(0.55f, 0.12f, 0.12f, 1f);

    [Tooltip("Alpha on option + team highlighter images so TMP behind stays readable.")]
    [SerializeField, Range(0.06f, 0.55f)]
    float optionAndTeamHighlighterAlpha = 0.18f;



    [Header("Typewriter")]

    [SerializeField] TypewriterStepMode questionTypewriterMode = TypewriterStepMode.Words;

    [SerializeField] float questionStepDelay = 0.18f;

    [SerializeField] TypewriterStepMode optionTypewriterMode = TypewriterStepMode.Characters;

    [SerializeField] float optionStepDelay = 0.07f;

    [SerializeField] int forceQuestionSlot = -1;



    [Header("Colours (fallback RawImage tint when no highlighter)")]

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


    [Header("Narrator (WebGL: browser text-to-speech)")]

    [SerializeField] QuizVoiceDirector quizVoiceDirector;


    [Header("SFX (assign clips on GameplaySfx)")]

    [SerializeField] GameplaySfx gameplaySfx;



    GameplayRoundEconomy _roundEconomy;



    QuizQuestionData _activeQuestion;

    string _quizCategoryId;



    GameObject _timeUpRoot;

    TMP_Text _timeUpMessage;

    Button _timeUpMenuButton;



    GameObject _roundBannerRoot;

    TMP_Text _roundBannerText;



    GameObject _lifelinePromptRoot;

    TMP_Text _lifelinePromptBody;

    Button _lifelinePromptOkButton;



    Color[] _optionBaseImageColors = new Color[4];

    Graphic[] _optionHighlighterGraphics = new Graphic[4];

    Color[] _optionHighlighterBaseColors = new Color[4];



    bool _timeUpEnded;

    bool _introComplete;

    Coroutine _timerCoroutine;

    bool _freezeQuestionTimerCountdown;

    Coroutine _introCoroutine;

    Coroutine _advanceCoroutine;



    bool _fiftyUsedThisQuestion;

    bool _audienceUsedThisQuestion;

    bool _phoneUsedThisQuestion;



    int _teacherSequentialIndex;



    bool _teamPlayActive;

    bool _teamPopupWired;

    bool _teamPopupAwaitingOk;

    int _teamPopupSelectedIndex = -1;

    Button _teamSelectOkButton;

    readonly Button[] _teamSelectPickButtons = new Button[4];

    readonly Graphic[] _teamPopupHighlighterGraphics = new Graphic[4];

    /// <summary>When true (team play), Team panel pick buttons are non-interactable until the choose-team popup is visible.</summary>

    bool _teamPickUiLocked = true;



    GameObject _validationToastRoot;

    CanvasGroup _validationToastGroup;

    TMP_Text _validationToastText;

    Coroutine _validationToastCoroutine;



    CanvasGroup _mainCanvasGroup;

    RectTransform _mainRect;

    Vector3 _mainBaseScale = Vector3.one;



    readonly Color[] _bottomTeamHighlighterBaseColors = new Color[4];

    bool _bottomTeamHighlighterColorsCached;



    void Awake()

    {

        _teamPlayActive = PlayerPrefs.GetInt(StudentCredentials.PrefsViaTeamPlayKey, 0) == 1;

        ResolveReferences();

        CacheBottomTeamHighlighterColours();

        ResolveOptionHighlighters();

        WireBackButton();

        WireLifelineButtons();

        EnsureTeamSelectPopupResolved();

        WireTeamSelectPopup();

        ApplyTeamBarsVisibility();

        FillCategoryLabel();

        EnsureMainTransitionRoot();



        _roundEconomy = GetComponent<GameplayRoundEconomy>();

        if (_roundEconomy == null)

            _roundEconomy = gameObject.AddComponent<GameplayRoundEconomy>();

        _roundEconomy.Initialize(teamsDetailsRoot, transform);

        if (quizVoiceDirector == null)

            quizVoiceDirector = GetComponent<QuizVoiceDirector>();

        if (quizVoiceDirector == null)

            quizVoiceDirector = GetComponentInChildren<QuizVoiceDirector>(true);

        if (quizVoiceDirector == null)

            quizVoiceDirector = gameObject.AddComponent<QuizVoiceDirector>();

    }



    void Start()

    {

        ResolveGameplaySfx();

        LevelCompletionResults.Clear();

        _quizCategoryId = PlayerPrefs.GetString(StudentCredentials.PrefsSelectedQuizKey, "Math");

        _teacherSequentialIndex = 0;

        _activeQuestion = QuizContent.GetSequentialForCategoryWithProgress(_quizCategoryId, _teacherSequentialIndex, forceQuestionSlot, null);



        _roundEconomy?.OnRunStarted(_teamPlayActive);

        _introCoroutine = StartCoroutine(IntroTypewriterRoutine());

    }



    void ResolveGameplaySfx()

    {

        var allSfx = GetComponentsInChildren<GameplaySfx>(true);

        foreach (var g in allSfx)

            if (g != null)

                g.InitializeAudio();

        if (gameplaySfx == null)

        {

            foreach (var g in allSfx)

            {

                if (g != null && g.HasAnyClipConfigured())

                {

                    gameplaySfx = g;

                    break;

                }

            }



            if (gameplaySfx == null && allSfx.Length > 0)

                gameplaySfx = allSfx[0];

        }



        if (gameplaySfx == null)

            gameplaySfx = gameObject.AddComponent<GameplaySfx>();



        gameplaySfx.InitializeAudio();

    }



    void OnDestroy()

    {

        if (backButton != null) backButton.onClick.RemoveListener(GoToMenu);

        if (_timeUpMenuButton != null) _timeUpMenuButton.onClick.RemoveListener(GoToMenu);

        if (lifeline5050Button != null) lifeline5050Button.onClick.RemoveListener(OnLifeline5050);

        if (lifelineAudienceButton != null) lifelineAudienceButton.onClick.RemoveListener(OnLifelineAudience);

        if (lifelinePhoneButton != null) lifelinePhoneButton.onClick.RemoveListener(OnLifelinePhone);

        if (_lifelinePromptOkButton != null) _lifelinePromptOkButton.onClick.RemoveListener(HideLifelinePrompt);



        quizVoiceDirector?.CancelSpeech();

        gameplaySfx?.StopTimerLoop();

        UnwireTeamSelectPopup();

    }



    void ResolveReferences()

    {

        if (teamsDetailsRoot == null)

        {

            teamsDetailsRoot = transform.Find("Main/TEAMS DETAILS");

            if (teamsDetailsRoot == null)

                teamsDetailsRoot = transform.Find("Main/Team Panel");

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

        if (optionRoot == null)

            optionRoot = transform.Find("Main");

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

        if (lifeRoot == null)

            lifeRoot = transform.Find("Main/Lifeline Panel");

        if (lifeRoot != null)

        {

            if (lifeline5050Button == null)

                lifeline5050Button = FindLifelineButton(lifeRoot, "Lifeline 50 50 Button");

            if (lifelineAudienceButton == null)

                lifelineAudienceButton = FindLifelineButton(lifeRoot, "Ask audience Button");

            if (lifelinePhoneButton == null)

                lifelinePhoneButton = FindLifelineButton(lifeRoot, "Call A Friend Button");

        }

    }



    void ResolveOptionHighlighters()

    {

        for (var i = 0; i < 4; i++)

        {

            var btnTr = optionButtons[i] != null ? optionButtons[i].transform : null;

            if (btnTr == null) continue;

            var hiTr = FindHighlighterTransform(btnTr);

            if (hiTr == null) continue;

            var g = hiTr.GetComponent<Graphic>();

            if (g == null) continue;

            _optionHighlighterGraphics[i] = g;

            _optionHighlighterBaseColors[i] = g.color;

            g.raycastTarget = false;

            g.gameObject.SetActive(false);

        }

    }



    static Transform FindHighlighterTransform(Transform parent)

    {

        for (var i = 0; i < parent.childCount; i++)

        {

            var c = parent.GetChild(i);

            if (c.name.IndexOf("highlight", StringComparison.OrdinalIgnoreCase) >= 0)

                return c;

        }



        return null;

    }



    static Graphic FindHighlighterGraphicOnTeamRow(Transform teamRow)

    {

        var t = FindHighlighterTransform(teamRow);

        return t != null ? t.GetComponent<Graphic>() : null;

    }



    void EnsureMainTransitionRoot()

    {

        if (_mainRect != null) return;

        var main = transform.Find("Main");

        if (main == null) return;

        _mainRect = main as RectTransform;

        if (_mainRect == null) return;

        _mainBaseScale = _mainRect.localScale;

        _mainCanvasGroup = main.GetComponent<CanvasGroup>();

        if (_mainCanvasGroup == null)

            _mainCanvasGroup = main.gameObject.AddComponent<CanvasGroup>();

    }



    void EnsureTeamSelectPopupResolved()

    {

        if (!_teamPlayActive) return;



        if (teamSelectPopupRoot == null)

        {

            foreach (var path in new[] { "Main/Team Select Popup", "Main/Choose Team Popup", "Main/Team Choice Popup", "Team Select Popup" })

            {

                var t = transform.Find(path);

                if (t != null)

                {

                    teamSelectPopupRoot = t.gameObject;

                    break;

                }

            }

        }



        if (teamSelectPopupRoot == null)

            BuildRuntimeTeamSelectPopup();



        if (teamSelectPopupRoot != null)

        {

            NormalizeTeamSelectPopupLayout();

            teamSelectPopupRoot.SetActive(false);

        }

    }



    void NormalizeTeamSelectPopupLayout()

    {

        if (teamSelectPopupRoot == null) return;

        var panel = teamSelectPopupRoot.transform.Find("Panel");

        if (panel == null) return;

        var rt = panel as RectTransform;

        if (rt == null) return;

        rt.anchorMin = new Vector2(0.5f, 1f);

        rt.anchorMax = new Vector2(0.5f, 1f);

        rt.pivot = new Vector2(0.5f, 1f);

        rt.sizeDelta = new Vector2(540f, 168f);

        rt.anchoredPosition = new Vector2(0f, -8f);



        var dim = teamSelectPopupRoot.transform.Find("Dim");

        if (dim != null)

            dim.gameObject.SetActive(false);



        var titleTr = panel.Find("Title");

        if (titleTr != null)

        {

            var tmp = titleTr.GetComponent<TMP_Text>();

            if (tmp != null)

                tmp.text = "choose a team - & press OK";

        }



        var subTr = panel.Find("Subtitle");

        if (subTr != null)

            subTr.gameObject.SetActive(false);

    }



    void BuildRuntimeTeamSelectPopup()

    {

        var white = GetWhiteSprite();

        var root = new GameObject("Team Select Popup", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));

        root.transform.SetParent(transform, false);

        var canvas = root.GetComponent<Canvas>();

        canvas.overrideSorting = true;

        canvas.sortingOrder = 10800;

        StretchFull(root.GetComponent<RectTransform>());



        var panel = new GameObject("Panel", typeof(Image));

        panel.transform.SetParent(root.transform, false);

        var pImg = panel.GetComponent<Image>();

        RuntimeGeneratedUiStyle.ApplyPanel(pImg);

        pImg.raycastTarget = true;

        var prt = panel.GetComponent<RectTransform>();

        prt.anchorMin = new Vector2(0.5f, 1f);

        prt.anchorMax = new Vector2(0.5f, 1f);

        prt.pivot = new Vector2(0.5f, 1f);

        prt.sizeDelta = new Vector2(540f, 168f);

        prt.anchoredPosition = new Vector2(0f, -8f);

        if (!RuntimeGeneratedUiStyle.UsePremiumChrome())

        {

            var outline = panel.AddComponent<Outline>();

            outline.effectDistance = new Vector2(2f, -2f);

            outline.effectColor = new Color(0.92f, 0.75f, 0.22f, 1f);

        }



        var titleGo = new GameObject("Title", typeof(TextMeshProUGUI));

        titleGo.transform.SetParent(panel.transform, false);

        var title = titleGo.GetComponent<TextMeshProUGUI>();

        title.text = "choose a team - & press OK";

        title.fontSize = 20f;

        title.fontStyle = FontStyles.Bold;

        title.alignment = TextAlignmentOptions.Center;

        title.color = new Color(1f, 0.9f, 0.45f, 1f);

        var titleRt = title.rectTransform;

        titleRt.anchorMin = new Vector2(0.06f, 0.3f);

        titleRt.anchorMax = new Vector2(0.94f, 0.92f);

        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;



        var okGo = new GameObject("OK Button", typeof(Image), typeof(Button));

        okGo.transform.SetParent(panel.transform, false);

        var okImg = okGo.GetComponent<Image>();

        RuntimeGeneratedUiStyle.ApplyButton(okImg);

        var okRt = okGo.GetComponent<RectTransform>();

        okRt.anchorMin = new Vector2(0.32f, 0.06f);

        okRt.anchorMax = new Vector2(0.68f, 0.28f);

        okRt.offsetMin = okRt.offsetMax = Vector2.zero;

        if (!RuntimeGeneratedUiStyle.UsePremiumChrome())

            okGo.AddComponent<Outline>().effectColor = new Color(1f, 0.88f, 0.4f, 0.9f);



        var okLblGo = new GameObject("Label", typeof(TextMeshProUGUI));

        okLblGo.transform.SetParent(okGo.transform, false);

        var okLbl = okLblGo.GetComponent<TextMeshProUGUI>();

        okLbl.text = "OK";

        okLbl.fontSize = 20f;

        okLbl.fontStyle = FontStyles.Bold;

        okLbl.alignment = TextAlignmentOptions.Center;

        okLbl.color = Color.white;

        StretchFull(okLbl.rectTransform);



        teamSelectPopupRoot = root;

        root.SetActive(false);

        GetComponent<UIButtonClickFeedback>()?.RegisterNewButtonsInHierarchy();

    }



    void WireTeamSelectPopup()

    {

        if (!_teamPlayActive || teamSelectPopupRoot == null || teamsDetailsRoot == null || _teamPopupWired) return;



        var okBtn = FindDeepButton(teamSelectPopupRoot.transform, "OK Button")

                    ?? FindDeepButton(teamSelectPopupRoot.transform, "Ok Button");

        if (okBtn == null) return;



        _teamPopupWired = true;

        _teamSelectOkButton = okBtn;

        _teamSelectOkButton.onClick.AddListener(OnTeamPopupOkClicked);



        HideDuplicateTeamPickButtonsUnderPopup();



        for (var i = 0; i < 4; i++)

        {

            var letter = (char)('A' + i);

            var pickTr = teamsDetailsRoot.Find($"Team {letter}");

            if (pickTr == null)

                pickTr = FindDeepChild(teamsDetailsRoot, $"Team {letter}");

            if (pickTr == null)

                pickTr = FindDeepChild(teamsDetailsRoot, $"Team{letter}");



            var btn = pickTr != null ? pickTr.GetComponent<Button>() : null;

            _teamSelectPickButtons[i] = btn;

            if (btn != null)

            {

                var captured = i;

                btn.onClick.AddListener(() => OnTeamPickButtonClicked(captured));

            }



            var hi = pickTr != null ? FindHighlighterGraphicOnTeamRow(pickTr) : null;

            _teamPopupHighlighterGraphics[i] = hi;

            if (hi != null)

                hi.raycastTarget = false;

        }



        SetTeamPickUiLocked(true);

    }



    void HideDuplicateTeamPickButtonsUnderPopup()

    {

        if (teamSelectPopupRoot == null) return;

        for (var i = 0; i < 4; i++)

        {

            var letter = (char)('A' + i);

            var tr = FindDeepChild(teamSelectPopupRoot.transform, $"Team {letter}");

            if (tr == null)

                tr = FindDeepChild(teamSelectPopupRoot.transform, $"Team{letter}");

            if (tr != null)

                tr.gameObject.SetActive(false);

        }

    }



    void UnwireTeamSelectPopup()

    {

        if (!_teamPopupWired) return;



        if (_teamSelectOkButton != null)

            _teamSelectOkButton.onClick.RemoveListener(OnTeamPopupOkClicked);



        for (var i = 0; i < 4; i++)

        {

            if (_teamSelectPickButtons[i] == null) continue;

            _teamSelectPickButtons[i].onClick.RemoveAllListeners();

        }



        _teamPopupWired = false;

    }



    static Transform FindDeepChild(Transform root, string exactName)

    {

        if (root.name == exactName)

            return root;

        for (var i = 0; i < root.childCount; i++)

        {

            var f = FindDeepChild(root.GetChild(i), exactName);

            if (f != null)

                return f;

        }



        return null;

    }



    static Button FindDeepButton(Transform root, string exactName)

    {

        if (root.name == exactName)

        {

            var b = root.GetComponent<Button>();

            if (b != null) return b;

        }



        for (var i = 0; i < root.childCount; i++)

        {

            var b = FindDeepButton(root.GetChild(i), exactName);

            if (b != null) return b;

        }



        return null;

    }



    void ApplyTeamPickButtonsEnabledState()

    {

        var allowed = GetAllowedTeamIndices();

        for (var i = 0; i < 4; i++)

        {

            if (_teamSelectPickButtons[i] == null) continue;

            if (_teamPlayActive && _teamPickUiLocked)

                _teamSelectPickButtons[i].interactable = false;

            else if (_roundEconomy != null && _roundEconomy.IsTeamEliminated(i))

                _teamSelectPickButtons[i].interactable = false;

            else

                _teamSelectPickButtons[i].interactable = allowed.Count == 0 || allowed.Contains(i);

        }

    }



    void SetTeamPickUiLocked(bool locked)

    {

        _teamPickUiLocked = locked;

        ApplyTeamPickButtonsEnabledState();

    }



    HashSet<int> GetAllowedTeamIndices()

    {

        var set = new HashSet<int>();

        var letters = ParseTeamLetters(PlayerPrefs.GetString(StudentCredentials.PrefsSelectedTeamsKey, string.Empty));

        if (letters.Count == 0)

            return set;



        for (var i = 0; i < 4; i++)

        {

            if (letters.Contains((char)('A' + i)))

                set.Add(i);

        }



        return set;

    }



    void OnTeamPickButtonClicked(int index)

    {

        if (_teamPlayActive && _teamPickUiLocked)

            return;

        var allowed = GetAllowedTeamIndices();

        if (allowed.Count > 0 && !allowed.Contains(index))

            return;



        _teamPopupSelectedIndex = index;

        for (var i = 0; i < 4; i++)

        {

            var g = _teamPopupHighlighterGraphics[i];

            if (g == null) continue;

            g.gameObject.SetActive(i == index);

            if (i == index)

                g.color = HighlighterTint(new Color(1f, 0.88f, 0.28f, 1f));

            else

            {

                g.gameObject.SetActive(false);

                g.color = _bottomTeamHighlighterBaseColors[i];

            }

        }

    }



    void OnTeamPopupOkClicked()

    {

        if (_teamPopupSelectedIndex < 0)

        {

            ShowTeamValidationToast(

                "Please choose a team first, then press OK.");

            return;

        }



        SyncBottomBarTeamHighlight(_teamPopupSelectedIndex);

        SetTeamPickUiLocked(true);

        _teamPopupAwaitingOk = false;

        if (teamSelectPopupRoot != null)

            teamSelectPopupRoot.SetActive(false);

    }



    void SyncBottomBarTeamHighlight(int teamIndex)

    {

        ClearBottomBarTeamHighlights();

        if (teamsDetailsRoot == null || teamIndex < 0 || teamIndex >= teamsDetailsRoot.childCount)

            return;

        var row = teamsDetailsRoot.GetChild(teamIndex);

        if (!row.gameObject.activeSelf)

            return;

        var g = FindHighlighterGraphicOnTeamRow(row);

        if (g != null)

        {

            g.gameObject.SetActive(true);

            g.color = HighlighterTint(new Color(1f, 0.88f, 0.28f, 1f));

        }

    }



    Color HighlighterTint(Color rgb)

    {

        return new Color(rgb.r, rgb.g, rgb.b, optionAndTeamHighlighterAlpha);

    }



    void CacheBottomTeamHighlighterColours()

    {

        if (_bottomTeamHighlighterColorsCached || teamsDetailsRoot == null) return;

        for (var i = 0; i < 4 && i < teamsDetailsRoot.childCount; i++)

        {

            var g = FindHighlighterGraphicOnTeamRow(teamsDetailsRoot.GetChild(i));

            if (g != null)

                _bottomTeamHighlighterBaseColors[i] = g.color;

        }

        _bottomTeamHighlighterColorsCached = true;

    }



    void ClearBottomBarTeamHighlights()

    {

        if (teamsDetailsRoot == null) return;

        for (var i = 0; i < teamsDetailsRoot.childCount && i < 4; i++)

        {

            var row = teamsDetailsRoot.GetChild(i);

            var g = FindHighlighterGraphicOnTeamRow(row);

            if (g == null) continue;

            g.gameObject.SetActive(false);

            g.color = _bottomTeamHighlighterBaseColors[i];

        }

    }



    IEnumerator TeamSelectionPopupRoutine()

    {

        EnsureTeamSelectPopupResolved();

        WireTeamSelectPopup();

        if (teamSelectPopupRoot == null)

            yield break;



        if (!_teamPopupWired)

        {

            Debug.LogError("GameplaySceneController: Team select OK button or Team Panel missing — cannot continue team flow.");

            yield break;

        }



        SetTeamPickUiLocked(true);

        _teamPopupAwaitingOk = true;

        _teamPopupSelectedIndex = -1;

        ClearBottomBarTeamHighlights();

        NormalizeTeamSelectPopupLayout();

        HideDuplicateTeamPickButtonsUnderPopup();

        SetTeamPickUiLocked(false);

        teamSelectPopupRoot.SetActive(true);

        GetComponent<UIButtonClickFeedback>()?.RegisterNewButtonsInHierarchy();



        while (_teamPopupAwaitingOk)

            yield return null;



        SetTeamPickUiLocked(true);

    }



    void ShowTeamValidationToast(string message)

    {

        EnsureValidationToast();

        if (_validationToastText != null)

            _validationToastText.text = message;

        if (_validationToastCoroutine != null)

            StopCoroutine(_validationToastCoroutine);

        _validationToastCoroutine = StartCoroutine(ValidationToastRoutine());

    }



    void EnsureValidationToast()

    {

        if (_validationToastRoot != null) return;



        var white = GetWhiteSprite();

        var root = new GameObject("TeamValidationToast", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));

        root.transform.SetParent(transform, false);

        var canvas = root.GetComponent<Canvas>();

        canvas.overrideSorting = true;

        canvas.sortingOrder = 13000;

        StretchFull(root.GetComponent<RectTransform>());



        var panel = new GameObject("Panel", typeof(Image), typeof(CanvasGroup));

        panel.transform.SetParent(root.transform, false);

        var pImg = panel.GetComponent<Image>();

        RuntimeGeneratedUiStyle.ApplyPanel(pImg);

        pImg.raycastTarget = false;

        var prt = panel.GetComponent<RectTransform>();

        prt.anchorMin = new Vector2(0.5f, 0.5f);

        prt.anchorMax = new Vector2(0.5f, 0.5f);

        prt.pivot = new Vector2(0.5f, 0.5f);

        prt.sizeDelta = new Vector2(500f, 132f);

        prt.anchoredPosition = Vector2.zero;

        if (!RuntimeGeneratedUiStyle.UsePremiumChrome())

        {

            var outline = panel.AddComponent<Outline>();

            outline.effectColor = new Color(0.95f, 0.72f, 0.18f, 1f);

            outline.effectDistance = new Vector2(1.5f, -1.5f);

        }



        _validationToastGroup = panel.GetComponent<CanvasGroup>();

        _validationToastGroup.alpha = 0f;

        _validationToastGroup.blocksRaycasts = false;



        var accent = new GameObject("Accent", typeof(Image));

        accent.transform.SetParent(panel.transform, false);

        var aImg = accent.GetComponent<Image>();

        aImg.sprite = white;

        aImg.color = new Color(0.85f, 0.65f, 0.15f, 0.95f);

        aImg.raycastTarget = false;

        var art = accent.GetComponent<RectTransform>();

        art.anchorMin = new Vector2(0f, 1f);

        art.anchorMax = new Vector2(1f, 1f);

        art.pivot = new Vector2(0.5f, 1f);

        art.sizeDelta = new Vector2(0f, 5f);

        art.anchoredPosition = Vector2.zero;

        accent.SetActive(!RuntimeGeneratedUiStyle.UsePremiumChrome());



        var bodyGo = new GameObject("Body", typeof(TextMeshProUGUI));

        bodyGo.transform.SetParent(panel.transform, false);

        _validationToastText = bodyGo.GetComponent<TextMeshProUGUI>();

        _validationToastText.fontSize = 19f;

        _validationToastText.alignment = TextAlignmentOptions.Center;

        _validationToastText.color = new Color(0.98f, 0.95f, 1f, 1f);

        _validationToastText.textWrappingMode = TextWrappingModes.Normal;

        var brt = _validationToastText.rectTransform;

        brt.anchorMin = new Vector2(0.06f, 0.1f);

        brt.anchorMax = new Vector2(0.94f, 0.82f);

        brt.offsetMin = brt.offsetMax = Vector2.zero;



        _validationToastRoot = root;

        root.SetActive(false);

    }



    IEnumerator ValidationToastRoutine()

    {

        if (_validationToastRoot == null) yield break;

        _validationToastRoot.SetActive(true);

        var fadeIn = 0.18f;

        var hold = 2.4f;

        var fadeOut = 0.35f;

        float t;

        t = 0f;

        while (t < fadeIn)

        {

            t += Time.unscaledDeltaTime;

            _validationToastGroup.alpha = Mathf.Clamp01(t / fadeIn);

            yield return null;

        }



        _validationToastGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(hold);

        t = 0f;

        while (t < fadeOut)

        {

            t += Time.unscaledDeltaTime;

            _validationToastGroup.alpha = Mathf.Clamp01(1f - t / fadeOut);

            yield return null;

        }



        _validationToastGroup.alpha = 0f;

        _validationToastRoot.SetActive(false);

        _validationToastCoroutine = null;

    }



    static Button FindLifelineButton(Transform lifeRoot, string objectName)

    {

        var direct = lifeRoot.Find(objectName);

        if (direct != null)

        {

            var b = direct.GetComponent<Button>();

            if (b != null) return b;

        }



        foreach (var btn in lifeRoot.GetComponentsInChildren<Button>(true))

        {

            if (btn.gameObject.name == objectName)

                return btn;

        }



        return null;

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

                show = selected.Count > 0 && selected.Contains((char)('A' + i));

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

        if (quizVoiceDirector != null)

            quizVoiceDirector.OnNewQuestionStarting();

        if (_teamPlayActive)

            SetTeamPickUiLocked(true);

        SetLifelineButtonsInteractable(false);

        StopTimerCoroutine();

        _freezeQuestionTimerCountdown = false;

        _timeUpEnded = false;

        RefreshTimerLabelForNewRound();



        yield return StartCoroutine(ShowRoundBannerIfNeededRoutine());



        if (questionText != null)

        {

            questionText.text = string.Empty;

            questionText.gameObject.SetActive(false);

        }



        for (var i = 0; i < 4; i++)

        {

            if (optionLabels[i] != null)

                optionLabels[i].gameObject.SetActive(false);

            if (optionButtons[i] != null)

                optionButtons[i].gameObject.SetActive(false);

        }



        ResetLifelineUsageFlags();

        UpdateLifelineButtonStates();



        PrepareOptionsForRound();

        for (var i = 0; i < 4; i++)

        {

            if (optionImages[i] != null)

                optionImages[i].color = _optionBaseImageColors[i];

        }



        ClearOptionHighlighterStates();

        var data = GetQuestionData();

        if (gameplaySfx != null)

            yield return gameplaySfx.PlayQuestionLeadInAndWait();



        if (questionText != null)

        {

            questionText.gameObject.SetActive(true);

            questionText.text = string.Empty;

        }



        for (var i = 0; i < 4; i++)

        {

            if (optionLabels[i] != null) optionLabels[i].text = string.Empty;

            if (optionButtons[i] != null) optionButtons[i].interactable = false;

        }



        if (questionTimeSeconds > 0f && !_timeUpEnded)

            gameplaySfx?.StartTimerLoop();



        if (questionText != null)

            yield return TypewriterTMP.Animate(questionText, data.Question, questionTypewriterMode, questionStepDelay,

                () => quizVoiceDirector?.SpeakQuestion(data.Question));



        for (var i = 0; i < 4; i++)

        {

            if (optionButtons[i] != null)

                optionButtons[i].gameObject.SetActive(true);

            if (optionLabels[i] != null)

                optionLabels[i].gameObject.SetActive(true);

        }



        for (var i = 0; i < 4; i++)

        {

            if (optionLabels[i] == null) continue;

            if (!optionLabels[i].gameObject.activeSelf) continue;

            var line = $"{(char)('A' + i)}: {data.Options[i]}";

            var optIdx = i;

            yield return TypewriterTMP.Animate(optionLabels[i], line, optionTypewriterMode, optionStepDelay,

                () => quizVoiceDirector?.SpeakOption(optIdx, data.Options[optIdx]));

        }

        if (quizVoiceDirector != null)

            yield return quizVoiceDirector.WaitUntilSpeechIdle();



        if (_teamPlayActive)

        {

            yield return new WaitForSecondsRealtime(teamPlayPostOptionsDelaySeconds);

            yield return StartCoroutine(TeamSelectionPopupRoutine());

            if (quizVoiceDirector != null && _teamPopupSelectedIndex >= 0 && _roundEconomy != null)

                quizVoiceDirector.SpeakTeamWillAnswer(_roundEconomy.GetVoiceTeamDisplayName(_teamPopupSelectedIndex));

            if (quizVoiceDirector != null)

                yield return quizVoiceDirector.WaitUntilSpeechIdle();

        }



        if (questionTimeSeconds > 0f && !_timeUpEnded && _timerCoroutine == null)

            _timerCoroutine = StartCoroutine(TimerRoutine(startTimerBed: false));



        if (_roundEconomy != null && !_roundEconomy.RunEnded)

            _roundEconomy.RefreshPrizeLadderDisplay();



        for (var i = 0; i < 4; i++)

        {

            if (optionButtons[i] != null) optionButtons[i].interactable = !_timeUpEnded;

        }



        _introComplete = true;



        if (!_timeUpEnded)

            SetLifelineButtonsInteractable(true);

    }



    QuizQuestionData GetQuestionData()

    {

        return _activeQuestion ?? QuizContent.FallbackQuestion;

    }



    int ResolveEconomyTeamIndex()

    {

        if (!_teamPlayActive) return -1;

        if (_teamPopupSelectedIndex >= 0 && _teamPopupSelectedIndex < 4)

        {

            if (_roundEconomy != null && !_roundEconomy.IsTeamEliminated(_teamPopupSelectedIndex))

                return _teamPopupSelectedIndex;

        }



        if (teamsDetailsRoot != null)

        {

            for (var i = 0; i < 4 && i < teamsDetailsRoot.childCount; i++)

            {

                if (!teamsDetailsRoot.GetChild(i).gameObject.activeSelf) continue;

                if (_roundEconomy != null && _roundEconomy.IsTeamEliminated(i)) continue;

                return i;

            }

        }



        return 0;

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

        var useHighlighterFeedback = HasAnyOptionHighlighter();



        for (var i = 0; i < 4; i++)

        {

            if (optionButtons[i] == null) continue;

            var btn = optionButtons[i];

            var img = optionImages[i];

            if (img != null) _optionBaseImageColors[i] = img.color;



            btn.onClick.RemoveAllListeners();

            var captured = i;

            btn.onClick.AddListener(() => OnOptionClicked(captured, data.CorrectOptionIndex));



            if (useHighlighterFeedback)

            {

                btn.transition = Selectable.Transition.None;

                btn.targetGraphic = img;

            }

            else

            {

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

    }



    bool HasAnyOptionHighlighter()

    {

        for (var i = 0; i < 4; i++)

        {

            if (_optionHighlighterGraphics[i] != null)

                return true;

        }



        return false;

    }



    void SetOptionHighlighterVisible(int index, bool visible, Color? tint = null)

    {

        var g = index >= 0 && index < 4 ? _optionHighlighterGraphics[index] : null;

        if (g == null) return;

        if (tint.HasValue)

        {

            var c = tint.Value;

            g.color = HighlighterTint(c);

        }

        g.gameObject.SetActive(visible);

    }



    void ClearOptionHighlighterStates()

    {

        for (var i = 0; i < 4; i++)

        {

            var g = _optionHighlighterGraphics[i];

            if (g == null) continue;

            g.color = _optionHighlighterBaseColors[i];

            g.gameObject.SetActive(false);

        }

    }



    void OnLifeline5050()

    {

        if (_timeUpEnded || !_introComplete || _fiftyUsedThisQuestion) return;

        gameplaySfx?.PlayLifeline();

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

            SetOptionHighlighterVisible(idx, false);

        }



        _fiftyUsedThisQuestion = true;

        if (lifeline5050Button != null) lifeline5050Button.interactable = false;

    }



    void OnLifelineAudience()

    {

        if (_timeUpEnded || !_introComplete || _audienceUsedThisQuestion) return;

        gameplaySfx?.PlayLifeline();

        _audienceUsedThisQuestion = true;

        if (lifelineAudienceButton != null) lifelineAudienceButton.interactable = false;

        ShowLifelinePrompt(

            "Ask the audience for their vote.\n\nWhen everyone has answered, press OK to return to the question.");

    }



    void OnLifelinePhone()

    {

        if (_timeUpEnded || !_introComplete || _phoneUsedThisQuestion) return;

        gameplaySfx?.PlayLifeline();

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

        RuntimeGeneratedUiStyle.ApplyPanel(pImg);

        var prt = panel.GetComponent<RectTransform>();

        prt.anchorMin = new Vector2(0.5f, 0.5f);

        prt.anchorMax = new Vector2(0.5f, 0.5f);

        prt.pivot = new Vector2(0.5f, 0.5f);

        prt.sizeDelta = new Vector2(560f, 320f);

        prt.anchoredPosition = Vector2.zero;

        if (!RuntimeGeneratedUiStyle.UsePremiumChrome())

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

        RuntimeGeneratedUiStyle.ApplyButton(bImg);

        var okRt = btnGo.GetComponent<RectTransform>();

        okRt.anchorMin = new Vector2(0.3f, 0.07f);

        okRt.anchorMax = new Vector2(0.7f, 0.24f);

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

        GetComponent<UIButtonClickFeedback>()?.RegisterNewButtonsInHierarchy();

    }



    void RefreshTimerLabelForNewRound()

    {

        if (timerLabel == null) return;

        if (questionTimeSeconds > 0f)

            timerLabel.text = Mathf.CeilToInt(questionTimeSeconds).ToString();

        else

            timerLabel.text = "0";

    }



    IEnumerator ShowRoundBannerIfNeededRoutine()

    {

        var label = RoundBannerLabelForIndex(_teacherSequentialIndex);

        if (label == null) yield break;

        EnsureRoundBannerOverlay();

        _roundBannerText.text = label;

        _roundBannerRoot.SetActive(true);

        if (gameplaySfx != null)

            yield return gameplaySfx.PlayRoundBannerIntroAndWait(_teacherSequentialIndex, roundBannerDisplaySeconds);

        else

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, roundBannerDisplaySeconds));

        _roundBannerRoot.SetActive(false);

    }



    static string RoundBannerLabelForIndex(int ladderIndexZeroBased)

    {

        return ladderIndexZeroBased switch

        {

            0 => "ROUND 01",

            5 => "ROUND 02",

            10 => "ROUND 03",

            _ => null

        };

    }



    void EnsureRoundBannerOverlay()

    {

        if (_roundBannerRoot != null) return;

        var white = GetWhiteSprite();

        var root = new GameObject("RoundBannerOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));

        root.transform.SetParent(transform, false);

        var canvas = root.GetComponent<Canvas>();

        canvas.overrideSorting = true;

        canvas.sortingOrder = 12500;

        StretchFull(root.GetComponent<RectTransform>());



        var dim = new GameObject("Dim", typeof(Image));

        dim.transform.SetParent(root.transform, false);

        var dimImg = dim.GetComponent<Image>();

        dimImg.sprite = white;

        dimImg.color = new Color(0f, 0f, 0f, 0.34f);

        dimImg.raycastTarget = true;

        StretchFull(dim.GetComponent<RectTransform>());



        var plate = new GameObject("Plate", typeof(Image));

        plate.transform.SetParent(root.transform, false);

        var plateImg = plate.GetComponent<Image>();

        RuntimeGeneratedUiStyle.ApplyPanel(plateImg);

        plateImg.raycastTarget = false;

        var plt = plate.GetComponent<RectTransform>();

        plt.anchorMin = new Vector2(0.18f, 1f);

        plt.anchorMax = new Vector2(0.82f, 1f);

        plt.pivot = new Vector2(0.5f, 1f);

        plt.sizeDelta = new Vector2(0f, 52f);

        plt.anchoredPosition = new Vector2(0f, -12f);



        var titleGo = new GameObject("RoundTitle", typeof(TextMeshProUGUI));

        titleGo.transform.SetParent(plate.transform, false);

        _roundBannerText = titleGo.GetComponent<TextMeshProUGUI>();

        _roundBannerText.color = new Color(1f, 0.93f, 0.72f, 1f);

        _roundBannerText.fontStyle = FontStyles.Bold;

        _roundBannerText.enableAutoSizing = true;

        _roundBannerText.fontSizeMin = 16f;

        _roundBannerText.fontSizeMax = 40f;

        _roundBannerText.fontSize = 28f;

        _roundBannerText.characterSpacing = 2f;

        _roundBannerText.textWrappingMode = TextWrappingModes.NoWrap;

        _roundBannerText.verticalAlignment = VerticalAlignmentOptions.Middle;

        _roundBannerText.horizontalAlignment = HorizontalAlignmentOptions.Center;



        var trt = _roundBannerText.rectTransform;

        trt.anchorMin = new Vector2(0.06f, 0.1f);

        trt.anchorMax = new Vector2(0.94f, 0.9f);

        trt.offsetMin = trt.offsetMax = Vector2.zero;



        _roundBannerRoot = root;

        root.SetActive(false);

    }



    IEnumerator TimerRoutine(bool startTimerBed = true)

    {

        if (startTimerBed)

            gameplaySfx?.StartTimerLoop();

        var remaining = questionTimeSeconds;

        while (remaining > 0f && !_timeUpEnded && !_freezeQuestionTimerCountdown)

        {

            if (timerLabel != null)

                timerLabel.text = Mathf.CeilToInt(remaining).ToString();

            yield return null;

            remaining -= Time.unscaledDeltaTime;

        }



        _timerCoroutine = null;



        if (_timeUpEnded)

        {

            gameplaySfx?.StopTimerLoop();

            yield break;

        }



        if (_freezeQuestionTimerCountdown)

            yield break;



        gameplaySfx?.StopTimerLoop();



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

        _teamPopupAwaitingOk = false;

        if (teamSelectPopupRoot != null)

            teamSelectPopupRoot.SetActive(false);

        if (_teamPlayActive)

            SetTeamPickUiLocked(true);



        _advanceCoroutine = StartCoroutine(TimeUpAnswerAdvanceRoutine());

    }



    IEnumerator TimeUpAnswerAdvanceRoutine()

    {

        var data = GetQuestionData();

        var correctIdx = Mathf.Clamp(data.CorrectOptionIndex, 0, 3);

        yield return StartCoroutine(AnswerFeedbackAndAdvanceRoutine(-1, correctIdx));

        _advanceCoroutine = null;

    }



    void StopTimerCoroutine()

    {

        if (_timerCoroutine != null)

        {

            StopCoroutine(_timerCoroutine);

            _timerCoroutine = null;

        }

        gameplaySfx?.StopTimerLoop();

    }



    void OnOptionClicked(int index, int correctIndex)

    {

        if (_timeUpEnded || !_introComplete) return;

        if (_advanceCoroutine != null) return;



        _freezeQuestionTimerCountdown = true;

        SetAllOptionsInteractable(false);

        SetLifelineButtonsInteractable(false);

        if (quizVoiceDirector != null)

        {

            quizVoiceDirector.CancelSpeech();

            var pickData = GetQuestionData();

            var teamNm = string.Empty;

            if (_teamPlayActive && _roundEconomy != null)

            {

                var ti = ResolveEconomyTeamIndex();

                if (ti >= 0)

                    teamNm = _roundEconomy.GetVoiceTeamDisplayName(ti);

            }

            quizVoiceDirector.SpeakChosenOptionImmediate(index, pickData, _teamPlayActive, teamNm);

        }



        _advanceCoroutine = StartCoroutine(AnswerFeedbackAndAdvanceRoutine(index, correctIndex));

    }



    IEnumerator AnswerFeedbackAndAdvanceRoutine(int index, int correctIndex)

    {

        var voiceData = GetQuestionData();

        if (index < 0 && quizVoiceDirector != null)

            quizVoiceDirector.SpeakTimeUpReveal(voiceData);

        System.Action<bool> voiceJudgement = null;

        if (index >= 0)

            voiceJudgement = correct =>

            {

                quizVoiceDirector?.SpeakJudgement(correct);

                gameplaySfx?.PlayAnswerJudgement(correct);

            };

        if (HasAnyOptionHighlighter())

            yield return StartCoroutine(OptionHighlighterRevealRoutine(index, correctIndex, voiceJudgement));

        else

            yield return StartCoroutine(LegacyOptionTintRevealRoutine(index, correctIndex, voiceJudgement));



        yield return new WaitForSecondsRealtime(pauseBeforeNextQuestionSeconds);

        if (quizVoiceDirector != null)

            yield return quizVoiceDirector.WaitUntilSpeechIdle();



        ClearOptionHighlighterStates();

        for (var i = 0; i < 4; i++)

        {

            if (optionImages[i] != null)

                optionImages[i].color = _optionBaseImageColors[i];

        }



        if (_teamPlayActive)

            ClearBottomBarTeamHighlights();



        var answerCorrect = index == correctIndex;

        if (_roundEconomy != null)

        {

            yield return _roundEconomy.ProcessAnswerAfterFeedback(

                answerCorrect,

                _teacherSequentialIndex,

                ResolveEconomyTeamIndex(),

                _teamPlayActive,

                ApplyTeamPickButtonsEnabledState);

            if (_roundEconomy.RunEnded)

            {

                _advanceCoroutine = null;

                yield break;

            }

        }



        yield return StartCoroutine(QuestionTransitionRoutine());



        _teacherSequentialIndex++;

        _activeQuestion = QuizContent.GetSequentialForCategoryWithProgress(_quizCategoryId, _teacherSequentialIndex, forceQuestionSlot, _activeQuestion);

        _introComplete = false;



        if (_introCoroutine != null)

            StopCoroutine(_introCoroutine);

        _introCoroutine = StartCoroutine(IntroTypewriterRoutine());

        _advanceCoroutine = null;

    }



    IEnumerator LegacyOptionTintRevealRoutine(int index, int correctIndex, System.Action<bool> onRevealJudgement)

    {

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

        if (index >= 0)

            onRevealJudgement?.Invoke(correct);



        yield return new WaitForSecondsRealtime(optionRevealHoldSeconds);

    }



    IEnumerator OptionHighlighterRevealRoutine(int picked, int correctIndex, System.Action<bool> onRevealJudgement)

    {

        if (picked < 0)

        {

            SetOptionHighlighterVisible(correctIndex, true, optionHighlightYellow);

            yield return new WaitForSecondsRealtime(optionRevealHoldSeconds);

            SetOptionHighlighterVisible(correctIndex, true, optionCorrectBright);

            var gCorrect = correctIndex >= 0 && correctIndex < 4 ? _optionHighlighterGraphics[correctIndex] : null;

            if (gCorrect != null)

                yield return StartCoroutine(PulseGraphicRoutine(gCorrect, optionCorrectBright, optionCorrectDim, optionBlinkPulseCount));

            yield break;

        }



        SetOptionHighlighterVisible(picked, true, optionHighlightYellow);



        yield return new WaitForSecondsRealtime(optionRevealHoldSeconds);



        if (picked == correctIndex)

        {

            SetOptionHighlighterVisible(picked, true, optionCorrectBright);

            onRevealJudgement?.Invoke(true);

            yield return StartCoroutine(PulseGraphicRoutine(_optionHighlighterGraphics[picked], optionCorrectBright, optionCorrectDim, optionBlinkPulseCount));

        }

        else

        {

            onRevealJudgement?.Invoke(false);

            yield return StartCoroutine(PulseWrongAndCorrectRoutine(picked, correctIndex));

        }

    }



    IEnumerator PulseWrongAndCorrectRoutine(int wrongIdx, int correctIdx)

    {

        var pulses = Mathf.Max(4, optionBlinkPulseCount);

        var gWrong = _optionHighlighterGraphics[wrongIdx];

        var gCorrect = _optionHighlighterGraphics[correctIdx];



        SetOptionHighlighterVisible(wrongIdx, true, optionWrongBright);

        SetOptionHighlighterVisible(correctIdx, true, optionCorrectDim);



        for (var p = 0; p < pulses; p++)

        {

            var wrongOn = (p % 2) == 0;

            if (gWrong != null)

                gWrong.color = HighlighterTint(Color.Lerp(optionWrongDim, optionWrongBright, wrongOn ? 1f : 0.35f));

            if (gCorrect != null)

                gCorrect.color = HighlighterTint(Color.Lerp(optionCorrectDim, optionCorrectBright, wrongOn ? 0.4f : 1f));

            yield return new WaitForSecondsRealtime(optionBlinkPulseSeconds);

        }



        if (gWrong != null)

            gWrong.color = HighlighterTint(optionWrongBright);

        if (gCorrect != null)

            gCorrect.color = HighlighterTint(optionCorrectBright);

    }



    IEnumerator PulseGraphicRoutine(Graphic g, Color bright, Color dim, int pulses)

    {

        if (g == null) yield break;

        for (var p = 0; p < pulses; p++)

        {

            var t = (p % 2) == 0 ? 1f : 0f;

            g.color = HighlighterTint(Color.Lerp(dim, bright, t));

            yield return new WaitForSecondsRealtime(optionBlinkPulseSeconds);

        }



        g.color = HighlighterTint(bright);

    }



    IEnumerator QuestionTransitionRoutine()

    {

        EnsureMainTransitionRoot();

        if (_mainCanvasGroup == null || _mainRect == null)

            yield break;



        var half = Mathf.Max(0.05f, questionTransitionHalfSeconds);

        float el;



        el = 0f;

        while (el < half)

        {

            el += Time.unscaledDeltaTime;

            var u = Mathf.Clamp01(el / half);

            _mainCanvasGroup.alpha = Mathf.Lerp(1f, transitionFadeOutAlpha, u);

            var s = Mathf.Lerp(1f, transitionMinScale, u);

            _mainRect.localScale = _mainBaseScale * s;

            yield return null;

        }



        ClearQuestionAndOptionTextsForTransition();



        el = 0f;

        while (el < half)

        {

            el += Time.unscaledDeltaTime;

            var u = Mathf.Clamp01(el / half);

            _mainCanvasGroup.alpha = Mathf.Lerp(transitionFadeOutAlpha, 1f, u);

            var s = Mathf.Lerp(transitionMinScale, transitionPeakScale, u);

            _mainRect.localScale = _mainBaseScale * s;

            yield return null;

        }



        el = 0f;

        var settle = half * 0.45f;

        while (el < settle)

        {

            el += Time.unscaledDeltaTime;

            var u = Mathf.Clamp01(el / settle);

            _mainRect.localScale = Vector3.Lerp(_mainBaseScale * transitionPeakScale, _mainBaseScale, u);

            yield return null;

        }



        _mainCanvasGroup.alpha = 1f;

        _mainRect.localScale = _mainBaseScale;

    }



    void ClearQuestionAndOptionTextsForTransition()

    {

        if (questionText != null)

            questionText.text = string.Empty;

        for (var i = 0; i < 4; i++)

        {

            if (optionLabels[i] != null)

                optionLabels[i].text = string.Empty;

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

        RuntimeGeneratedUiStyle.ApplyPanel(pImg);

        var prt = panel.GetComponent<RectTransform>();

        prt.anchorMin = new Vector2(0.5f, 0.5f);

        prt.anchorMax = new Vector2(0.5f, 0.5f);

        prt.pivot = new Vector2(0.5f, 0.5f);

        prt.sizeDelta = new Vector2(580f, 340f);

        prt.anchoredPosition = Vector2.zero;

        if (!RuntimeGeneratedUiStyle.UsePremiumChrome())

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

        RuntimeGeneratedUiStyle.ApplyButton(bImg);

        var brt = btnGo.GetComponent<RectTransform>();

        brt.anchorMin = new Vector2(0.26f, 0.09f);

        brt.anchorMax = new Vector2(0.74f, 0.27f);

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

        GetComponent<UIButtonClickFeedback>()?.RegisterNewButtonsInHierarchy();

    }



    static void StretchFull(RectTransform rt)

    {

        rt.anchorMin = Vector2.zero;

        rt.anchorMax = Vector2.one;

        rt.offsetMin = Vector2.zero;

        rt.offsetMax = Vector2.zero;

    }



    static Sprite GetWhiteSprite() => RuntimeGeneratedUiStyle.WhiteFallbackSprite();



    void GoToMenu()

    {

        quizVoiceDirector?.CancelSpeech();

        gameplaySfx?.StopTimerLoop();

        if (string.IsNullOrEmpty(menuSceneName)) return;

        SceneManager.LoadScene(menuSceneName);

    }

}


