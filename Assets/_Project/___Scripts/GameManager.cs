using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelsPanel;
    public GameObject settingsPanel;
    public GameObject pausePanel;
    public GameObject gameplayPanel;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Toggle Buttons (optional)")]
    public GameObject soundOnIcon;
    public GameObject soundOffIcon;
    public GameObject musicOnIcon;
    public GameObject musicOffIcon;

    [Header("Level Settings")]
    public string gameplaySceneName = "Gameplay";
    public int totalLevels = 10;

    // PlayerPrefs keys
    private const string KEY_SOUND = "SoundEnabled";
    private const string KEY_MUSIC = "MusicEnabled";
    private const string KEY_CURRENT_LEVEL = "CurrentLevel";

    private bool isPaused = false;

    // ---------- Singleton + Init ----------
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadAudioSettings();
    }

    private void Start()
    {
        ShowOnly(mainMenuPanel);
        Time.timeScale = 1f;
    }

    // ---------- Main Menu Actions ----------
    public void StartPlay()
    {
        // Loads the saved level or level 1 by default
        int level = PlayerPrefs.GetInt(KEY_CURRENT_LEVEL, 1);
        LoadLevel(level);
    }

    public void OnPlay()
    {
        StartPlay();
    }

    public void OnLevels()
    {
        ShowOnly(levelsPanel);
    }

    public void OnSetting()
    {
        ShowOnly(settingsPanel);
    }

    // ---------- Close Buttons ----------
    public void OnCloseToMenu()
    {
        // Closes any popup/panel and returns to the main menu
        ShowOnly(mainMenuPanel);
    }

    public void OnCloseToLevel()
    {
        // Closes popup/panel and returns to the current level (gameplay)
        ShowOnly(gameplayPanel);
        Time.timeScale = 1f;
        isPaused = false;
    }

    // ---------- Level Flow ----------
    public void OnRetry()
    {
        // Reload current level
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnNextLevel()
    {
        int current = PlayerPrefs.GetInt(KEY_CURRENT_LEVEL, 1);
        int next = current + 1;

        if (next > totalLevels)
        {
            // No more levels — go back to menu
            OnMainMenu();
            return;
        }

        PlayerPrefs.SetInt(KEY_CURRENT_LEVEL, next);
        PlayerPrefs.Save();
        LoadLevel(next);
    }

    public void LoadLevel(int levelNumber)
    {
        PlayerPrefs.SetInt(KEY_CURRENT_LEVEL, levelNumber);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        isPaused = false;

        // Loads scene by name: "Level1", "Level2", ...
        SceneManager.LoadScene("Level" + levelNumber);
    }

    // ---------- Pause / Continue ----------
    public void OnPause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void OnContinue()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    // ---------- Audio: Sound ----------
    public void OnSoundOn()
    {
        PlayerPrefs.SetInt(KEY_SOUND, 1);
        PlayerPrefs.Save();
        ApplySoundSetting(true);
    }

    public void OnSoundOff()
    {
        PlayerPrefs.SetInt(KEY_SOUND, 0);
        PlayerPrefs.Save();
        ApplySoundSetting(false);
    }

    private void ApplySoundSetting(bool enabled)
    {
        if (sfxSource != null) sfxSource.mute = !enabled;
        if (soundOnIcon != null) soundOnIcon.SetActive(enabled);
        if (soundOffIcon != null) soundOffIcon.SetActive(!enabled);
    }

    // ---------- Audio: Music ----------
    public void OnMusicOn()
    {
        PlayerPrefs.SetInt(KEY_MUSIC, 1);
        PlayerPrefs.Save();
        ApplyMusicSetting(true);
    }

    public void OnMusicOff()
    {
        PlayerPrefs.SetInt(KEY_MUSIC, 0);
        PlayerPrefs.Save();
        ApplyMusicSetting(false);
    }

    private void ApplyMusicSetting(bool enabled)
    {
        if (musicSource != null) musicSource.mute = !enabled;
        if (musicOnIcon != null) musicOnIcon.SetActive(enabled);
        if (musicOffIcon != null) musicOffIcon.SetActive(!enabled);
    }

    private void LoadAudioSettings()
    {
        bool soundOn = PlayerPrefs.GetInt(KEY_SOUND, 1) == 1;
        bool musicOn = PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
        ApplySoundSetting(soundOn);
        ApplyMusicSetting(musicOn);
    }

    // ---------- Helpers ----------
    private void ShowOnly(GameObject panel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panel == mainMenuPanel);
        if (levelsPanel != null) levelsPanel.SetActive(panel == levelsPanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        if (pausePanel != null) pausePanel.SetActive(panel == pausePanel);
        if (gameplayPanel != null) gameplayPanel.SetActive(panel == gameplayPanel);
    }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
