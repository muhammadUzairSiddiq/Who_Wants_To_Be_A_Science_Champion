using System;
using UnityEngine;

/// <summary>Written by gameplay when the level ends; read by LevelCompleted scene.</summary>
[Serializable]
public class LevelCompletionPayload
{
    public bool teamMode = true;
    public string winnerName;
    public int winnerScore;
    public string runnerName;
    public string thirdName;
    public int runnerScore;
    public int thirdScore;

    /// <summary>Solo win: formatted prize line for UI (e.g. points).</summary>
    public string soloPrizeSummary = string.Empty;
}

public static class LevelCompletionResults
{
    public const string PrefsKey = "QuizMaster_LevelCompletionPayload";

    public static void Save(LevelCompletionPayload payload)
    {
        if (payload == null) return;
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(payload));
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out LevelCompletionPayload payload)
    {
        var raw = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            payload = null;
            return false;
        }

        try
        {
            payload = JsonUtility.FromJson<LevelCompletionPayload>(raw);
            return payload != null;
        }
        catch (Exception)
        {
            payload = null;
            return false;
        }
    }

    public static void Clear() => PlayerPrefs.DeleteKey(PrefsKey);
}
