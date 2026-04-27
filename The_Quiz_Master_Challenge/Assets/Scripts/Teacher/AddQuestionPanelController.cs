using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AddQuestionPanelController : MonoBehaviour
{
    [Header("Auto-resolved from \"Add New Question Panel\" if empty")]
    [SerializeField] TMP_InputField questionInput;
    [SerializeField] TMP_Dropdown categoryDropdown;
    [SerializeField] TMP_Dropdown difficultyDropdown;
    [SerializeField] TMP_InputField[] optionInputs = new TMP_InputField[4];
    [SerializeField] Toggle[] correctToggles = new Toggle[4];
    [SerializeField] Button clearButton;
    [SerializeField] Button saveButton;

    void Awake()
    {
        ResolveReferences();
        WireExclusiveCorrectToggles();
        if (clearButton != null)
            clearButton.onClick.AddListener(ClearForm);
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);
    }

    void OnDestroy()
    {
        if (clearButton != null)
            clearButton.onClick.RemoveListener(ClearForm);
        if (saveButton != null)
            saveButton.onClick.RemoveListener(OnSaveClicked);
        for (var i = 0; i < 4; i++)
        {
            if (correctToggles[i] == null) continue;
            correctToggles[i].onValueChanged.RemoveAllListeners();
        }
    }

    void ResolveReferences()
    {
        if (optionInputs == null || optionInputs.Length < 4)
            optionInputs = new TMP_InputField[4];
        if (correctToggles == null || correctToggles.Length < 4)
            correctToggles = new Toggle[4];

        var root = transform;
        if (questionInput == null)
            questionInput = root.Find("Question InputField")?.GetComponent<TMP_InputField>();
        if (categoryDropdown == null)
            categoryDropdown = root.Find("Category Dropdown")?.GetComponent<TMP_Dropdown>();
        if (difficultyDropdown == null)
            difficultyDropdown = root.Find("Difficulty Dropdown (1)")?.GetComponent<TMP_Dropdown>();

        if (optionInputs[0] == null)
            optionInputs[0] = root.Find("InputField Option A")?.GetComponent<TMP_InputField>();
        if (optionInputs[1] == null)
            optionInputs[1] = root.Find("InputField Option b")?.GetComponent<TMP_InputField>()
                              ?? root.Find("InputField Option B")?.GetComponent<TMP_InputField>();
        if (optionInputs[2] == null)
            optionInputs[2] = root.Find("InputField Option C")?.GetComponent<TMP_InputField>();
        if (optionInputs[3] == null)
            optionInputs[3] = root.Find("InputField Option D")?.GetComponent<TMP_InputField>();

        if (correctToggles[0] == null) correctToggles[0] = FindCorrectToggle(root, "A");
        if (correctToggles[1] == null) correctToggles[1] = FindCorrectToggle(root, "B");
        if (correctToggles[2] == null) correctToggles[2] = FindCorrectToggle(root, "C");
        if (correctToggles[3] == null) correctToggles[3] = FindCorrectToggle(root, "D");

        if (clearButton == null)
            clearButton = root.Find("Clear Button")?.GetComponent<Button>();
        if (saveButton == null)
            saveButton = root.Find("Save Button")?.GetComponent<Button>();
    }

    static Toggle FindCorrectToggle(Transform root, string optionLetter)
    {
        var needle = $"Option {optionLetter} Correct";
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root || t.childCount == 0) continue;
            if (t.name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
            var toggleTr = t.Find("Toggle");
            if (toggleTr != null)
            {
                var tg = toggleTr.GetComponent<Toggle>();
                if (tg != null) return tg;
            }
        }

        return null;
    }

    void WireExclusiveCorrectToggles()
    {
        for (var i = 0; i < 4; i++)
        {
            if (correctToggles[i] == null) continue;
            var idx = i;
            correctToggles[i].onValueChanged.AddListener(on =>
            {
                if (!on) return;
                for (var j = 0; j < 4; j++)
                {
                    if (j == idx || correctToggles[j] == null) continue;
                    correctToggles[j].SetIsOnWithoutNotify(false);
                }
            });
        }
    }

    public void ClearForm()
    {
        if (questionInput != null) questionInput.text = string.Empty;
        if (categoryDropdown != null) categoryDropdown.value = 0;
        if (difficultyDropdown != null) difficultyDropdown.value = 0;

        foreach (var f in optionInputs)
        {
            if (f != null) f.text = string.Empty;
        }

        for (var i = 0; i < 4; i++)
        {
            if (correctToggles[i] != null)
                correctToggles[i].SetIsOnWithoutNotify(false);
        }
    }

    void OnSaveClicked()
    {
        var qText = questionInput != null ? questionInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(qText))
        {
            Debug.LogWarning("AddQuestion: enter a question.");
            return;
        }

        var options = new string[4];
        for (var i = 0; i < 4; i++)
        {
            options[i] = optionInputs[i] != null ? optionInputs[i].text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(options[i]))
            {
                Debug.LogWarning($"AddQuestion: option {(char)('A' + i)} is empty.");
                return;
            }
        }

        var correctIndex = -1;
        for (var i = 0; i < 4; i++)
        {
            if (correctToggles[i] != null && correctToggles[i].isOn)
            {
                correctIndex = i;
                break;
            }
        }

        if (correctIndex < 0)
        {
            Debug.LogWarning("AddQuestion: mark exactly one correct answer.");
            return;
        }

        var catLabel = string.Empty;
        if (categoryDropdown != null && categoryDropdown.options.Count > 0)
        {
            var v = Mathf.Clamp(categoryDropdown.value, 0, categoryDropdown.options.Count - 1);
            catLabel = categoryDropdown.options[v].text ?? string.Empty;
        }

        var diffLabel = "Easy";
        if (difficultyDropdown != null && difficultyDropdown.options.Count > 0)
        {
            var v = Mathf.Clamp(difficultyDropdown.value, 0, difficultyDropdown.options.Count - 1);
            diffLabel = difficultyDropdown.options[v].text ?? "Easy";
        }

        var record = new TeacherQuestionRecord
        {
            id = TeacherQuestionStore.AllocateNextQuestionId(),
            question = qText,
            categoryKey = TeacherQuestionStore.CanonicalCategoryFromDropdown(catLabel),
            categoryLabel = catLabel.Trim(),
            difficulty = diffLabel.Trim(),
            options = options,
            correctOptionIndex = correctIndex,
            correctAnswer = options[correctIndex],
            createdUtcUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        TeacherQuestionStore.AppendQuestion(record);
        Debug.Log($"Teacher question saved: {record.id} ({record.categoryKey} / {record.difficulty})");
        ClearForm();
    }
}
