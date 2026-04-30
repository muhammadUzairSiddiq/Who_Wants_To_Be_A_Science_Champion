using System.Collections;
using UnityEngine;

/// <summary>
/// Gameplay sound effects. Requires an <see cref="AudioListener"/> in the scene (added automatically if missing).
/// Voice/TTS bypasses Unity audio — if only VO works, you were missing a listener or clips were on a different GameplaySfx than the controller picked up.
/// </summary>
[DisallowMultipleComponent]
public class GameplaySfx : MonoBehaviour
{
    [Tooltip("Sequential clips (round intro, question lead-in).")]
    [SerializeField] AudioSource blockingSource;

    [Tooltip("One-shots: lifeline, correct, wrong.")]
    [SerializeField] AudioSource oneShotSource;

    [Tooltip("Looping timer bed.")]
    [SerializeField] AudioSource timerLoopSource;

    [Tooltip("Timer loop volume while lifeline / correct / wrong Unity clips play.")]
    [SerializeField, Range(0f, 1f)]
    float timerBedVolumeDuringSfxConflict = 0.22f;

    [Header("Clips (Inspector or Resources: Audio / QuizMasterSfx / GameplayAudio)")]
    [SerializeField] AudioClip roundIntroClip;

    [Tooltip("If true, Initial Sound only when ROUND 01 shows (ladder index 0).")]
    [SerializeField] bool roundIntroOnlyOnRound01 = true;

    [SerializeField] AudioClip questionLeadInClip;
    [SerializeField] AudioClip lifelineClip;
    [SerializeField] AudioClip correctAnswerClip;
    [SerializeField] AudioClip wrongAnswerClip;
    [SerializeField] AudioClip timerLoopClip;

    [Tooltip("Short click for UI buttons (optional). Auto-filled from Assets/Audio in Editor Play Mode if named with Click/Button/Tap.")]
    [SerializeField] AudioClip uiButtonClickClip;

#if UNITY_EDITOR
    static bool _loggedMissingClips;
#endif

    bool _initialized;

    float _timerNominalVolume = 1f;

    float _timerDuckUntilUnscaled = float.NegativeInfinity;

    void Awake() => InitializeAudio();

    void Update()
    {
        if (timerLoopSource == null || !timerLoopSource.isPlaying) return;
        timerLoopSource.volume =
            Time.unscaledTime < _timerDuckUntilUnscaled ? timerBedVolumeDuringSfxConflict : _timerNominalVolume;
    }

    /// <summary>Called from <see cref="GameplaySceneController"/> Start so listeners/clips resolve after all Awakes.</summary>
    public void InitializeAudio()
    {
        if (_initialized) return;
        _initialized = true;

        EnsureAudioListenerPresent();
        EnsureSources();
        TryLoadClipsFromResources();
        TryLoadClipsFromAssetsAudioFolder();
#if UNITY_EDITOR
        if (!_loggedMissingClips && !HasAnyClipConfigured())
        {
            _loggedMissingClips = true;
            Debug.LogWarning(
                "GameplaySfx: No clips found on '" + gameObject.name +
                "'. In the Editor, clips under Assets/Audio/ are picked up automatically during Play. " +
                "For standalone/WebGL builds you must either assign clips here in the Inspector, OR put copies under Assets/Resources/Audio/ (or QuizMasterSfx/ GameplayAudio/). " +
                "Unity cannot Resources-load files that sit only in Assets/Audio — that folder is not special unless referenced.");
        }
#endif
    }

    /// <summary>True if any clip field is set (Inspector or Resources).</summary>
    public bool HasAnyClipConfigured() =>
        roundIntroClip != null || questionLeadInClip != null || lifelineClip != null ||
        correctAnswerClip != null || wrongAnswerClip != null || timerLoopClip != null ||
        uiButtonClickClip != null;

    static void EnsureAudioListenerPresent()
    {
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var l in listeners)
        {
            if (l != null && l.enabled && l.gameObject.activeInHierarchy)
                return;
        }

        var cam = Camera.main;
        if (cam != null)
        {
            var al = cam.GetComponent<AudioListener>();
            if (al != null)
            {
                al.enabled = true;
                return;
            }

            cam.gameObject.AddComponent<AudioListener>();
            return;
        }

