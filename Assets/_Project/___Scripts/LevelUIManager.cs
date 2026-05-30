using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PERSISTENT UI manager — lives under GameManager (DontDestroyOnLoad).
/// One instance for the whole game; responds to scene-load events to refresh.
///
/// Setup (assign in Inspector, ONCE in _StartMenu scene):
///   Panels   → gameplayPanel (HUD), pausePanel, winPanel, losePanel
///   HUD      → levelNumberText
///   Buttons  → pauseButton, retryButton (HUD)
///              pauseContinueButton, pauseHomeButton   (pause panel)
///              winNextLevelButton,  winHomeButton     (win panel)
///              loseRetryButton,     loseHomeButton    (lose panel)
/// </summary>
public class LevelUIManager : MonoBehaviour
{
    public static LevelUIManager Instance;

    [Header("Panels")]
    public GameObject gameplayPanel;
    public GameObject pausePanel;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("HUD")]
    public TextMeshProUGUI levelNumberText;
    public Button pauseButton;
    public Button retryButton;

    [Header("Pause Panel Buttons")]
    public Button pauseContinueButton;
    public Button pauseHomeButton;

    [Header("Win Panel Buttons")]
    public Button winNextLevelButton;
    public Button winHomeButton;

    [Header("Lose Panel Buttons")]
    public Button loseRetryButton;
    public Button loseHomeButton;

    [Header("Menu Scene Names")]
    public string startMenuSceneName = "_StartMenu";
    public string levelSelectSceneName = "_LevelSelect";

    private void Awake()
    {
        // Singleton — only one across whole game.
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        WireButtons();
        // Apply initial state for the scene we're already in.
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isMenu = scene.name == startMenuSceneName || scene.name == levelSelectSceneName;

        // Hide all level panels on every scene change.
        SetPanel(pausePanel, false);
        SetPanel(winPanel, false);
        SetPanel(losePanel, false);

        // HUD only visible in level scenes.
        SetPanel(gameplayPanel, !isMenu);

        // Update level number from the scene's build index.
        if (!isMenu && levelNumberText != null)
        {
            int idx = scene.buildIndex;
            levelNumberText.text = "Level " + idx;
        }
    }

    private void WireButtons()
    {
        if (GameManager.Instance == null) return;

        AddListener(pauseButton,         GameManager.Instance.OnPause);
        AddListener(retryButton,         GameManager.Instance.OnRetryLevel);
        AddListener(pauseContinueButton, GameManager.Instance.OnContinue);
        AddListener(pauseHomeButton,     GameManager.Instance.OnMainMenu);
        AddListener(winNextLevelButton,  GameManager.Instance.OnNextLevel);
        AddListener(winHomeButton,       GameManager.Instance.OnMainMenu);
        AddListener(loseRetryButton,     GameManager.Instance.OnRetryLevel);
        AddListener(loseHomeButton,      GameManager.Instance.OnMainMenu);
    }

    private static void AddListener(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    // ---- Called by GameManager when level completes / fails ----
    public void ShowWinScreen()
    {
        SetPanel(gameplayPanel, false);
        SetPanel(pausePanel, false);
        SetPanel(winPanel, true);
    }

    public void ShowLoseScreen()
    {
        SetPanel(gameplayPanel, false);
        SetPanel(pausePanel, false);
        SetPanel(losePanel, true);
    }

    public void ShowPauseScreen() => SetPanel(pausePanel, true);
    public void HidePauseScreen() => SetPanel(pausePanel, false);

    // ---- Inspector-OnClick fallbacks ----
    public void OnPause()     => GameManager.Instance?.OnPause();
    public void OnResume()    => GameManager.Instance?.OnContinue();
    public void OnRetry()     => GameManager.Instance?.OnRetryLevel();
    public void OnNextLevel() => GameManager.Instance?.OnNextLevel();
    public void OnMainMenu()  => GameManager.Instance?.OnMainMenu();

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
