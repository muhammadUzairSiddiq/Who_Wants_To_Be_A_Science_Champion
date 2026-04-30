using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Prize ladder colours, per-team chances / elimination, score text updates, and session end (level 15 → LevelCompleted).
/// </summary>
public class GameplayRoundEconomy : MonoBehaviour
{
    const int MaxChances = 3;

    static readonly Color PrizeYellowRgb = new(1f, 0.9f, 0.28f);
    static readonly Color PrizeGreenRgb = new(0.22f, 0.92f, 0.42f);

    [Tooltip("Alpha on ladderprize overlay; lower = more see-through for text behind.")]
    [SerializeField, Range(0.06f, 0.55f)]
    float prizeOverlayAlpha = 0.16f;

    [SerializeField] string levelCompletedSceneName = "LevelCompleted";
    [SerializeField] string menuSceneName = "Menu";

    Transform _teamsRoot;
    Transform _canvasRoot;

    Graphic[] _prizeGraphics = new Graphic[QuizScoreLadder.LevelCount];

    /// <summary>First rung still in play (yellow). Rungs below are green. Does not advance on wrong answers.</summary>
    int _prizeCursor;

    TMP_Text[] _teamScoreTexts = new TMP_Text[4];
    TMP_Text[] _teamNameTexts = new TMP_Text[4];
    readonly List<GameObject>[] _teamChanceObjects = new List<GameObject>[4];
    readonly int[] _teamChancesLeft = new int[4];
    readonly bool[] _teamEliminated = new bool[4];

    GameObject[] _soloChanceObjects = new GameObject[3];
    int _soloChancesLeft = MaxChances;
    TMP_Text _soloScoreText;

    GameObject _eliminationOverlay;
    TMP_Text _eliminationBody;
    Button _eliminationOkButton;

    public bool RunEnded { get; private set; }

    public void Initialize(Transform teamsDetailsRoot, Transform canvasRoot)
    {
        _teamsRoot = teamsDetailsRoot;
        _canvasRoot = canvasRoot;
        ResolvePrizeLadder();
        ResolveTeamScoresAndNames();
        ResolveTeamChances();
        ResolveSoloChancesAndScore();
        RefreshPrizeLadderDisplay();
    }

    public void OnRunStarted(bool teamPlay)
    {
        RunEnded = false;
        _prizeCursor = 0;

        for (var i = 0; i < 4; i++)
        {
            _teamEliminated[i] = false;
            _teamChancesLeft[i] = 0;
            if (_teamsRoot == null || i >= _teamsRoot.childCount) continue;
            var row = _teamsRoot.GetChild(i);
            if (!row.gameObject.activeSelf) continue;
            _teamChancesLeft[i] = MaxChances;
            ResetChanceRow(_teamChanceObjects[i]);
        }

        _soloChancesLeft = MaxChances;
        ResetChanceRowList(_soloChanceObjects);
        ShowSoloChances(!teamPlay);

        RefreshPrizeLadderDisplay();
    }

    static void ResetChanceRow(List<GameObject> list)
    {
        if (list == null) return;
        foreach (var go in list)
        {
            if (go != null)
                go.SetActive(true);
        }
    }

    static void ResetChanceRowList(GameObject[] arr)
    {
        if (arr == null) return;
        foreach (var go in arr)
        {
            if (go != null)
                go.SetActive(true);
        }
    }

    void ShowSoloChances(bool show)
    {
        foreach (var go in _soloChanceObjects)
        {
            if (go != null)
                go.SetActive(show);
        }
    }

    public bool IsTeamEliminated(int teamIndex) =>
        teamIndex >= 0 && teamIndex < 4 && _teamEliminated[teamIndex];

    public IEnumerator ProcessAnswerAfterFeedback(
        bool correct,
        int questionIndexZeroBased,
        int selectedTeamIndex,
        bool teamPlay,
        Action refreshTeamPickButtons)
    {
        var level = questionIndexZeroBased + 1;
        if (level < 1 || level > QuizScoreLadder.LevelCount)
            yield break;

        if (correct)
        {
            var pts = QuizScoreLadder.GetPointsForLevel(level);
            if (teamPlay && selectedTeamIndex >= 0 && selectedTeamIndex < 4)
                AddScoreToTeam(selectedTeamIndex, pts);
            else
                AddScoreSolo(pts);

            _prizeCursor = Mathf.Min(_prizeCursor + 1, QuizScoreLadder.LevelCount);
        }
        else
        {
            if (teamPlay && selectedTeamIndex >= 0 && selectedTeamIndex < 4)
                yield return ConsumeTeamChance(selectedTeamIndex, refreshTeamPickButtons);
            else
                yield return ConsumeSoloChance(refreshTeamPickButtons);
        }

        RefreshPrizeLadderDisplay();

        if (questionIndexZeroBased >= QuizScoreLadder.LevelCount - 1)
        {
            RunEnded = true;
            if (teamPlay)
                BuildAndSaveTeamPodiumTopScores();
            else
                BuildAndSaveSoloPodium();

            SceneManager.LoadScene(string.IsNullOrEmpty(levelCompletedSceneName) ? "LevelCompleted" : levelCompletedSceneName);
            yield break;
        }
    }

