using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Host-side narrator for quiz flow. WebGL uses <see cref="QuizMasterWebSpeechBridge"/> (browser voices).
/// True celebrity / cloned voices are not available from the browser API—voice selection is automatic (prefers male / Indian English when listed).<br/>
/// In the Unity Editor on Windows, narration runs via PowerShell + System.Speech in a separate process (Unity Editor cannot load Framework Speech inside its own runtime reliably).
/// </summary>
[DisallowMultipleComponent]
public class QuizVoiceDirector : MonoBehaviour
{
    [Tooltip("Master switch for all narrator lines.")]
    [SerializeField] bool voiceEnabled = true;

    [Tooltip("Clear queued speech when a new question intro begins.")]
    [SerializeField] bool cancelSpeechOnNewQuestion = true;

    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (voiceEnabled)
            QuizMasterWebSpeechBridge.EnsureInitialized();
#endif
    }

    public void CancelSpeech()
    {
        if (!voiceEnabled) return;
        QuizMasterWebSpeechBridge.Cancel();
    }

    /// <summary>Call at the start of each question intro.</summary>
    public void OnNewQuestionStarting()
    {
        if (!voiceEnabled || !cancelSpeechOnNewQuestion) return;
        QuizMasterWebSpeechBridge.Cancel();
    }

    /// <summary>Waits until all queued narration has finished (Editor/WebGL).</summary>
    public IEnumerator WaitUntilSpeechIdle(float timeoutSeconds = 180f)
    {
        if (!voiceEnabled) yield break;
        yield return QuizMasterWebSpeechBridge.WaitUntilSpeechIdle(timeoutSeconds);
    }

    public void SpeakQuestion(string questionRaw)
    {
        if (!voiceEnabled) return;
        var q = SanitizeForSpeech(questionRaw);
        if (string.IsNullOrEmpty(q)) return;
        QuizMasterWebSpeechBridge.Enqueue(q, 0);
    }

    public void SpeakOption(int indexZeroTo3, string optionRaw)
    {
        if (!voiceEnabled) return;
        if (indexZeroTo3 < 0 || indexZeroTo3 > 3) return;
        var letter = (char)('A' + indexZeroTo3);
        var body = SanitizeForSpeech(optionRaw);
        if (string.IsNullOrEmpty(body)) body = "blank";
        QuizMasterWebSpeechBridge.Enqueue($"Option {letter}: {body}", 0);
    }

    /// <summary>Call immediately when the player locks an answer (before reveal animation).</summary>
    public void SpeakChosenOptionImmediate(int chosenIndex, QuizQuestionData data, bool teamMode, string teamDisplayNameOrEmpty)
    {
        if (!voiceEnabled) return;
        if (data == null || chosenIndex < 0 || chosenIndex > 3) return;

        var letter = (char)('A' + chosenIndex);
        var opt = SanitizeForSpeech(data.Options[chosenIndex]);
        if (string.IsNullOrEmpty(opt)) opt = "blank";

        string line;
        if (teamMode && !string.IsNullOrEmpty(teamDisplayNameOrEmpty))
            line = $"Team {SanitizeForSpeech(teamDisplayNameOrEmpty)} chose Option {letter}: {opt}.";
        else
            line = $"You chose Option {letter}: {opt}.";

        QuizMasterWebSpeechBridge.Enqueue(line, 0);
    }

    /// <summary>When the UI shows the final correct / incorrect result.</summary>
    public void SpeakJudgement(bool correct)
    {
        if (!voiceEnabled) return;
        QuizMasterWebSpeechBridge.Enqueue(
            correct ? "Yes! That is the correct answer!" : "No. That is the wrong answer.",
            correct ? 1 : 2);
    }

    /// <summary>After team confirms in the popup.</summary>
    public void SpeakTeamWillAnswer(string teamDisplayName)
    {
        if (!voiceEnabled) return;
        var nm = SanitizeForSpeech(teamDisplayName);
        if (string.IsNullOrEmpty(nm)) nm = "Your team";
        QuizMasterWebSpeechBridge.Enqueue($"Team {nm} will answer this question.", 0);
    }

    public void SpeakTimeUpReveal(QuizQuestionData data)
    {
        if (!voiceEnabled) return;
        if (data == null) return;
        var ci = Mathf.Clamp(data.CorrectOptionIndex, 0, 3);
        var letter = (char)('A' + ci);
        var ans = SanitizeForSpeech(data.Options[ci]);
        if (string.IsNullOrEmpty(ans)) ans = "blank";
        QuizMasterWebSpeechBridge.Enqueue($"Time is up. The correct answer was Option {letter}: {ans}.", 2);
    }

    public static string SanitizeForSpeech(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        var s = Regex.Replace(raw, "<[^>]+>", " ");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }
}