        var host = Object.FindFirstObjectByType<GameplaySceneController>();
        if (host != null)
        {
            var al = host.GetComponent<AudioListener>();
            if (al != null)
            {
                al.enabled = true;
                return;
            }

            host.gameObject.AddComponent<AudioListener>();
            return;
        }

        new GameObject("AudioListener (auto)").AddComponent<AudioListener>();
    }

    void EnsureSources()
    {
        void Prep(AudioSource s)
        {
            if (s == null) return;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 1f;
            s.mute = false;
            s.ignoreListenerPause = true;
        }

        if (blockingSource == null)
            blockingSource = GetComponent<AudioSource>();
        if (blockingSource == null)
        {
            blockingSource = gameObject.AddComponent<AudioSource>();
            Prep(blockingSource);
        }
        else Prep(blockingSource);

        if (oneShotSource == null)
        {
            var go = new GameObject("SfxOneShot");
            go.transform.SetParent(transform, false);
            oneShotSource = go.AddComponent<AudioSource>();
            Prep(oneShotSource);
        }
        else Prep(oneShotSource);

        if (timerLoopSource == null)
        {
            var go = new GameObject("TimerLoopSfx");
            go.transform.SetParent(transform, false);
            timerLoopSource = go.AddComponent<AudioSource>();
            Prep(timerLoopSource);
        }
        else Prep(timerLoopSource);

        if (timerLoopSource != null)
            _timerNominalVolume = timerLoopSource.volume;
    }

    void ExtendTimerBedDuckForClip(AudioClip clip)
    {
        if (clip == null || timerLoopClip == null || timerLoopSource == null || !timerLoopSource.isPlaying)
            return;
        var end = Time.unscaledTime + Mathf.Max(0.04f, clip.length);
        if (end > _timerDuckUntilUnscaled)
            _timerDuckUntilUnscaled = end;
    }

    /// <summary>
    /// Loads from Resources subfolders (QuizMasterSfx, Audio, GameplayAudio) by fuzzy clip.name match,
    /// then tries exact resource paths under QuizMasterSfx/.
    /// </summary>
    void TryLoadClipsFromResources()
    {
        foreach (var folder in new[] { "QuizMasterSfx", "Audio", "GameplayAudio" })
        {
            foreach (var c in Resources.LoadAll<AudioClip>(folder))
            {
                if (c == null) continue;
                ApplyClipByHeuristic(c);
            }
        }

        if (roundIntroClip == null)
            roundIntroClip = LoadExact("Initial Sound") ?? LoadExact("InitialSound");
        if (questionLeadInClip == null)
            questionLeadInClip = LoadExact("Question sound effect") ?? LoadExact("QuestionSound") ?? LoadExact("Question");
        if (lifelineClip == null)
            lifelineClip = LoadExact("Lifeline Button") ?? LoadExact("LifelineButton");
        if (timerLoopClip == null)
            timerLoopClip = LoadExact("Timer Sound") ?? LoadExact("TimerSound");
        if (correctAnswerClip == null)
            correctAnswerClip = LoadExact("Correct") ?? LoadExact("CorrectAnswer");
        if (wrongAnswerClip == null)
            wrongAnswerClip = LoadExact("Wrong") ?? LoadExact("WrongAnswer");

        static AudioClip LoadExact(string resourceFileNameNoExt) =>
            Resources.Load<AudioClip>("QuizMasterSfx/" + resourceFileNameNoExt);
    }

    /// <summary>
    /// Unity only includes <see cref="Resources"/> paths and referenced assets in builds.
    /// Editor Play Mode can still resolve loose clips under Assets/Audio via the AssetDatabase.
    /// </summary>
    void TryLoadClipsFromAssetsAudioFolder()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;

        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            var c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (c != null)
                ApplyClipByHeuristic(c);
        }