    public void RefreshPrizeLadderDisplay()
    {
        for (var i = 0; i < QuizScoreLadder.LevelCount; i++)
        {
            var g = _prizeGraphics[i];
            if (g == null) continue;
            if (i < _prizeCursor)
            {
                g.gameObject.SetActive(true);
                g.color = new Color(PrizeGreenRgb.r, PrizeGreenRgb.g, PrizeGreenRgb.b, prizeOverlayAlpha);
            }
            else if (i == _prizeCursor && _prizeCursor < QuizScoreLadder.LevelCount)
            {
                g.gameObject.SetActive(true);
                g.color = new Color(PrizeYellowRgb.r, PrizeYellowRgb.g, PrizeYellowRgb.b, prizeOverlayAlpha);
            }
            else
            {
                g.gameObject.SetActive(false);
            }
        }
    }

    void ResolvePrizeLadder()
    {
        if (_canvasRoot == null) return;
        var main = _canvasRoot.Find("Main");
        if (main == null) return;

        var list = new List<(Graphic g, int ord)>();
        foreach (var rt in main.GetComponentsInChildren<Transform>(true))
        {
            if (rt == null) continue;
            var ord = ParsePrizeOrdinal(rt.name);
            if (ord < 0) continue;
            var g = ResolvePrizeHighlightGraphic(rt);
            if (g == null) continue;
            list.Add((g, ord));
        }

        list.Sort((a, b) => a.ord.CompareTo(b.ord));
        for (var i = 0; i < QuizScoreLadder.LevelCount && i < list.Count; i++)
            _prizeGraphics[i] = list[i].g;
    }

