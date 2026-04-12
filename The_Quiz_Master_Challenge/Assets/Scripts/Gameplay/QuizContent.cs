using System;
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

    static bool SameQuestionStem(QuizQuestionData a, QuizQuestionData b) =>
        a != null && b != null && string.Equals(a.Question, b.Question, StringComparison.Ordinal);
}
