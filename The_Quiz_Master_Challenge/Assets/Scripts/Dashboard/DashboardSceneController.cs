using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DashboardSceneController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] string teachersLoginSceneName = "TeachersLogin";

    [Header("Panels (optional — auto-resolve when empty)")]
    [SerializeField] GameObject mainMenuDashboardPanel;
    [SerializeField] GameObject addQuestionPanel;
    [SerializeField] GameObject listQuestionsPanel;

    [Header("Buttons (optional)")]
    [SerializeField] Button addQuestionButton;
    [SerializeField] Button viewQuestionsButton;
    [SerializeField] Button mainMenuBackButton;
    [SerializeField] Button addPanelBackButton;
    [SerializeField] Button listPanelBackButton;
    ViewQuestionsTableController viewQuestionsTableController;

    void Awake()
    {
        ResolveReferences();
        WireButtons();
        ShowMainDashboard();
    }

    void OnDestroy()
    {
        if (addQuestionButton != null) addQuestionButton.onClick.RemoveListener(ShowAddQuestionPanel);
        if (viewQuestionsButton != null) viewQuestionsButton.onClick.RemoveListener(ShowListQuestionsPanel);
        if (mainMenuBackButton != null) mainMenuBackButton.onClick.RemoveListener(GoToTeachersLogin);
        if (addPanelBackButton != null) addPanelBackButton.onClick.RemoveListener(ShowMainDashboard);
        if (listPanelBackButton != null) listPanelBackButton.onClick.RemoveListener(ShowMainDashboard);
    }

    void ResolveReferences()
    {
        if (mainMenuDashboardPanel == null)
        {
            var t = transform.Find("Main Menu Dashboard");
            if (t != null) mainMenuDashboardPanel = t.gameObject;
        }

        if (addQuestionPanel == null)
        {
            var t = transform.Find("Add Question Panel");
            if (t != null) addQuestionPanel = t.gameObject;
        }

        if (listQuestionsPanel == null)
        {
            var t = transform.Find("List of Questions Panel")
                    ?? transform.Find("View Question Panel");
            if (t != null) listQuestionsPanel = t.gameObject;
        }

        if (mainMenuDashboardPanel != null)
        {
            if (addQuestionButton == null)
                addQuestionButton = mainMenuDashboardPanel.transform.Find("Lower Panel/AddQuestion Button")?.GetComponent<Button>();
            if (viewQuestionsButton == null)
                viewQuestionsButton = mainMenuDashboardPanel.transform.Find("Lower Panel/View Question Button")?.GetComponent<Button>();
            if (mainMenuBackButton == null)
                mainMenuBackButton = mainMenuDashboardPanel.transform.Find("Back Button")?.GetComponent<Button>();
        }

        if (addPanelBackButton == null && addQuestionPanel != null)
        {
            var t = addQuestionPanel.transform.Find("Add New Question Panel/Back Button");
            if (t != null) addPanelBackButton = t.GetComponent<Button>();
        }

        if (listPanelBackButton == null && listQuestionsPanel != null)
        {
            listPanelBackButton = listQuestionsPanel.transform.Find("Back Button")?.GetComponent<Button>();
            if (listPanelBackButton == null)
                listPanelBackButton = listQuestionsPanel.transform.Find("Add New Question Panel/Back Button")?.GetComponent<Button>();
        }

        if (listQuestionsPanel != null)
        {
            if (viewQuestionsTableController == null)
                viewQuestionsTableController = listQuestionsPanel.GetComponentInChildren<ViewQuestionsTableController>(true);

            if (viewQuestionsTableController == null)
            {
                viewQuestionsTableController = listQuestionsPanel.AddComponent<ViewQuestionsTableController>();
                Debug.Log("Dashboard: auto-added ViewQuestionsTableController to View/List Questions Panel.");
            }
        }
    }

    void WireButtons()
    {
        if (addQuestionButton != null)
            addQuestionButton.onClick.AddListener(ShowAddQuestionPanel);
        if (viewQuestionsButton != null)
            viewQuestionsButton.onClick.AddListener(ShowListQuestionsPanel);
        if (mainMenuBackButton != null)
            mainMenuBackButton.onClick.AddListener(GoToTeachersLogin);
        if (addPanelBackButton != null)
            addPanelBackButton.onClick.AddListener(ShowMainDashboard);
        if (listPanelBackButton != null)
            listPanelBackButton.onClick.AddListener(ShowMainDashboard);
    }

    void ShowAddQuestionPanel()
    {
        if (mainMenuDashboardPanel != null) mainMenuDashboardPanel.SetActive(false);
        if (listQuestionsPanel != null) listQuestionsPanel.SetActive(false);
        if (addQuestionPanel != null) addQuestionPanel.SetActive(true);
    }

    void ShowListQuestionsPanel()
    {
        if (mainMenuDashboardPanel != null) mainMenuDashboardPanel.SetActive(false);
        if (addQuestionPanel != null) addQuestionPanel.SetActive(false);
        if (listQuestionsPanel != null) listQuestionsPanel.SetActive(true);
        if (viewQuestionsTableController != null) viewQuestionsTableController.RefreshTable();
    }

    void ShowMainDashboard()
    {
        if (addQuestionPanel != null) addQuestionPanel.SetActive(false);
        if (listQuestionsPanel != null) listQuestionsPanel.SetActive(false);
        if (mainMenuDashboardPanel != null) mainMenuDashboardPanel.SetActive(true);
    }

    void GoToTeachersLogin()
    {
        if (string.IsNullOrEmpty(teachersLoginSceneName)) return;
        SceneManager.LoadScene(teachersLoginSceneName);
    }
}