    static Graphic ResolvePrizeHighlightGraphic(Transform prizeRow)
    {
        foreach (Transform c in prizeRow)
        {
            if (c.name.IndexOf("ladderprize", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var hit = c.GetComponent<Graphic>();
                if (hit != null) return hit;
            }
        }

        return prizeRow.GetComponent<Graphic>();
    }

    static int ParsePrizeOrdinal(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;
        if (string.Equals(name.Trim(), "PRIZE 01", StringComparison.OrdinalIgnoreCase))
            return 0;
        var m = Regex.Match(name.Trim(), @"^PRIZE\s*01\s*\((\d+)\)\s*$", RegexOptions.IgnoreCase);
        if (!m.Success) return -1;
        return int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : -1;
    }

    void ResolveTeamScoresAndNames()
    {
        if (_teamsRoot == null) return;
        for (var i = 0; i < 4 && i < _teamsRoot.childCount; i++)
        {
            var row = _teamsRoot.GetChild(i);
            _teamScoreTexts[i] = FindTmpByNameContains(row, "TEAM SCORE");
            _teamNameTexts[i] = FindTmpByNameContains(row, "TEAM NAME");
        }
    }

    static TMP_Text FindTmpByNameContains(Transform row, string contains)
    {
        foreach (var t in row.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == null) continue;
            if (t.gameObject.name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                return t;
        }

        return null;
    }

    void ResolveTeamChances()
    {
        if (_teamsRoot == null) return;
        for (var i = 0; i < 4 && i < _teamsRoot.childCount; i++)
        {
            var row = _teamsRoot.GetChild(i);
            var list = new List<GameObject>();
            foreach (Transform c in row)
            {
                if (c.name.IndexOf("chance", StringComparison.OrdinalIgnoreCase) >= 0)
                    list.Add(c.gameObject);
            }

            list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            while (list.Count > MaxChances)
                list.RemoveAt(list.Count - 1);
            _teamChanceObjects[i] = list;
        }
    }

    void ResolveSoloChancesAndScore()
    {
        if (_canvasRoot == null) return;
        var tmp = new List<GameObject>();

        Transform timerRoot = null;
        foreach (var t in _canvasRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            if (string.Equals(t.gameObject.name, "TIME", StringComparison.OrdinalIgnoreCase) ||
                t.gameObject.name.IndexOf("timer", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                timerRoot = t;
                break;
            }
        }

        if (timerRoot != null)
        {
            foreach (Transform c in timerRoot)
            {
                if (c.name.IndexOf("chance", StringComparison.OrdinalIgnoreCase) >= 0)
                    tmp.Add(c.gameObject);
            }

            tmp.Sort((a, b) =>
            {
                var ra = a.GetComponent<RectTransform>();
                var rb = b.GetComponent<RectTransform>();
                var ax = ra != null ? ra.anchoredPosition.x : 0f;
                var bx = rb != null ? rb.anchoredPosition.x : 0f;
                return ax.CompareTo(bx);
            });
        }

        if (tmp.Count == 0)
        {
            Transform soloRoot = null;
            foreach (var t in _canvasRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name.IndexOf("Single Player Chances", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    t.childCount > 0)
                {
                    soloRoot = t;
                    break;
                }
            }

            if (soloRoot != null)
            {
                foreach (Transform c in soloRoot)
                {
                    if (c.name.IndexOf("chance", StringComparison.OrdinalIgnoreCase) >= 0)
                        tmp.Add(c.gameObject);
                }

                tmp.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            }
        }

        for (var i = 0; i < MaxChances && i < tmp.Count; i++)
            _soloChanceObjects[i] = tmp[i];

        _soloScoreText = FindFirstTeamScoreOnCanvas();
    }

    TMP_Text FindFirstTeamScoreOnCanvas()
    {
        if (_canvasRoot == null) return null;
        TMP_Text fallback = null;
        foreach (var t in _canvasRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == null) continue;
            if (t.gameObject.name.IndexOf("TEAM SCORE", StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (t.gameObject.activeInHierarchy) return t;
            if (fallback == null) fallback = t;
        }

        return fallback;
    }

    void AddScoreToTeam(int teamIndex, int delta)
    {
        var tmp = teamIndex >= 0 && teamIndex < 4 ? _teamScoreTexts[teamIndex] : null;
        if (tmp == null) return;
        var cur = ParseScore(tmp.text);
        tmp.text = (cur + delta).ToString(CultureInfo.InvariantCulture);
    }

    void AddScoreSolo(int delta)
    {
        if (_soloScoreText == null) return;
        var cur = ParseScore(_soloScoreText.text);
        _soloScoreText.text = (cur + delta).ToString(CultureInfo.InvariantCulture);
    }

    static int ParseScore(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        var digits = Regex.Replace(raw.Trim(), @"[^\d\-]", string.Empty);
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    IEnumerator ConsumeTeamChance(int teamIndex, Action refreshTeamPick)
    {
        if (teamIndex < 0 || teamIndex >= 4 || _teamEliminated[teamIndex]) yield break;

        _teamChancesLeft[teamIndex] = Mathf.Max(0, _teamChancesLeft[teamIndex] - 1);
        HideChanceForRemaining(_teamChanceObjects[teamIndex], _teamChancesLeft[teamIndex]);

        if (_teamChancesLeft[teamIndex] > 0)
            yield break;

        _teamEliminated[teamIndex] = true;
        var teamName = GetTeamDisplayName(teamIndex);
        yield return ShowEliminationDialog($"{teamName} will be eliminated.");

        if (_teamsRoot != null && teamIndex < _teamsRoot.childCount)
        {
            var row = _teamsRoot.GetChild(teamIndex);
            var btn = row.GetComponent<Button>();
            if (btn != null)
                btn.interactable = false;
        }

        refreshTeamPick?.Invoke();

        if (CountRemainingTeams() == 1)
        {
            BuildAndSaveTeamPodiumLastStanding();
            RunEnded = true;
            var w = FindSoleSurvivorTeamIndex();
            if (w >= 0)
                yield return ShowEliminationDialog($"{GetTeamDisplayName(w)} wins the level!");

            SceneManager.LoadScene(string.IsNullOrEmpty(levelCompletedSceneName) ? "LevelCompleted" : levelCompletedSceneName);
        }
    }

    IEnumerator ConsumeSoloChance(Action refreshTeamPick)
    {
        _soloChancesLeft = Mathf.Max(0, _soloChancesLeft - 1);
        var list = new List<GameObject>();
        foreach (var go in _soloChanceObjects)
        {
            if (go != null)
                list.Add(go);
        }

        HideChanceForRemaining(list, _soloChancesLeft);

        if (_soloChancesLeft > 0)
            yield break;

        yield return ShowEliminationDialog(
            "You are out of chances.\nTap OK to return to the main menu.");
        RunEnded = true;
        SceneManager.LoadScene(string.IsNullOrEmpty(menuSceneName) ? "Menu" : menuSceneName);
    }

    static void HideChanceForRemaining(List<GameObject> chanceObjects, int remainingAfterDecrement)
    {
        if (chanceObjects == null || chanceObjects.Count == 0) return;
        var idx = Mathf.Clamp(remainingAfterDecrement, 0, chanceObjects.Count - 1);
        if (chanceObjects[idx] != null)
            chanceObjects[idx].SetActive(false);
    }

    static void HideChanceForRemaining(GameObject[] chanceObjects, int remainingAfterDecrement)
    {
        if (chanceObjects == null || chanceObjects.Length == 0) return;
        var idx = Mathf.Clamp(remainingAfterDecrement, 0, chanceObjects.Length - 1);
        if (chanceObjects[idx] != null)
            chanceObjects[idx].SetActive(false);
    }

    string GetTeamDisplayName(int teamIndex)
    {
        if (teamIndex >= 0 && teamIndex < 4 && _teamNameTexts[teamIndex] != null)
        {
            var t = _teamNameTexts[teamIndex].text?.Trim();
            if (!string.IsNullOrEmpty(t)) return t;
        }

        return "Team " + (char)('A' + teamIndex);
    }

    IEnumerator ShowEliminationDialog(string message)
    {
        EnsureEliminationOverlay();
        if (_eliminationBody != null)
            _eliminationBody.text = message;
        var done = false;
        void OnOk()
        {
            done = true;
            if (_eliminationOverlay != null)
                _eliminationOverlay.SetActive(false);
            if (_eliminationOkButton != null)
                _eliminationOkButton.onClick.RemoveAllListeners();
        }

        if (_eliminationOkButton != null)
            _eliminationOkButton.onClick.AddListener(OnOk);

        if (_eliminationOverlay != null)
            _eliminationOverlay.SetActive(true);

        while (!done)
            yield return null;
    }

    void EnsureEliminationOverlay()
    {
        if (_eliminationOverlay != null) return;

        var white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        var root = new GameObject("EliminationOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(_canvasRoot, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 14000;
        Stretch(root.GetComponent<RectTransform>());

        var dim = new GameObject("Dim", typeof(Image));
        dim.transform.SetParent(root.transform, false);
        var dimImg = dim.GetComponent<Image>();
        dimImg.sprite = white;
        dimImg.color = new Color(0.04f, 0.02f, 0.12f, 0.88f);
        dimImg.raycastTarget = true;
        Stretch(dim.GetComponent<RectTransform>());

        var panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var pImg = panel.GetComponent<Image>();
        pImg.sprite = white;
        pImg.color = new Color(0.14f, 0.08f, 0.24f, 1f);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(520f, 220f);
        prt.anchoredPosition = Vector2.zero;
        panel.AddComponent<Outline>().effectColor = new Color(0.92f, 0.72f, 0.18f, 1f);

        var bodyGo = new GameObject("Body", typeof(TextMeshProUGUI));
        bodyGo.transform.SetParent(panel.transform, false);
        _eliminationBody = bodyGo.GetComponent<TextMeshProUGUI>();
        _eliminationBody.fontSize = 24f;
        _eliminationBody.alignment = TextAlignmentOptions.Center;
        _eliminationBody.color = Color.white;
        _eliminationBody.textWrappingMode = TextWrappingModes.Normal;
        var brt = _eliminationBody.rectTransform;
        brt.anchorMin = new Vector2(0.08f, 0.35f);
        brt.anchorMax = new Vector2(0.92f, 0.88f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;

        var btnGo = new GameObject("OK Button", typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panel.transform, false);
        var bImg = btnGo.GetComponent<Image>();
        bImg.sprite = white;
        bImg.color = new Color(0.42f, 0.28f, 0.62f, 1f);
        var okRt = btnGo.GetComponent<RectTransform>();
        okRt.anchorMin = new Vector2(0.32f, 0.1f);
        okRt.anchorMax = new Vector2(0.68f, 0.26f);
        okRt.offsetMin = okRt.offsetMax = Vector2.zero;
        _eliminationOkButton = btnGo.GetComponent<Button>();

        var lblGo = new GameObject("Label", typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(btnGo.transform, false);
        var lbl = lblGo.GetComponent<TextMeshProUGUI>();
        lbl.text = "OK";
        lbl.fontSize = 22f;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = Color.white;
        Stretch(lbl.rectTransform);

        _eliminationOverlay = root;
        root.SetActive(false);
        GetComponent<UIButtonClickFeedback>()?.RegisterNewButtonsInHierarchy();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    bool IsTeamRowActive(int teamIndex)
    {
        if (_teamsRoot == null || teamIndex < 0 || teamIndex >= _teamsRoot.childCount) return false;
        return _teamsRoot.GetChild(teamIndex).gameObject.activeSelf;
    }

    int ReadTeamScore(int teamIndex)
    {
        if (teamIndex < 0 || teamIndex >= 4) return 0;
        var tmp = _teamScoreTexts[teamIndex];
        return tmp == null ? 0 : ParseScore(tmp.text);
    }

    int CountRemainingTeams()
    {
        var n = 0;
        for (var i = 0; i < 4; i++)
        {
            if (!IsTeamRowActive(i)) continue;
            if (_teamEliminated[i]) continue;
            n++;
        }

        return n;
    }

    int FindSoleSurvivorTeamIndex()
    {
        var found = -1;
        for (var i = 0; i < 4; i++)
        {
            if (!IsTeamRowActive(i)) continue;
            if (_teamEliminated[i]) continue;
            if (found >= 0) return -1;
            found = i;
        }

        return found;
    }

    void BuildAndSaveTeamPodiumLastStanding()
    {
        var survivor = FindSoleSurvivorTeamIndex();
        var eliminated = new List<(int score, string name)>();
        for (var i = 0; i < 4; i++)
        {
            if (!IsTeamRowActive(i)) continue;
            if (!_teamEliminated[i]) continue;
            eliminated.Add((ReadTeamScore(i), GetTeamDisplayName(i)));
        }

        eliminated.Sort((a, b) =>
        {
            var c = b.score.CompareTo(a.score);
            return c != 0 ? c : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        });

        var wName = survivor >= 0 ? GetTeamDisplayName(survivor) : "—";
        var wScore = survivor >= 0 ? ReadTeamScore(survivor) : 0;
        LevelCompletionResults.Save(new LevelCompletionPayload
        {
            teamMode = true,
            winnerName = wName,
            winnerScore = wScore,
            runnerName = eliminated.Count > 0 ? eliminated[0].name : "—",
            runnerScore = eliminated.Count > 0 ? eliminated[0].score : 0,
            thirdName = eliminated.Count > 1 ? eliminated[1].name : "—",
            thirdScore = eliminated.Count > 1 ? eliminated[1].score : 0
        });
    }

    void BuildAndSaveTeamPodiumTopScores()
    {
        var entries = new List<(int idx, int score, string name)>();
        for (var i = 0; i < 4; i++)
        {
            if (!IsTeamRowActive(i)) continue;
            entries.Add((i, ReadTeamScore(i), GetTeamDisplayName(i)));
        }

        entries.Sort((a, b) =>
        {
            var c = b.score.CompareTo(a.score);
            return c != 0 ? c : a.idx.CompareTo(b.idx);
        });

        void Slot(int rank, out string nm, out int sc)
        {
            if (rank < entries.Count)
            {
                nm = entries[rank].name;
                sc = entries[rank].score;
            }
            else
            {
                nm = "—";
                sc = 0;
            }
        }

        Slot(0, out var wName, out var wSc);
        Slot(1, out var rName, out var rSc);
        Slot(2, out var tName, out var tSc);

        LevelCompletionResults.Save(new LevelCompletionPayload
        {
            teamMode = true,
            winnerName = wName,
            winnerScore = wSc,
            runnerName = rName,
            runnerScore = rSc,
            thirdName = tName,
            thirdScore = tSc
        });
    }

    void BuildAndSaveSoloPodium()
    {
        var sc = _soloScoreText != null ? ParseScore(_soloScoreText.text) : 0;
        var nm = StudentCredentials.GetSavedStudentName()?.Trim();
        if (string.IsNullOrEmpty(nm)) nm = "Player";

        var prizeLine = sc.ToString("N0", CultureInfo.InvariantCulture) + " pts";
        LevelCompletionResults.Save(new LevelCompletionPayload
        {
            teamMode = false,
            winnerName = nm,
            winnerScore = sc,
            soloPrizeSummary = prizeLine,
            runnerName = string.Empty,
            runnerScore = 0,
            thirdName = string.Empty,
            thirdScore = 0
        });
    }
}