#endif
    }

    static string NormalizeClipKey(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        return name.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();
    }

    void ApplyClipByHeuristic(AudioClip c)
    {
        var k = NormalizeClipKey(c.name);

        if (wrongAnswerClip == null &&
            (k.Contains("wrong") || k.Contains("incorrect") || k.Contains("fail")))
        {
            wrongAnswerClip = c;
            return;
        }

        if (correctAnswerClip == null &&
            k.Contains("correct") &&
            !k.Contains("incorrect"))
        {
            correctAnswerClip = c;
            return;
        }

        if (roundIntroClip == null &&
            (k.Contains("initial") || k.Contains("roundintro") ||
             (k.Contains("intro") && !k.Contains("question"))))
        {
            roundIntroClip = c;
            return;
        }

        if (questionLeadInClip == null &&
            ((k.Contains("question") && !k.Contains("timer")) || k.Contains("questionsting")))
        {
            questionLeadInClip = c;
            return;
        }

        if (timerLoopClip == null &&
            (k.Contains("timer") || k.Contains("countdown") || k.Contains("tick")) &&
            !k.Contains("question"))
        {
            timerLoopClip = c;
            return;
        }

        if (lifelineClip == null &&
            (k.Contains("lifeline") || k.Contains("5050") || k.Contains("fiftyfifty") ||
             k.Contains("audience") || k.Contains("phone")))
        {
            lifelineClip = c;
            return;
        }

        if (uiButtonClickClip == null &&
            (k.Contains("click") || k.Contains("button") || k == "tap" || k.Contains("ui")))
        {
            uiButtonClickClip = c;
        }
    }

    public IEnumerator PlayRoundBannerIntroAndWait(int ladderIndexZeroBased, float fallbackSecondsWhenNoIntroClip)
    {
        var playIntro = roundIntroClip != null && (!roundIntroOnlyOnRound01 || ladderIndexZeroBased == 0);
        if (playIntro)
        {
            yield return PlayBlocking(roundIntroClip);
            yield break;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, fallbackSecondsWhenNoIntroClip));
    }

    public IEnumerator PlayQuestionLeadInAndWait()
    {
        if (questionLeadInClip == null) yield break;
        yield return PlayBlocking(questionLeadInClip);
    }

    IEnumerator PlayBlocking(AudioClip clip)
    {
        if (clip == null || blockingSource == null) yield break;

        blockingSource.Stop();
        blockingSource.loop = false;
        blockingSource.clip = clip;
        blockingSource.Play();

        yield return null;

        var minEnd = Time.unscaledTime + Mathf.Max(clip.length, 0.05f);
        var hardCap = Time.unscaledTime + Mathf.Min(clip.length + 0.5f, 120f);
        while ((blockingSource.isPlaying || Time.unscaledTime < minEnd) && Time.unscaledTime < hardCap)
            yield return null;

        blockingSource.Stop();
        blockingSource.clip = null;
    }

    public void StartTimerLoop()
    {
        StopTimerLoop();
        _timerDuckUntilUnscaled = float.NegativeInfinity;
        if (timerLoopClip == null || timerLoopSource == null) return;
        timerLoopSource.loop = true;
        timerLoopSource.clip = timerLoopClip;
        timerLoopSource.volume = _timerNominalVolume;
        timerLoopSource.Play();
    }

    public void StopTimerLoop()
    {
        _timerDuckUntilUnscaled = float.NegativeInfinity;
        if (timerLoopSource == null) return;
        timerLoopSource.Stop();
        timerLoopSource.clip = null;
        timerLoopSource.volume = _timerNominalVolume;
    }

    public void PlayLifeline()
    {
        if (lifelineClip != null && oneShotSource != null)
        {
            ExtendTimerBedDuckForClip(lifelineClip);
            oneShotSource.PlayOneShot(lifelineClip);
        }
    }

    public void PlayAnswerJudgement(bool correct)
    {
        var c = correct ? correctAnswerClip : wrongAnswerClip;
        if (c != null && oneShotSource != null)
        {
            ExtendTimerBedDuckForClip(c);
            oneShotSource.PlayOneShot(c);
        }
    }

    public void PlayUiButtonClick()
    {
        if (uiButtonClickClip != null && oneShotSource != null)
            oneShotSource.PlayOneShot(uiButtonClickClip);
    }
}
