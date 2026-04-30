using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static class StudentCredentials
{
    public const string PrefsSelectedQuizKey = "QuizMaster_SelectedQuizType";
    public const string PrefsSelectedTeamsKey = "QuizMaster_SelectedTeamLetters";
    public const string PrefsViaTeamPlayKey = "QuizMaster_ViaTeamPlay";
    /// <summary>Four display names from menu team setup (slots 0–3 = teams A–D), separated by ASCII Record Separator.</summary>
    public const string PrefsTeamDisplayNamesKey = "QuizMaster_TeamDisplayNamesV1";
    public const string PrefsCoinsPrefix = "QuizMaster_Coins_";

    static readonly Regex StructuredRoll = new(@"^\d{4}-\d{2}-\d{3}$", RegexOptions.CultureInvariant);

    public static string GetSavedStudentName() =>
        PlayerPrefs.GetString(LoginSceneController.PrefsStudentNameKey, string.Empty);

    public static string GetSavedRollRaw() =>
        PlayerPrefs.GetString(LoginSceneController.PrefsRollNumberKey, string.Empty);

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

    const char TeamNameDelimiter = '\x1e';
    const int MaxTeamDisplayNameLength = 48;

    public static void SetTeamDisplayNamesFromSlots(System.Collections.Generic.IReadOnlyList<string> fourSlots)
    {
        var parts = new string[4];
        for (var i = 0; i < 4; i++)
        {
            var s = fourSlots != null && i < fourSlots.Count ? fourSlots[i] : string.Empty;
            parts[i] = SanitizeTeamDisplayName(s);
        }

        PlayerPrefs.SetString(PrefsTeamDisplayNamesKey, string.Join(TeamNameDelimiter.ToString(), parts));
        PlayerPrefs.Save();
    }

    /// <summary>Slot 0 = Team A … slot 3 = Team D. Empty if unset.</summary>
    public static string GetTeamDisplayNameForSlot(int slotIndex0To3)
    {
        var parts = LoadTeamDisplayNamesParts();
        if (slotIndex0To3 < 0 || slotIndex0To3 >= parts.Length) return string.Empty;
        return parts[slotIndex0To3] ?? string.Empty;
    }

    static string[] LoadTeamDisplayNamesParts()
    {
        var raw = PlayerPrefs.GetString(PrefsTeamDisplayNamesKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return new[] { "", "", "", "" };
        var split = raw.Split(TeamNameDelimiter);
        var result = new string[4];
        for (var i = 0; i < 4; i++)
            result[i] = i < split.Length ? split[i].Trim() : string.Empty;
        return result;
    }

    static string SanitizeTeamDisplayName(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Trim().Replace(TeamNameDelimiter, ' ');
        if (s.Length > MaxTeamDisplayNameLength)
            s = s.Substring(0, MaxTeamDisplayNameLength).Trim();
        return s;
    }

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

    public static string GetClassDisplayLine(string rollRaw)
    {
        if (TryParseStructuredRoll(rollRaw, out _, out int cls, out _))
            return "Class: " + Ordinal(cls);
        return "Class: —";
    }

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
