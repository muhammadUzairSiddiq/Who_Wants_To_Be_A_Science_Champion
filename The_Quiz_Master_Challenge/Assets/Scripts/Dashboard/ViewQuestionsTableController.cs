using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class ViewQuestionsTableController : MonoBehaviour
{
    [Serializable]
    struct ColumnWidths
    {
        public float id;
        public float question;
        public float category;
        public float difficulty;
        public float correctAnswer;
        public float action;
    }

    [Serializable]
    public class StringEvent : UnityEvent<string> { }

    [Header("Root References (optional - auto-resolve if empty)")]
    [SerializeField] RectTransform tableHost;
    [SerializeField] TMP_InputField searchInput;
    [SerializeField] TMP_Dropdown categoryFilterDropdown;

    [Header("Typography")]
    [SerializeField] TMP_FontAsset tableFont;
    [SerializeField] float headerFontSize = 28f;
    [SerializeField] float rowFontSize = 24f;
    [SerializeField] Color headerTextColor = new(1f, 0.89f, 0.25f, 1f);
    [SerializeField] Color rowTextColor = new(0.95f, 0.95f, 1f, 1f);

    [Header("Table Styling")]
    [SerializeField] Sprite headerBackgroundSprite;
    [SerializeField] Sprite rowBackgroundSprite;
    [SerializeField] Color headerBackgroundColor = new(0.16f, 0.08f, 0.32f, 0.94f);
    [SerializeField] Color rowBackgroundColor = new(0.10f, 0.06f, 0.25f, 0.90f);
    [SerializeField] float rowHeight = 72f;
    [SerializeField] float rowSpacing = 8f;
    [SerializeField] float columnSpacing = 90f;
    [SerializeField] float headerColumnSpacing = 50f;
    [SerializeField] ColumnWidths columnWidths = new()
    {
        id = 80f,
        question = 360f,
        category = 150f,
        difficulty = 150f,
        correctAnswer = 130f,
        action = 170f
    };
    [SerializeField] bool useAutoProportionalColumns = true;

    [Header("Badge Sprites (Optional)")]
    [SerializeField] Sprite categoryBadgeSprite;
    [SerializeField] Sprite categoryBiologySprite;
    [SerializeField] Sprite categoryChemistrySprite;
    [SerializeField] Sprite categoryMathSprite;
    [SerializeField] Sprite categoryMixedSprite;
    [SerializeField] Sprite categoryPhysicsSprite;
    [SerializeField] Sprite difficultyEasySprite;
    [SerializeField] Sprite difficultyMediumSprite;
    [SerializeField] Sprite difficultyHardSprite;

    [Header("Action Button Sprites (Optional)")]
    [SerializeField] Sprite editButtonSprite;
    [SerializeField] Sprite viewButtonSprite;
    [SerializeField] Sprite deleteButtonSprite;
    [SerializeField] Color actionButtonColor = Color.white;

    [Header("Action Events")]
    [SerializeField] StringEvent onEditClicked = new();
    [SerializeField] StringEvent onViewClicked = new();
    [SerializeField] StringEvent onDeleteClicked = new();

    VerticalLayoutGroup tableLayout;
    RectTransform contentRoot;
    readonly List<GameObject> runtimeRows = new();
    readonly List<TeacherQuestionRecord> filteredRecords = new();
    bool wired;

    static readonly string[] HeaderTitles =
    {
        "ID", "Question", "Category", "Difficulty", "Correct Answer", "Action"
    };

    void Awake()
    {
#if UNITY_EDITOR
        AutoResolveSpritesFromTeacherViewQuestionFolder();
#endif
        ResolveReferences();
        EnsureTableRoot();
        BuildHeader();
        WireInputs();
    }

    void OnEnable()
    {
        RefreshTable();
    }

    void OnDestroy()
    {
        if (wired && searchInput != null)
            searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        if (wired && categoryFilterDropdown != null)
            categoryFilterDropdown.onValueChanged.RemoveListener(OnFilterChanged);
    }

    public void RefreshTable()
    {
        if (contentRoot == null) return;

        ClearRows();
        BuildFilteredRecords();
        for (var i = 0; i < filteredRecords.Count; i++)
            BuildRow(filteredRecords[i], i);
    }

    void ResolveReferences()
    {
        if (tableHost == null)
        {
            var host = transform.Find("TableHost")
                       ?? transform.Find("Table")
                       ?? transform.Find("Questions Table")
                       ?? transform.Find("Add New Question Panel");
            if (host != null) tableHost = host as RectTransform;
        }

        if (searchInput == null)
        {
            searchInput = transform.Find("Search InputField")?.GetComponent<TMP_InputField>()
                         ?? transform.Find("Search Input")?.GetComponent<TMP_InputField>()
                         ?? transform.Find("Search Bar/InputField")?.GetComponent<TMP_InputField>();
        }

        if (categoryFilterDropdown == null)
        {
            categoryFilterDropdown = transform.Find("Category Dropdown")?.GetComponent<TMP_Dropdown>()
                                   ?? transform.Find("Category Filter")?.GetComponent<TMP_Dropdown>();
        }
    }

    void EnsureTableRoot()
    {
        if (tableHost == null) tableHost = transform as RectTransform;
        if (tableHost == null) return;

        var existing = tableHost.Find("RuntimeTableContent") as RectTransform;
        if (existing != null)
        {
            contentRoot = existing;
            tableLayout = contentRoot.GetComponent<VerticalLayoutGroup>();
            return;
        }

        var go = new GameObject("RuntimeTableContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentRoot = go.GetComponent<RectTransform>();
        contentRoot.SetParent(tableHost, false);
        contentRoot.anchorMin = new Vector2(0f, 0f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;

        tableLayout = go.GetComponent<VerticalLayoutGroup>();
        tableLayout.childAlignment = TextAnchor.UpperCenter;
        tableLayout.childControlHeight = false;
        tableLayout.childControlWidth = true;
        tableLayout.childForceExpandHeight = false;
        tableLayout.childForceExpandWidth = true;
        tableLayout.spacing = rowSpacing;
        tableLayout.padding = new RectOffset(8, 8, 8, 8);

        var fit = go.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void BuildHeader()
    {
        if (contentRoot == null || contentRoot.Find("HeaderRow") != null) return;

        var widths = GetResolvedColumnWidths();
        var header = CreateRowContainer("HeaderRow", true);
        header.transform.SetAsFirstSibling();

        AddTextCell(header.transform, HeaderTitles[0], widths.id, true, TextAlignmentOptions.Center);
        AddTextCell(header.transform, HeaderTitles[1], widths.question, true, TextAlignmentOptions.Center);
        AddTextCell(header.transform, HeaderTitles[2], widths.category, true, TextAlignmentOptions.Center);
        AddTextCell(header.transform, HeaderTitles[3], widths.difficulty, true, TextAlignmentOptions.Center);
        AddTextCell(header.transform, HeaderTitles[4], widths.correctAnswer, true, TextAlignmentOptions.Center);
        AddTextCell(header.transform, HeaderTitles[5], widths.action, true, TextAlignmentOptions.Center);
    }

    void WireInputs()
    {
        if (wired) return;
        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchChanged);
        if (categoryFilterDropdown != null)
            categoryFilterDropdown.onValueChanged.AddListener(OnFilterChanged);
        wired = true;
    }

    void OnSearchChanged(string _) => RefreshTable();
    void OnFilterChanged(int _) => RefreshTable();

    void BuildFilteredRecords()
    {
        filteredRecords.Clear();
        var all = TeacherQuestionStore.GetAllRecords();

        var search = searchInput != null ? (searchInput.text ?? string.Empty).Trim() : string.Empty;
        var selectedCategory = GetSelectedCategoryFilter();

        for (var i = 0; i < all.Count; i++)
        {
            var q = all[i];
            if (q == null || string.IsNullOrWhiteSpace(q.question)) continue;
            if (!MatchesCategoryFilter(q, selectedCategory)) continue;
            if (!MatchesSearch(q, search)) continue;
            filteredRecords.Add(q);
        }

        filteredRecords.Sort((a, b) =>
        {
            var aOrd = TeacherQuestionStore.ParseQuestionIdOrdinal(a?.id);
            var bOrd = TeacherQuestionStore.ParseQuestionIdOrdinal(b?.id);
            if (aOrd != bOrd) return aOrd.CompareTo(bOrd);
            return string.Compare(a?.id, b?.id, StringComparison.OrdinalIgnoreCase);
        });
    }

    static bool MatchesSearch(TeacherQuestionRecord q, string search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        return Contains(q.id, search)
               || Contains(q.question, search)
               || Contains(q.categoryLabel, search)
               || Contains(q.categoryKey, search)
               || Contains(q.difficulty, search)
               || Contains(q.correctAnswer, search);
    }

    static bool Contains(string source, string needle) =>
        !string.IsNullOrEmpty(source)
        && source.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    bool MatchesCategoryFilter(TeacherQuestionRecord q, string selected)
    {
        if (string.IsNullOrWhiteSpace(selected)) return true;
        if (selected.Equals("All", StringComparison.OrdinalIgnoreCase)) return true;
        if (selected.Equals("All Categories", StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(q.categoryLabel?.Trim(), selected, StringComparison.OrdinalIgnoreCase)
               || string.Equals(q.categoryKey?.Trim(), selected, StringComparison.OrdinalIgnoreCase);
    }

    string GetSelectedCategoryFilter()
    {
        if (categoryFilterDropdown == null || categoryFilterDropdown.options.Count == 0) return "All";
        var idx = Mathf.Clamp(categoryFilterDropdown.value, 0, categoryFilterDropdown.options.Count - 1);
        return categoryFilterDropdown.options[idx].text ?? "All";
    }

    void BuildRow(TeacherQuestionRecord q, int rowIndex)
    {
        var widths = GetResolvedColumnWidths();
        var row = CreateRowContainer($"Row_{rowIndex + 1:D3}", false);
        runtimeRows.Add(row);

        AddTextCell(row.transform, q.id ?? "-", widths.id, false, TextAlignmentOptions.Center);
        AddTextCell(row.transform, q.question ?? string.Empty, widths.question, false, TextAlignmentOptions.Center);
        AddBadgeCell(row.transform, q.categoryLabel ?? q.categoryKey ?? "-", widths.category, GetCategorySprite(q));
        AddBadgeCell(row.transform, q.difficulty ?? "-", widths.difficulty, GetDifficultySprite(q.difficulty));
        AddTextCell(row.transform, q.correctAnswer ?? "-", widths.correctAnswer, false, TextAlignmentOptions.Center);
        AddActionCell(row.transform, q, widths.action);
    }

    GameObject CreateRowContainer(string rowName, bool isHeader)
    {
        var row = new GameObject(
            rowName,
            typeof(RectTransform),
            typeof(Image),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));

        var rt = row.GetComponent<RectTransform>();
        rt.SetParent(contentRoot, false);
        rt.localScale = Vector3.one;

        var image = row.GetComponent<Image>();
        image.sprite = isHeader ? headerBackgroundSprite : rowBackgroundSprite;
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = isHeader ? headerBackgroundColor : rowBackgroundColor;

        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;
        layout.spacing = isHeader ? headerColumnSpacing : columnSpacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;

        var le = row.GetComponent<LayoutElement>();
        le.minHeight = rowHeight;
        le.preferredHeight = rowHeight;
        le.flexibleWidth = 1f;

        return row;
    }

    void AddTextCell(Transform parent, string text, float width, bool isHeader, TextAlignmentOptions alignment)
    {
        var cell = new GameObject(
            "Cell",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(TextMeshProUGUI));

        cell.transform.SetParent(parent, false);

        var le = cell.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.flexibleWidth = 0f;

        var tmp = cell.GetComponent<TextMeshProUGUI>();
        if (tableFont != null) tmp.font = tableFont;
        tmp.text = text;
        tmp.fontSize = isHeader ? headerFontSize : rowFontSize;
        tmp.alignment = alignment;
        tmp.color = isHeader ? headerTextColor : rowTextColor;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }

    void AddBadgeCell(Transform parent, string text, float width, Sprite badgeSprite)
    {
        var holder = new GameObject(
            "BadgeCell",
            typeof(RectTransform),
            typeof(LayoutElement));
        holder.transform.SetParent(parent, false);
        var holderLe = holder.GetComponent<LayoutElement>();
        holderLe.preferredWidth = width;
        holderLe.flexibleWidth = 0f;

        var badge = new GameObject(
            "Badge",
            typeof(RectTransform),
            typeof(Image));
        badge.transform.SetParent(holder.transform, false);

        var badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.5f, 0.5f);
        badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
        badgeRt.pivot = new Vector2(0.5f, 0.5f);
        badgeRt.sizeDelta = new Vector2(Mathf.Min(width * 0.56f, 96f), rowHeight - 34f);
        badgeRt.anchoredPosition = Vector2.zero;

        var img = badge.GetComponent<Image>();
        img.sprite = badgeSprite;
        img.type = badgeSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = badgeSprite != null ? Color.white : new Color(0.24f, 0.47f, 0.90f, 0.45f);

        if (badgeSprite == null)
        {
            var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txt.transform.SetParent(badge.transform, false);
            var txtRt = txt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(8f, 2f);
            txtRt.offsetMax = new Vector2(-8f, -2f);
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            if (tableFont != null) tmp.font = tableFont;
            tmp.text = text;
            tmp.fontSize = rowFontSize - 2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = rowTextColor;
        }
    }

    Sprite GetDifficultySprite(string difficulty)
    {
        if (string.IsNullOrWhiteSpace(difficulty)) return null;
        if (difficulty.Equals("Easy", StringComparison.OrdinalIgnoreCase)) return difficultyEasySprite;
        if (difficulty.Equals("Medium", StringComparison.OrdinalIgnoreCase)) return difficultyMediumSprite;
        if (difficulty.Equals("Hard", StringComparison.OrdinalIgnoreCase)) return difficultyHardSprite;
        return null;
    }

    Sprite GetCategorySprite(TeacherQuestionRecord q)
    {
        var key = (q?.categoryKey ?? q?.categoryLabel ?? string.Empty).Trim();
        if (key.Equals("Math", StringComparison.OrdinalIgnoreCase) || key.Equals("Maths", StringComparison.OrdinalIgnoreCase))
            return categoryMathSprite;
        if (key.Equals("Physics", StringComparison.OrdinalIgnoreCase))
            return categoryPhysicsSprite;
        if (key.Equals("Chemistry", StringComparison.OrdinalIgnoreCase))
            return categoryChemistrySprite;
        if (key.Equals("Biology", StringComparison.OrdinalIgnoreCase))
            return categoryBiologySprite;
        if (key.Equals("Mixed", StringComparison.OrdinalIgnoreCase))
            return categoryMixedSprite;
        if (key.Equals("Science", StringComparison.OrdinalIgnoreCase))
            return categoryMixedSprite != null ? categoryMixedSprite : categoryBadgeSprite;
        return categoryBadgeSprite;
    }

    void AddActionCell(Transform parent, TeacherQuestionRecord q, float width)
    {
        var holder = new GameObject(
            "ActionCell",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(HorizontalLayoutGroup));
        holder.transform.SetParent(parent, false);

        var le = holder.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.flexibleWidth = 0f;

        var h = holder.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 14f;
        h.padding = new RectOffset(0, 0, 0, 0);
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlHeight = false;
        h.childControlWidth = false;
        h.childForceExpandHeight = false;
        h.childForceExpandWidth = false;

        AddActionButton(holder.transform, "Edit", editButtonSprite, () =>
        {
            onEditClicked?.Invoke(q.id);
            Debug.Log($"Edit clicked: {q.id}");
        });

        AddActionButton(holder.transform, "View", viewButtonSprite, () =>
        {
            onViewClicked?.Invoke(q.id);
            Debug.Log($"View clicked: {q.id}");
        });

        AddActionButton(holder.transform, "Delete", deleteButtonSprite, () =>
        {
            if (!TeacherQuestionStore.RemoveQuestionById(q.id))
            {
                Debug.LogWarning($"Delete failed: {q.id}");
                return;
            }

            onDeleteClicked?.Invoke(q.id);
            RefreshTable();
        });
    }

    void AddActionButton(Transform parent, string fallbackLabel, Sprite sprite, UnityAction onClick)
    {
        var go = new GameObject(
            $"{fallbackLabel} Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(44f, 36f);

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 44f;
        le.preferredHeight = 36f;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = actionButtonColor;

        if (sprite == null)
        {
            var txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(go.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            if (tableFont != null) tmp.font = tableFont;
            tmp.text = fallbackLabel.Substring(0, 1);
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(onClick);
    }

    void ClearRows()
    {
        for (var i = 0; i < runtimeRows.Count; i++)
        {
            if (runtimeRows[i] != null)
                Destroy(runtimeRows[i]);
        }
        runtimeRows.Clear();
    }

    ColumnWidths GetResolvedColumnWidths()
    {
        var resolved = columnWidths;
        if (tableHost == null) return resolved;

        var available = Mathf.Max(300f, tableHost.rect.width - 36f);
        if (useAutoProportionalColumns)
        {
            var usable = Mathf.Max(240f, available - (columnSpacing * 5f));
            resolved.id = usable * 0.07f;
            resolved.question = usable * 0.26f;
            resolved.category = usable * 0.16f;
            resolved.difficulty = usable * 0.16f;
            resolved.correctAnswer = usable * 0.11f;
            resolved.action = usable * 0.16f;
            return resolved;
        }

        var totalDesired = columnWidths.id + columnWidths.question + columnWidths.category
                           + columnWidths.difficulty + columnWidths.correctAnswer + columnWidths.action
                           + (columnSpacing * 5f);
        if (totalDesired <= available) return resolved;

        var scale = available / totalDesired;
        resolved.id *= scale;
        resolved.question *= scale;
        resolved.category *= scale;
        resolved.difficulty *= scale;
        resolved.correctAnswer *= scale;
        resolved.action *= scale;
        return resolved;
    }

#if UNITY_EDITOR
    void AutoResolveSpritesFromTeacherViewQuestionFolder()
    {
        if (TryLoadSheetSprites("Assets/UI/TEACHER SIDE/View Question/Action Button Icons.png", out var actionMap))
        {
            if (editButtonSprite == null && actionMap.TryGetValue("Edit", out var sEdit)) editButtonSprite = sEdit;
            if (viewButtonSprite == null)
            {
                if (actionMap.TryGetValue("Hide", out var sView)) viewButtonSprite = sView;
            }
            if (deleteButtonSprite == null && actionMap.TryGetValue("Delete", out var sDelete)) deleteButtonSprite = sDelete;
        }

        if (TryLoadSheetSprites("Assets/UI/TEACHER SIDE/View Question/Difficulty Button.png", out var diffMap))
        {
            if (difficultyEasySprite == null && diffMap.TryGetValue("Easy", out var easy)) difficultyEasySprite = easy;
            if (difficultyMediumSprite == null && diffMap.TryGetValue("Medium", out var medium)) difficultyMediumSprite = medium;
            if (difficultyHardSprite == null && diffMap.TryGetValue("Hard", out var hard)) difficultyHardSprite = hard;
        }

        if (TryLoadSheetSprites("Assets/UI/TEACHER SIDE/View Question/Category Icons.png", out var catMap))
        {
            if (categoryBiologySprite == null && catMap.TryGetValue("Biology", out var bio)) categoryBiologySprite = bio;
            if (categoryChemistrySprite == null)
            {
                if (catMap.TryGetValue("chemistry", out var chemLower)) categoryChemistrySprite = chemLower;
                else if (catMap.TryGetValue("Chemistry", out var chem)) categoryChemistrySprite = chem;
            }
            if (categoryMathSprite == null && catMap.TryGetValue("Maths", out var maths)) categoryMathSprite = maths;
            if (categoryMixedSprite == null && catMap.TryGetValue("Mixed", out var mixed)) categoryMixedSprite = mixed;
            if (categoryPhysicsSprite == null && catMap.TryGetValue("Physics", out var phy)) categoryPhysicsSprite = phy;
        }

        if (categoryBadgeSprite == null)
            categoryBadgeSprite = categoryMixedSprite ?? categoryPhysicsSprite ?? categoryMathSprite;
    }

    static bool TryLoadSheetSprites(string assetPath, out Dictionary<string, Sprite> sprites)
    {
        sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        var loaded = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (loaded == null || loaded.Length == 0) return false;

        for (var i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] is Sprite sp && !string.IsNullOrWhiteSpace(sp.name))
                sprites[sp.name] = sp;
        }
        return sprites.Count > 0;
    }
#endif
}
