using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// WebGL build: browser SpeechSynthesis (jslib).<br/>
/// Unity Editor (Windows): runs short PowerShell snippets that call System.Speech — works even when Unity Editor uses CoreCLR (in-process Framework DLL load fails there).<br/>
/// Do not add System.Speech.dll under Assets/Plugins.<br/>
/// Other Editor OS / standalone players: speech off unless WebGL.
/// </summary>
public static class QuizMasterWebSpeechBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern int QuizSpeech_Init();

    [DllImport("__Internal")]
    static extern void QuizSpeech_Cancel();

    [DllImport("__Internal")]
    static extern void QuizSpeech_Enqueue(string text, int mood);

    [DllImport("__Internal")]
    static extern int QuizSpeech_IsBusy();

    static bool _initialized;

    public static bool EnsureInitialized()
    {
        if (_initialized) return true;
        try
        {
            _initialized = QuizSpeech_Init() != 0;
        }
        catch
        {
            _initialized = false;
        }

        return _initialized;
    }

    public static void Cancel()
    {
        try { QuizSpeech_Cancel(); } catch { /* ignored */ }
    }

    public static void Enqueue(string text, int mood = 0)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (!EnsureInitialized()) return;
        try { QuizSpeech_Enqueue(text, mood); } catch { /* ignored */ }
    }

    public static bool WillRunInCurrentPlayer => true;

    public static bool IsSpeechBusy()
    {
        if (!_initialized && !EnsureInitialized()) return false;
        try { return QuizSpeech_IsBusy() != 0; }
        catch { return false; }
    }

#elif UNITY_EDITOR

    static bool _editorInitAttempted;
    static bool _editorOk;

    public static bool EnsureInitialized()
    {
        if (_editorInitAttempted) return _editorOk;
        _editorInitAttempted = true;
#if UNITY_EDITOR_WIN
        _editorOk = EditorPowerShellSpeech.Init();
        if (!_editorOk)
            Debug.LogWarning(
                "Quiz voice (Editor): Could not find powershell.exe or speech failed to run. On Windows, PowerShell should be at Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe.");
#else
        _editorOk = false;
#endif
        return _editorOk;
    }

    public static void Cancel()
    {
#if UNITY_EDITOR_WIN
        EditorPowerShellSpeech.Cancel();
#endif
    }

    public static void Enqueue(string text, int mood = 0)
    {
        if (string.IsNullOrEmpty(text)) return;
#if UNITY_EDITOR_WIN
        if (!EnsureInitialized()) return;
        EditorPowerShellSpeech.Enqueue(text, mood);
#endif
    }

    public static bool WillRunInCurrentPlayer => Application.platform == RuntimePlatform.WindowsEditor;

#if UNITY_EDITOR_WIN
    public static bool IsSpeechBusy() => EditorPowerShellSpeech.IsBusy();
#else
    public static bool IsSpeechBusy() => false;
#endif

#else

    public static bool EnsureInitialized() => false;

    public static void Cancel() { }

    public static void Enqueue(string text, int mood = 0) { }

    public static bool WillRunInCurrentPlayer => false;

    public static bool IsSpeechBusy() => false;

#endif

    /// <summary>Yields until the narrated phrase queue is drained (or timeout).</summary>
    public static IEnumerator WaitUntilSpeechIdle(float timeoutSeconds = 180f)
    {
        var elapsed = 0f;
        while (IsSpeechBusy())
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= timeoutSeconds)
                yield break;
            yield return null;
        }
    }

#if UNITY_EDITOR && UNITY_EDITOR_WIN

    /// <summary>
    /// Each phrase runs in a separate PowerShell process — Framework loads System.Speech outside Unity.
    /// </summary>
    static class EditorPowerShellSpeech
    {
        static readonly Queue<(string text, int mood)> Pending = new Queue<(string text, int mood)>();
        static readonly object Gate = new object();
        static Thread _worker;
        static AutoResetEvent _wake = new AutoResetEvent(false);
        static string _powershellExe;
        static System.Diagnostics.Process _running;

        public static bool Init()
        {
            _powershellExe = ResolvePowerShellPath();
            if (string.IsNullOrEmpty(_powershellExe))
                return false;

            EnsureWorker();
            return true;
        }

        static string ResolvePowerShellPath()
        {
            var sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var legacy = Path.Combine(sys, "WindowsPowerShell", "v1.0", "powershell.exe");
            if (File.Exists(legacy))
                return legacy;

            var pf64 = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrEmpty(pf64))
            {
                var pwsh = Path.Combine(pf64, "PowerShell", "7", "pwsh.exe");
                if (File.Exists(pwsh))
                    return pwsh;
            }

            return null;
        }

        static void EnsureWorker()
        {
            lock (Gate)
            {
                if (_worker != null && _worker.IsAlive) return;
                _worker = new Thread(WorkerLoop)
                {
                    IsBackground = true,
                    Name = "QuizVoiceEditorPwshTTS"
                };
                _worker.Start();
            }
        }

        static void WorkerLoop()
        {
            while (true)
            {
                _wake.WaitOne(300);

                while (true)
                {
                    (string text, int mood) item;
                    lock (Gate)
                    {
                        if (Pending.Count == 0) break;
                        item = Pending.Dequeue();
                    }

                    RunSpeakProcess(item.text, item.mood);
                }
            }
        }

        static void RunSpeakProcess(string text, int mood)
        {
            text = NormalizeForSpeech(text);
            if (text.Length > 4000)
                text = text.Substring(0, 4000);

            var rate = mood switch
            {
                1 => 2,
                2 => -2,
                _ => 0
            };

            var escaped = EscapePowerShellSingleQuoted(text);
            var script =
                "Add-Type -AssemblyName System.Speech;" +
                "$s=New-Object System.Speech.Synthesis.SpeechSynthesizer;" +
                "$s.Rate=" + rate + ";" +
                "$s.Speak('" + escaped + "');";

            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _powershellExe,
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encoded,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };

                using var p = new System.Diagnostics.Process { StartInfo = psi };
                lock (Gate)
                {
                    _running = p;
                }

                p.Start();
                p.WaitForExit(90000);

                lock (Gate)
                {
                    if (_running == p)
                        _running = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Quiz voice (Editor PowerShell): " + e.Message);
                lock (Gate)
                {
                    _running = null;
                }
            }
        }

        static string NormalizeForSpeech(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        /// <summary>Escape content inside PowerShell single-quoted string.</summary>
        static string EscapePowerShellSingleQuoted(string s)
        {
            return s.Replace("'", "''");
        }

        public static void Enqueue(string text, int mood)
        {
            lock (Gate)
            {
                Pending.Enqueue((text, mood));
            }

            _wake.Set();
        }

        public static void Cancel()
        {
            System.Diagnostics.Process killCopy = null;
            lock (Gate)
            {
                Pending.Clear();
                killCopy = _running;
            }

            try
            {
                if (killCopy != null && !killCopy.HasExited)
                    killCopy.Kill();
            }
            catch
            {
                /* ignored */
            }
        }

        public static bool IsBusy()
        {
            lock (Gate)
            {
                if (Pending.Count > 0)
                    return true;
                try
                {
                    return _running != null && !_running.HasExited;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

#endif
}
