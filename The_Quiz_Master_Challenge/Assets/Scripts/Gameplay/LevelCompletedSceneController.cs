using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Fills podium UI from <see cref="LevelCompletionResults"/> and returns to menu.</summary>
public class LevelCompletedSceneController : MonoBehaviour
{
    [SerializeField] string menuSceneName = "Menu";

    Button _nextButton;

    void Start()
    {
        if (!LevelCompletionResults.TryLoad(out var data) || data == null)
            data = new LevelCompletionPayload { teamMode = true, winnerName = "—", runnerName = "—", thirdName = "—" };

        if (data.teamMode)
        {
            ApplySlot(FindPanelRoot("TEAMS Winner"), data.winnerName, data.winnerScore);
            SetPanelActive("TEAMS Runner Up", true);
            SetPanelActive("TEAMS Third", true);
            ApplySlot(FindPanelRoot("TEAMS Runner Up"), data.runnerName, data.runnerScore);
            ApplySlot(FindPanelRoot("TEAMS Third"), data.thirdName, data.thirdScore);
        }
        else
        {
            SetPanelActive("TEAMS Runner Up", false);
            SetPanelActive("TEAMS Third", false);
            ApplySoloWinner(FindPanelRoot("TEAMS Winner"), data);
        }

        _nextButton = FindButtonByName("Next Button");
        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveAllListeners();
            _nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    void OnDestroy()
    {
        if (_nextButton != null)
            _nextButton.onClick.RemoveListener(OnNextClicked);
    }

    void OnNextClicked()
    {
        LevelCompletionResults.Clear();
        var name = string.IsNullOrEmpty(menuSceneName) ? "Menu" : menuSceneName;
        SceneManager.LoadScene(name);
    }

    Transform FindPanelRoot(string objectName)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t != null && string.Equals(t.gameObject.name, objectName, StringComparison.OrdinalIgnoreCase))
                return t;
        }

        return null;
    }

    void SetPanelActive(string objectName, bool on)
    {
        var root = FindPanelRoot(objectName);
        if (root != null)
            root.gameObject.SetActive(on);
    }

    static void ApplySlot(Transform root, string teamName, int score)
    {
        if (root == null) return;
        var nameTmp = FindTmpContains(root, "TEAM NAME");
        var scoreTmp = FindTmpContains(root, "TEAM SCORE");
        if (nameTmp != null)
            nameTmp.text = string.IsNullOrWhiteSpace(teamName) ? "—" : teamName.Trim();
        if (scoreTmp != null)
            scoreTmp.text = score.ToString("N0", CultureInfo.InvariantCulture);
    }

    static void ApplySoloWinner(Transform root, LevelCompletionPayload data)
    {
        if (root == null || data == null) return;
        var nameTmp = FindTmpContains(root, "TEAM NAME");
        var scoreTmp = FindTmpContains(root, "TEAM SCORE");
        var prizeTmp = FindTmpContains(root, "PRIZE");
        if (nameTmp != null)
            nameTmp.text = string.IsNullOrWhiteSpace(data.winnerName) ? "—" : data.winnerName.Trim();
        if (prizeTmp != null)
        {
            prizeTmp.gameObject.SetActive(true);
            prizeTmp.text = string.IsNullOrEmpty(data.soloPrizeSummary)
                ? data.winnerScore.ToString("N0", CultureInfo.InvariantCulture) + " pts"
                : data.soloPrizeSummary;
        }

        if (scoreTmp != null)
        {
            var sc = data.winnerScore.ToString("N0", CultureInfo.InvariantCulture);
            if (prizeTmp != null)
                scoreTmp.text = sc;
            else if (!string.IsNullOrEmpty(data.soloPrizeSummary))
                scoreTmp.text = sc + "\n<size=70%>" + data.soloPrizeSummary + "</size>";
            else
                scoreTmp.text = sc;
        }
    }

    static TMP_Text FindTmpContains(Transform root, string contains)
    {
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp != null && tmp.gameObject.name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                return tmp;
        }

        return null;
    }

    Button FindButtonByName(string exactOrContains)
    {
        foreach (var b in GetComponentsInChildren<Button>(true))
        {
            if (b == null) continue;
            if (string.Equals(b.gameObject.name, exactOrContains, StringComparison.OrdinalIgnoreCase))
                return b;
            if (b.gameObject.name.IndexOf(exactOrContains, StringComparison.OrdinalIgnoreCase) >= 0)
                return b;
        }

        return null;
    }
}
