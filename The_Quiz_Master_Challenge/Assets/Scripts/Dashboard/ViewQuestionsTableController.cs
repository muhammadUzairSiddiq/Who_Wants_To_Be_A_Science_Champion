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
    [SerializeField] ScrollRect scrollRect;
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
    [SerializeField] StringEvent onHideClicked = new();
    [SerializeField] StringEvent onDeleteClicked = new();

    VerticalLayoutGroup tableLayout;
    RectTransform contentRoot;
    RectTransform viewportRoot;
    DashboardSceneController dashboardSceneController;
    AddQuestionPanelController addQuestionPanelController;
    GameObject deleteConfirmOverlay;
    Action pendingDeleteAction;
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
        ResolveSceneControllers();
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
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

        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (scrollRect == null)
            {
                var t = transform.Find("Scroll View");
                if (t != null) scrollRect = t.GetComponent<ScrollRect>();
            }
        }
    }

    void ResolveSceneControllers()
    {
        if (dashboardSceneController == null)
            dashboardSceneController = FindFirstObjectByType<DashboardSceneController>(FindObjectsInactive.Include);
        if (addQuestionPanelController == null)
            addQuestionPanelController = FindFirstObjectByType<AddQuestionPanelController>(FindObjectsInactive.Include);
    }

    void EnsureTableRoot()
    {
        if (tableHost == null) tableHost = transform as RectTransform;
        if (tableHost == null) return;

        if (scrollRect != null && scrollRect.content != null)
        {
            contentRoot = scrollRect.content;
            viewportRoot = scrollRect.viewport;
            ConfigureContentLayout(contentRoot);
            return;
        }

        var existing = tableHost.Find("RuntimeScrollView/Viewport/RuntimeTableContent") as RectTransform;
        if (existing == null)
            existing = tableHost.Find("RuntimeTableContent") as RectTransform;
        if (existing != null)
        {
            contentRoot = existing;
            viewportRoot = contentRoot.parent as RectTransform;
            scrollRect = contentRoot.GetComponentInParent<ScrollRect>();
            ConfigureContentLayout(contentRoot);
            return;
        }

        CreateRuntimeScrollView();
    }

    void CreateRuntimeScrollView()
    {
        var scrollGo = new GameObject("RuntimeScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(tableHost, false);
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var scrollImage = scrollGo.GetComponent<Image>();
        scrollImage.color = new Color(1f, 1f, 1f, 0.02f);

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportRoot = viewportGo.GetComponent<RectTransform>();
        viewportRoot.SetParent(scrollRt, false);
        viewportRoot.anchorMin = new Vector2(0f, 0f);
        viewportRoot.anchorMax = new Vector2(1f, 1f);
        viewportRoot.offsetMin = Vector2.zero;
        viewportRoot.offsetMax = Vector2.zero;

        var viewportImage = viewportGo.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("RuntimeTableContent", typeof(RectTransform));
        contentRoot = contentGo.GetComponent<RectTransform>();
        contentRoot.SetParent(viewportRoot, false);
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, 0f);

        scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRoot;
        scrollRect.content = contentRoot;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.verticalNormalizedPosition = 1f;

        ConfigureContentLayout(contentRoot);
    }

    void ConfigureContentLayout(RectTransform content)
    {
        if (content == null) return;

        tableLayout = content.GetComponent<VerticalLayoutGroup>();
        if (tableLayout == null) tableLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        tableLayout.childAlignment = TextAnchor.UpperCenter;
        tableLayout.childControlHeight = false;
        tableLayout.childControlWidth = true;
        tableLayout.childForceExpandHeight = false;
        tableLayout.childForceExpandWidth = true;
        tableLayout.spacing = rowSpacing;
        tableLayout.padding = new RectOffset(0, 0, 0, 0);

        var fit = content.GetComponent<ContentSizeFitter>();
        if (fit == null) fit = content.gameObject.AddComponent<ContentSizeFitter>();
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
        SetRowHiddenVisual(row, q != null && q.isHidden);

        AddTextCell(row.transform, q.id ?? "-", widths.id, false, TextAlignmentOptions.Center);
        AddTextCell(row.transform, q.question ?? string.Empty, widths.question, false, TextAlignmentOptions.Center);
        AddBadgeCell(row.transform, q.categoryLabel ?? q.categoryKey ?? "-", widths.category, GetCategorySprite(q));
        AddBadgeCell(row.transform, q.difficulty ?? "-", widths.difficulty, GetDifficultySprite(q.difficulty));
        AddTextCell(row.transform, q.correctAnswer ?? "-", widths.correctAnswer, false, TextAlignmentOptions.Center);
        AddActionCell(row.transform, q, widths.action);
    }

    void SetRowHiddenVisual(GameObject row, bool isHidden)
    {
        if (row == null) return;
        var img = row.GetComponent<Image>();
        if (img == null) return;
        img.color = isHidden
            ? new Color(rowBackgroundColor.r, rowBackgroundColor.g, rowBackgroundColor.b, 0.28f)
            : rowBackgroundColor;
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
            HandleEdit(q);
        });

        AddActionButton(holder.transform, "Hide", viewButtonSprite, () =>
        {
            HandleHide(q);
        });

        AddActionButton(holder.transform, "Delete", deleteButtonSprite, () =>
        {
            ShowDeleteConfirm(() =>
            {
                if (!TeacherQuestionStore.RemoveQuestionById(q.id))
                {
                    Debug.LogWarning($"Delete failed: {q.id}");
                    return;
                }

                onDeleteClicked?.Invoke(q.id);
                RefreshTable();
            });
        });
    }

    void HandleEdit(TeacherQuestionRecord q)
    {
        if (q == null || string.IsNullOrWhiteSpace(q.id)) return;
        ResolveSceneControllers();
        if (dashboardSceneController != null)
            dashboardSceneController.ShowAddQuestionPanel();
        if (addQuestionPanelController != null)
            addQuestionPanelController.BeginEditById(q.id);
    }

    void HandleHide(TeacherQuestionRecord q)
    {
        if (q == null || string.IsNullOrWhiteSpace(q.id)) return;
        var targetHidden = !q.isHidden;
        if (!TeacherQuestionStore.SetHiddenById(q.id, targetHidden))
        {
            Debug.LogWarning($"Hide failed: {q.id}");
            return;
        }

        onHideClicked?.Invoke(q.id);
        RefreshTable();
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

    void ShowDeleteConfirm(Action onYes)
    {
        pendingDeleteAction = onYes;
        EnsureDeleteConfirmUI();
        if (deleteConfirmOverlay != null)
            deleteConfirmOverlay.SetActive(true);
    }

    void EnsureDeleteConfirmUI()
    {
        if (deleteConfirmOverlay != null) return;

        var root = tableHost != null ? tableHost : transform as RectTransform;
        if (root == null) return;

        var overlay = new GameObject("DeleteConfirmOverlay", typeof(RectTransform), typeof(Image));
        deleteConfirmOverlay = overlay;
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.SetParent(root, false);
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

        var blocker = overlay.AddComponent<Button>();
        blocker.onClick.AddListener(HideDeleteConfirm);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Outline));
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.SetParent(overlayRt, false);
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(700f, 290f);
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.11f, 0.06f, 0.28f, 0.98f);
        var panelOutline = panel.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.88f, 0.70f, 0.16f, 0.9f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        var panelLayout = panel.GetComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(30, 30, 22, 22);
        panelLayout.spacing = 16f;
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        var title = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
        title.transform.SetParent(panelRt, false);
        var titleLe = title.GetComponent<LayoutElement>();
        titleLe.preferredHeight = 52f;
        var titleTmp = title.GetComponent<TextMeshProUGUI>();
        if (tableFont != null) titleTmp.font = tableFont;
        titleTmp.text = "Confirm Deletion";
        titleTmp.fontSize = 38f;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = headerTextColor;

        var msg = new GameObject("Message", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
        msg.transform.SetParent(panelRt, false);
        var msgLe = msg.GetComponent<LayoutElement>();
        msgLe.preferredHeight = 88f;
        var msgTmp = msg.GetComponent<TextMeshProUGUI>();
        if (tableFont != null) msgTmp.font = tableFont;
        msgTmp.text = "Are you sure you want to delete this question?";
        msgTmp.fontSize = 30f;
        msgTmp.alignment = TextAlignmentOptions.Center;
        msgTmp.color = new Color(0.92f, 0.92f, 1f, 1f);

        var buttons = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttons.transform.SetParent(panelRt, false);
        var buttonsLe = buttons.GetComponent<LayoutElement>();
        buttonsLe.preferredHeight = 74f;
        var buttonsLayout = buttons.GetComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 30f;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlWidth = false;
        buttonsLayout.childControlHeight = false;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childForceExpandHeight = false;

        CreateConfirmButton(buttons.transform, "YES", new Color(0.12f, 0.72f, 0.28f, 1f), () =>
        {
            HideDeleteConfirm();
            pendingDeleteAction?.Invoke();
            pendingDeleteAction = null;
        });

        CreateConfirmButton(buttons.transform, "NO", new Color(0.88f, 0.24f, 0.26f, 1f), () =>
        {
            pendingDeleteAction = null;
            HideDeleteConfirm();
        });

        deleteConfirmOverlay.SetActive(false);
    }

    void CreateConfirmButton(Transform parent, string label, Color color, UnityAction onClick)
    {
        var btnGo = new GameObject(
            label + " Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(Outline));
        btnGo.transform.SetParent(parent, false);

        var le = btnGo.GetComponent<LayoutElement>();
        le.preferredWidth = 190f;
        le.preferredHeight = 62f;

        var img = btnGo.GetComponent<Image>();
        img.color = color;
        var outline = btnGo.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.22f);
        outline.effectDistance = new Vector2(1f, -1f);

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txt.transform.SetParent(btnGo.transform, false);
        var txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var tmp = txt.GetComponent<TextMeshProUGUI>();
        if (tableFont != null) tmp.font = tableFont;
        tmp.text = label;
        tmp.fontSize = 30f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    void HideDeleteConfirm()
    {
        if (deleteConfirmOverlay != null)
            deleteConfirmOverlay.SetActive(false);
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
