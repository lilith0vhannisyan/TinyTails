using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns all in-scene UI for a gameplay level.
/// Lives only in level scenes — NOT in the main menu.
///
/// LEVEL SCENE SETUP (assign in Inspector):
///   Panels   → gameplayPanel, pausePanel, winPanel, losePanel
///   HUD      → levelNumberText
///   Buttons  → pauseButton, retryButton (HUD)
///              pauseContinueButton, pauseHomeButton   (pause panel)
///              winNextLevelButton,  winHomeButton     (win panel)
///              loseRetryButton,     loseHomeButton    (lose panel)
///
/// All button listeners are wired in Start() — nothing needs to be set
/// in the Inspector's OnClick fields unless you prefer manual overrides.
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

    // ══════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitPanels();
        SetLevelText();
        WireButtons();

        // Re-hook all buttons (including the ones just wired above) for global click sound
        if (GameManager.Instance != null)
            GameManager.Instance.HookUpAllButtonsInScene();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Initialisation
    // ══════════════════════════════════════════════════════════════════════

    private void InitPanels()
    {
        SetPanel(gameplayPanel, true);
        SetPanel(pausePanel,    false);
        SetPanel(winPanel,      false);
        SetPanel(losePanel,     false);
    }

    private void SetLevelText()
    {
        if (levelNumberText != null)
            levelNumberText.text = "Level " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
    }

    private void WireButtons()
    {
        if (GameManager.Instance == null) return;

        // HUD
        AddListener(pauseButton,         GameManager.Instance.OnPause);
        AddListener(retryButton,         GameManager.Instance.OnRetryLevel);

        // Pause panel
        AddListener(pauseContinueButton, GameManager.Instance.OnContinue);
        AddListener(pauseHomeButton,     GameManager.Instance.OnMainMenu);

        // Win panel
        AddListener(winNextLevelButton,  GameManager.Instance.OnNextLevel);
        AddListener(winHomeButton,       GameManager.Instance.OnMainMenu);

        // Lose panel
        AddListener(loseRetryButton,     GameManager.Instance.OnRetryLevel);
        AddListener(loseHomeButton,      GameManager.Instance.OnMainMenu);
    }

    /// Adds a listener safely — removes first to prevent duplicates on hot reload.
    private static void AddListener(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Panel State — called by GameManager
    // ══════════════════════════════════════════════════════════════════════

    public void ShowWinScreen()
    {
        SetPanel(gameplayPanel, false);
        SetPanel(pausePanel,    false);
        SetPanel(winPanel,      true);
    }

    public void ShowLoseScreen()
    {
        SetPanel(gameplayPanel, false);
        SetPanel(pausePanel,    false);
        SetPanel(losePanel,     true);
    }

    public void ShowPauseScreen()
    {
        SetPanel(pausePanel, true);
    }

    public void HidePauseScreen()
    {
        SetPanel(pausePanel, false);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Manual Inspector Fallbacks
    // (Wire these in the Inspector's OnClick field if you prefer that workflow)
    // ══════════════════════════════════════════════════════════════════════

    public void OnPause()       => GameManager.Instance?.OnPause();
    public void OnResume()      => GameManager.Instance?.OnContinue();
    public void OnRetry()       => GameManager.Instance?.OnRetryLevel();
    public void OnNextLevel()   => GameManager.Instance?.OnNextLevel();
    public void OnMainMenu()    => GameManager.Instance?.OnMainMenu();



    // ══════════════════════════════════════════════════════════════════════
    // Private Helper
    // ══════════════════════════════════════════════════════════════════════

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
