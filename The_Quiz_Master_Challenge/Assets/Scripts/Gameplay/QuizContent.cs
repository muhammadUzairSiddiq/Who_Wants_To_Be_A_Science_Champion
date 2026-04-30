using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class QuizQuestionData
{
    public string Question;
    public string[] Options = new string[4];
    public int CorrectOptionIndex;
}

public static class QuizContent
{
    public static readonly QuizQuestionData FallbackQuestion = new()
    {
        Question = "Sample question?",
        Options = new[] { "Option A", "Option B", "Option C", "Option D" },
        CorrectOptionIndex = 0
    };

    public static readonly QuizQuestionData[] MathQuestions =
    {
        new QuizQuestionData
        {
            Question = "What is 17 + 28?",
            Options = new[] { "35", "45", "55", "42" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "What is 7 × 8?",
            Options = new[] { "54", "56", "63", "49" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "What is the value of pi (approx.)?",
            Options = new[] { "2.71", "3.14", "1.41", "9.81" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "What is 144 ÷ 12?",
            Options = new[] { "10", "11", "12", "14" },
            CorrectOptionIndex = 2
        },
        new QuizQuestionData
        {
            Question = "What is the square root of 81?",
            Options = new[] { "7", "8", "9", "10" },
            CorrectOptionIndex = 2
        }
    };

    public static readonly QuizQuestionData[] PhysicsQuestions =
    {
        new QuizQuestionData
        {
            Question = "Which quantity is measured in newtons?",
            Options = new[] { "Energy", "Force", "Power", "Voltage" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "What is the SI unit of electric current?",
            Options = new[] { "Volt", "Ohm", "Ampere", "Coulomb" },
            CorrectOptionIndex = 2
        },
        new QuizQuestionData
        {
            Question = "Approximate speed of light in vacuum?",
            Options = new[] { "3 x 10^8 m/s", "340 m/s", "1.5 x 10^8 m/s", "9.8 m/s" },
            CorrectOptionIndex = 0
        },
        new QuizQuestionData
        {
            Question = "Which law states F = ma?",
            Options = new[] { "Ohm's law", "Newton's second law", "Snell's law", "Hooke's law" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "What is the SI unit of power?",
            Options = new[] { "Joule", "Watt", "Newton", "Pascal" },
            CorrectOptionIndex = 1
        }
    };

    public static readonly QuizQuestionData[] ChemistryQuestions =
    {
        new QuizQuestionData
        {
            Question = "What is the chemical formula for water?",
            Options = new[] { "H2O", "CO2", "NaCl", "O2" },
            CorrectOptionIndex = 0
        },
        new QuizQuestionData
        {
            Question = "What is the pH of a neutral solution at 25°C?",
            Options = new[] { "0", "7", "14", "1" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "Which gas makes up most of Earth's atmosphere?",
            Options = new[] { "Oxygen", "Carbon dioxide", "Nitrogen", "Hydrogen" },
            CorrectOptionIndex = 2
        },
        new QuizQuestionData
        {
            Question = "What is table salt mainly composed of?",
            Options = new[] { "NaCl", "KCl", "CaCO3", "MgO" },
            CorrectOptionIndex = 0
        },
        new QuizQuestionData
        {
            Question = "Which particle carries a negative charge?",
            Options = new[] { "Proton", "Neutron", "Electron", "Nucleus" },
            CorrectOptionIndex = 2
        }
    };

    public static readonly QuizQuestionData[] BiologyQuestions =
    {
        new QuizQuestionData
        {
            Question = "Which organelle is known as the powerhouse of the cell?",
            Options = new[] { "Nucleus", "Mitochondria", "Ribosome", "Golgi body" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "Which blood cells carry oxygen?",
            Options = new[] { "White blood cells", "Red blood cells", "Platelets", "Plasma" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "Photosynthesis mainly occurs in which organelle?",
            Options = new[] { "Mitochondria", "Chloroplast", "Vacuole", "Lysosome" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "What does DNA stand for?",
            Options = new[]
            {
                "Deoxyribonucleic acid",
                "Dinitrogen acid",
                "Dual nuclear acid",
                "Dynamic nucleic acid"
            },
            CorrectOptionIndex = 0
        },
        new QuizQuestionData
        {
            Question = "What is the largest organ in the human body?",
            Options = new[] { "Liver", "Brain", "Skin", "Heart" },
            CorrectOptionIndex = 2
        }
    };

    public static readonly QuizQuestionData[] MixedQuestions =
    {
        new QuizQuestionData
        {
            Question = "Which planet is known as the Red Planet?",
            Options = new[] { "Venus", "Mars", "Jupiter", "Saturn" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "What is the hardest natural mineral on the Mohs scale?",
            Options = new[] { "Gold", "Quartz", "Diamond", "Iron" },
            CorrectOptionIndex = 2
        },
        new QuizQuestionData
        {
            Question = "What is the freezing point of water at standard pressure (°C)?",
            Options = new[] { "-10", "0", "32", "100" },
            CorrectOptionIndex = 1
        },
        new QuizQuestionData
        {
            Question = "Which gas do plants absorb for photosynthesis?",
            Options = new[] { "Oxygen", "Nitrogen", "Carbon dioxide", "Helium" },
            CorrectOptionIndex = 2
        },
        new QuizQuestionData
        {
            Question = "What is the center of an atom called?",
            Options = new[] { "Shell", "Nucleus", "Electron cloud", "Ion" },
            CorrectOptionIndex = 1
        }
    };

    public static QuizQuestionData[] GetPoolForCategory(string quizId)
    {
        if (string.IsNullOrWhiteSpace(quizId)) return MathQuestions;
        switch (quizId.Trim())
        {
            case "Math": return MathQuestions;
            case "Physics": return PhysicsQuestions;
            case "Chemistry": return ChemistryQuestions;
            case "Biology": return BiologyQuestions;
            case "Mixed": return MixedQuestions;
            default: return MixedQuestions;
        }
    }

    public static QuizQuestionData GetRandomForCategory(string quizId, int forcedSlotIndex = -1)
    {
        var pool = GetPoolForCategory(quizId);
        if (pool == null || pool.Length == 0) return FallbackQuestion;
        if (forcedSlotIndex >= 0 && forcedSlotIndex < pool.Length) return pool[forcedSlotIndex];
        return pool[UnityEngine.Random.Range(0, pool.Length)];
    }

    public static QuizQuestionData GetRandomForCategoryAvoiding(string quizId, int forcedSlotIndex, QuizQuestionData previous)
    {
        var pool = GetPoolForCategory(quizId);
        if (pool == null || pool.Length == 0) return FallbackQuestion;
        if (forcedSlotIndex >= 0 && forcedSlotIndex < pool.Length) return pool[forcedSlotIndex];
        if (previous == null || pool.Length < 2) return pool[UnityEngine.Random.Range(0, pool.Length)];

        QuizQuestionData pick = null;
        for (var attempt = 0; attempt < 24; attempt++)
        {
            pick = pool[UnityEngine.Random.Range(0, pool.Length)];
            if (!SameQuestionStem(pick, previous)) return pick;
        }

        return pick;
    }

    /// <summary>
    /// Levels 1–5 = Round 1 (Easy primary), 6–10 = Round 2 (Medium), 11–15 = Round 3 (Hard).
    /// Teacher questions are filtered by difficulty and ordered by Q### id; within each round slot, missing primary-tier
    /// questions are filled from the next tiers in order (e.g. only 4 Easy → 5th slot uses Medium). Built-in pool if empty.
    /// </summary>
    public static QuizQuestionData GetSequentialForCategoryWithProgress(
        string quizId,
        int teacherSequentialIndex,
        int forcedSlotIndex,
        QuizQuestionData previous) =>
        GetQuestionForLadderSlot(quizId, teacherSequentialIndex, forcedSlotIndex, previous);

    public static QuizQuestionData GetQuestionForLadderSlot(
        string quizId,
        int ladderIndexZeroBased,
        int forcedSlotIndex,
        QuizQuestionData previous)
    {
        if (string.IsNullOrWhiteSpace(quizId)) quizId = "Mixed";
        ladderIndexZeroBased = Mathf.Clamp(ladderIndexZeroBased, 0, QuizScoreLadder.LevelCount - 1);

        if (forcedSlotIndex >= 0)
        {
            var teacherPool = TeacherQuestionStore.BuildOrderedQuizDataPoolForStudentQuiz(quizId);
            if (forcedSlotIndex < teacherPool.Count)
                return teacherPool[forcedSlotIndex];
        }

        var posInRound = ladderIndexZeroBased % 5;
        var roundIndex = ladderIndexZeroBased / 5;

        string[] tierOrder = roundIndex switch
        {
            0 => new[] { "Easy", "Medium", "Hard" },
            1 => new[] { "Medium", "Hard", "Easy" },
            _ => new[] { "Hard", "Medium", "Easy" }
        };

        var easy = TeacherQuestionStore.BuildOrderedFilteredQuizDataPool(quizId, "Easy");
        var medium = TeacherQuestionStore.BuildOrderedFilteredQuizDataPool(quizId, "Medium");
        var hard = TeacherQuestionStore.BuildOrderedFilteredQuizDataPool(quizId, "Hard");

        List<QuizQuestionData> Pool(string tier)
        {
            if (string.Equals(tier, "Easy", StringComparison.OrdinalIgnoreCase)) return easy;
            if (string.Equals(tier, "Medium", StringComparison.OrdinalIgnoreCase)) return medium;
            return hard;
        }

        var idx = posInRound;
        foreach (var tier in tierOrder)
        {
            var pool = Pool(tier);
            if (idx < pool.Count)
                return pool[idx];
            idx -= pool.Count;
        }

        return GetRandomForCategoryAvoiding(quizId, forcedSlotIndex, previous);
    }

    static bool SameQuestionStem(QuizQuestionData a, QuizQuestionData b) =>
        a != null && b != null && string.Equals(a.Question, b.Question, StringComparison.Ordinal);
}

/// <summary>
/// Teacher-authored question (JSON / PlayerPrefs today; same fields for Firebase/Azure later).
/// </summary>
[Serializable]
public class TeacherQuestionRecord
{
    public string id;
    public string question;
    public string categoryKey;
    public string categoryLabel;
    public string difficulty;
    public string[] options = new string[4];
    public int correctOptionIndex;
    public string correctAnswer;
    public long createdUtcUnix;
    public bool isHidden;
}

[Serializable]
public class TeacherQuestionListWrapper
{
    public int schemaVersion = 1;
    public TeacherQuestionRecord[] items = new TeacherQuestionRecord[0];
}

public static class TeacherQuestionStore
{
    public const int CurrentSchemaVersion = 1;
    public const string PlayerPrefsJsonKey = "QuizMaster_TeacherQuestions_Json";

    static TeacherQuestionRecord[] EmptyItems() => new TeacherQuestionRecord[0];

    public static TeacherQuestionListWrapper LoadWrapper()
    {
        var json = PlayerPrefs.GetString(PlayerPrefsJsonKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return new TeacherQuestionListWrapper { schemaVersion = CurrentSchemaVersion, items = EmptyItems() };

        try
        {
            var w = JsonUtility.FromJson<TeacherQuestionListWrapper>(json);
            if (w == null || w.items == null)
                return new TeacherQuestionListWrapper { schemaVersion = CurrentSchemaVersion, items = EmptyItems() };
            w.schemaVersion = Mathf.Max(1, w.schemaVersion);
            return w;
        }
        catch
        {
            return new TeacherQuestionListWrapper { schemaVersion = CurrentSchemaVersion, items = EmptyItems() };
        }
    }

    public static void SaveWrapper(TeacherQuestionListWrapper wrapper)
    {
        if (wrapper.items == null) wrapper.items = EmptyItems();
        wrapper.schemaVersion = CurrentSchemaVersion;
        PlayerPrefs.SetString(PlayerPrefsJsonKey, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    public static IReadOnlyList<TeacherQuestionRecord> GetAllRecords()
    {
        var w = LoadWrapper().items;
        return w ?? EmptyItems();
    }

    public static void AppendQuestion(TeacherQuestionRecord record)
    {
        var w = LoadWrapper();
        var list = new List<TeacherQuestionRecord>(w.items ?? EmptyItems());
        list.Add(record);
        w.items = list.ToArray();
        SaveWrapper(w);
    }

    public static bool RemoveQuestionById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        var w = LoadWrapper();
        var list = new List<TeacherQuestionRecord>(w.items ?? EmptyItems());
        var removed = list.RemoveAll(q => string.Equals(q?.id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        if (removed <= 0) return false;

        w.items = list.ToArray();
        SaveWrapper(w);
        return true;
    }

    public static TeacherQuestionRecord GetQuestionById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var needle = id.Trim();
        foreach (var q in GetAllRecords())
        {
            if (q == null) continue;
            if (string.Equals(q.id, needle, StringComparison.OrdinalIgnoreCase))
                return q;
        }
        return null;
    }

    public static bool UpdateQuestionById(string id, TeacherQuestionRecord updated)
    {
        if (string.IsNullOrWhiteSpace(id) || updated == null) return false;

        var w = LoadWrapper();
        var items = w.items ?? EmptyItems();
        var target = id.Trim();
        for (var i = 0; i < items.Length; i++)
        {
            var existing = items[i];
            if (existing == null) continue;
            if (!string.Equals(existing.id, target, StringComparison.OrdinalIgnoreCase)) continue;

            updated.id = existing.id;
            if (updated.createdUtcUnix <= 0) updated.createdUtcUnix = existing.createdUtcUnix;
            items[i] = updated;
            w.items = items;
            SaveWrapper(w);
            return true;
        }

        return false;
    }

    public static bool SetHiddenById(string id, bool isHidden)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        var w = LoadWrapper();
        var items = w.items ?? EmptyItems();
        var target = id.Trim();
        for (var i = 0; i < items.Length; i++)
        {
            var q = items[i];
            if (q == null) continue;
            if (!string.Equals(q.id, target, StringComparison.OrdinalIgnoreCase)) continue;
            q.isHidden = isHidden;
            w.items = items;
            SaveWrapper(w);
            return true;
        }

        return false;
    }

    public static string AllocateNextQuestionId()
    {
        var w = LoadWrapper();
        var max = 0;
        foreach (var q in w.items ?? EmptyItems())
        {
            var n = ParseQuestionIdOrdinal(q?.id);
            if (n != int.MaxValue) max = Mathf.Max(max, n);
        }

        return "Q" + (max + 1).ToString("D3", CultureInfo.InvariantCulture);
    }

    /// <summary>Parses Q001-style ids for ordering; unknown ids sort last.</summary>
    public static int ParseQuestionIdOrdinal(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return int.MaxValue;
        var m = Regex.Match(id.Trim(), @"^Q(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return n;
        return int.MaxValue;
    }

    /// <summary>All teacher questions matching the student quiz category, sorted by Q### id (then id string).</summary>
    public static List<QuizQuestionData> BuildOrderedQuizDataPoolForStudentQuiz(string studentQuizId)
    {
        var records = new List<TeacherQuestionRecord>();
        foreach (var r in GetAllRecords())
        {
            if (r == null || string.IsNullOrWhiteSpace(r.question)) continue;
            if (r.isHidden) continue;
            if (!RecordMatchesStudentQuiz(r, studentQuizId)) continue;
            records.Add(r);
        }

        records.Sort(CompareRecordsByQuestionId);

        var result = new List<QuizQuestionData>(records.Count);
        foreach (var r in records)
            result.Add(ToQuizQuestionData(r));
        return result;
    }

    static int CompareRecordsByQuestionId(TeacherQuestionRecord a, TeacherQuestionRecord b)
    {
        var na = ParseQuestionIdOrdinal(a?.id);
        var nb = ParseQuestionIdOrdinal(b?.id);
        if (na != nb) return na.CompareTo(nb);
        return string.Compare(a?.id, b?.id, StringComparison.OrdinalIgnoreCase);
    }

    public static string CanonicalCategoryFromDropdown(string dropdownText)
    {
        var t = dropdownText?.Trim() ?? string.Empty;
        if (string.Equals(t, "Maths", StringComparison.OrdinalIgnoreCase)) return "Math";
        return t;
    }

    public static bool RecordMatchesStudentQuiz(TeacherQuestionRecord q, string studentQuizId)
    {
        if (q == null || string.IsNullOrWhiteSpace(studentQuizId)) return false;
        var sid = studentQuizId.Trim();
        var key = q.categoryKey?.Trim() ?? string.Empty;

        if (string.Equals(sid, "Mixed", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(key, sid, StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(key, "Science", StringComparison.OrdinalIgnoreCase))
        {
            return sid.Equals("Physics", StringComparison.OrdinalIgnoreCase)
                   || sid.Equals("Chemistry", StringComparison.OrdinalIgnoreCase)
                   || sid.Equals("Biology", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public static bool DifficultyMatches(string recordDifficulty, string tierEasyMediumHard)
    {
        var a = recordDifficulty?.Trim() ?? string.Empty;
        var b = tierEasyMediumHard?.Trim() ?? string.Empty;
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public static List<QuizQuestionData> BuildFilteredQuizDataPool(string studentQuizId, string difficultyTier)
    {
        var result = new List<QuizQuestionData>();
        foreach (var r in GetAllRecords())
        {
            if (r == null || string.IsNullOrWhiteSpace(r.question)) continue;
            if (r.isHidden) continue;
            if (!RecordMatchesStudentQuiz(r, studentQuizId)) continue;
            if (!DifficultyMatches(r.difficulty, difficultyTier)) continue;
            result.Add(ToQuizQuestionData(r));
        }

        return result;
    }

    /// <summary>Same as <see cref="BuildFilteredQuizDataPool"/> but sorted by Q001-style id for stable round order.</summary>
    public static List<QuizQuestionData> BuildOrderedFilteredQuizDataPool(string studentQuizId, string difficultyTier)
    {
        var records = new List<TeacherQuestionRecord>();
        foreach (var r in GetAllRecords())
        {
            if (r == null || string.IsNullOrWhiteSpace(r.question)) continue;
            if (r.isHidden) continue;
            if (!RecordMatchesStudentQuiz(r, studentQuizId)) continue;
            if (!DifficultyMatches(r.difficulty, difficultyTier)) continue;
            records.Add(r);
        }

        records.Sort(CompareRecordsByQuestionId);
        var result = new List<QuizQuestionData>(records.Count);
        foreach (var r in records)
            result.Add(ToQuizQuestionData(r));
        return result;
    }

    public static QuizQuestionData ToQuizQuestionData(TeacherQuestionRecord r)
    {
        return new QuizQuestionData
        {
            Question = r.question ?? string.Empty,
            Options = r.options != null && r.options.Length == 4
                ? (string[])r.options.Clone()
                : new[] { "", "", "", "" },
            CorrectOptionIndex = Mathf.Clamp(r.correctOptionIndex, 0, 3)
        };
    }
}

public static class QuizDifficultyProgress
{
    public const int EasyCapExclusive = 5;
    public const int MediumCapExclusive = 10;

    public static string GetTierForCorrectCount(int correctAnswersSoFar)
    {
        if (correctAnswersSoFar < EasyCapExclusive) return "Easy";
        if (correctAnswersSoFar < MediumCapExclusive) return "Medium";
        return "Hard";
    }
}
