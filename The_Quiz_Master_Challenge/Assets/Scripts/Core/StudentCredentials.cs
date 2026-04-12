using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Login data, roll parsing, and per-student coin storage (PlayerPrefs).
/// Profile key uses the full saved roll string so year–class–roll stays unique.
/// </summary>
public static class StudentCredentials
{
    public const string PrefsSelectedQuizKey = "QuizMaster_SelectedQuizType";
    public const string PrefsSelectedTeamsKey = "QuizMaster_SelectedTeamLetters";
    public const string PrefsViaTeamPlayKey = "QuizMaster_ViaTeamPlay";
    public const string PrefsCoinsPrefix = "QuizMaster_Coins_";

    static readonly Regex StructuredRoll = new(@"^\d{4}-\d{2}-\d{3}$", RegexOptions.CultureInvariant);

    public static string GetSavedStudentName() =>
        PlayerPrefs.GetString(LoginSceneController.PrefsStudentNameKey, string.Empty);

    public static string GetSavedRollRaw() =>
        PlayerPrefs.GetString(LoginSceneController.PrefsRollNumberKey, string.Empty);

    /// <summary>Stable id for coin storage (sanitized roll).</summary>
    public static string GetProfileStorageKey()
    {
        var roll = GetSavedRollRaw().Trim();
        if (string.IsNullOrEmpty(roll)) return "anonymous";
        return SanitizeKeySegment(roll);
    }

    public static int GetCoins()
    {
        var key = PrefsCoinsPrefix + GetProfileStorageKey();
        return PlayerPrefs.GetInt(key, 0);
    }

    public static void SetCoins(int value)
    {
        var key = PrefsCoinsPrefix + GetProfileStorageKey();
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public static void AddCoins(int delta) => SetCoins(Mathf.Max(0, GetCoins() + delta));

    public static bool TryParseStructuredRoll(string roll, out int year, out int classNumber, out string actualRollDigits)
    {
        year = 0;
        classNumber = 0;
        actualRollDigits = null;
        roll = roll?.Trim() ?? string.Empty;
        if (!StructuredRoll.IsMatch(roll)) return false;
        var p = roll.Split('-');
        if (p.Length != 3) return false;
        year = int.Parse(p[0], CultureInfo.InvariantCulture);
        classNumber = int.Parse(p[1], CultureInfo.InvariantCulture);
        actualRollDigits = p[2];
        return true;
    }

    /// <summary>UI line for the Class field, e.g. "Class: 9th". Uses middle segment when structured.</summary>
    public static string GetClassDisplayLine(string rollRaw)
    {
        if (TryParseStructuredRoll(rollRaw, out _, out int cls, out _))
            return "Class: " + Ordinal(cls);
        return "Class: —";
    }

    /// <summary>Shown near profile; structured rolls show the last segment (e.g. 004).</summary>
    public static string GetActualRollDisplay(string rollRaw)
    {
        if (TryParseStructuredRoll(rollRaw, out _, out _, out var tail))
            return tail;
        return rollRaw?.Trim() ?? "—";
    }

    static string SanitizeKeySegment(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Replace(":", "_").Replace("/", "_");
    }

    static string Ordinal(int n)
    {
        if (n < 0) return n.ToString(CultureInfo.InvariantCulture);
        var rem100 = n % 100;
        if (rem100 is >= 11 and <= 13) return n + "th";
        return (n % 10) switch
        {
            1 => n + "st",
            2 => n + "nd",
            3 => n + "rd",
            _ => n + "th"
        };
    }
}
