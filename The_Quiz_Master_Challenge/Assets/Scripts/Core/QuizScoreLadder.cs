/// <summary>
/// Prize ladder from the in-game score board (levels 1–15: Easy → Medium → Hard).
/// Use for awarding points to solo players and teams when they answer correctly at each level.
/// </summary>
public static class QuizScoreLadder
{
    public const int FirstLevel = 1;
    public const int LastLevel = 15;
    public const int LevelCount = LastLevel;

    /// <summary>Index 0 = level 1 (10 PTS), … index 14 = level 15 (200000 PTS).</summary>
    public static readonly int[] PointsPerLevel =
    {
        10,
        30,
        50,
        100,
        200,
        400,
        800,
        1600,
        3200,
        6400,
        12500,
        25000,
        50000,
        100000,
        200000
    };

    /// <param name="levelOneBased">Quiz ladder level 1–15 (matches UI score board rows).</param>
    /// <returns>Points for that level, or 0 if out of range.</returns>
    public static int GetPointsForLevel(int levelOneBased)
    {
        if (levelOneBased < FirstLevel || levelOneBased > LastLevel)
            return 0;
        return PointsPerLevel[levelOneBased - FirstLevel];
    }

    /// <summary>Easy = levels 1–5, Medium = 6–10, Hard = 11–15 (matches score board sections).</summary>
    public static string GetTierLabelForLevel(int levelOneBased)
    {
        if (levelOneBased < FirstLevel || levelOneBased > LastLevel)
            return string.Empty;
        if (levelOneBased <= QuizDifficultyProgress.EasyCapExclusive)
            return "Easy";
        if (levelOneBased <= QuizDifficultyProgress.MediumCapExclusive)
            return "Medium";
        return "Hard";
    }

    /// <summary>Sum of points from level <paramref name="firstLevel"/> through <paramref name="lastLevel"/> inclusive.</summary>
    public static int SumPointsForLevelRange(int firstLevel, int lastLevel)
    {
        var a = System.Math.Max(firstLevel, FirstLevel);
        var b = System.Math.Min(lastLevel, LastLevel);
        if (b < a)
            return 0;
        var sum = 0;
        for (var lv = a; lv <= b; lv++)
            sum += GetPointsForLevel(lv);
        return sum;
    }
}
