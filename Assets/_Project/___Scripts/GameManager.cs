using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Persistent singleton — survives all scene loads.
/// Owns: audio, vibration, PlayerPrefs, scene navigation, pause state,
///       and the Main Menu panel stack.
///
/// ── MAIN MENU SCENE SETUP (Inspector on the GameManager prefab) ──────────
///   Audio Sources  → musicSource, sfxSource
///   Audio Clips    → buttonClickSound, backgroundMusic, winMusic, loseMusic
///   Audio Icons    → soundOnIcon, soundOffIcon, musicOnIcon, musicOffIcon
///   Main Menu UI   → mainMenuPanel, levelsPanel, settingsPanel
///   Level Settings → totalLevels
///
/// ── LEVEL SCENE SETUP ────────────────────────────────────────────────────
///   Nothing to assign here.
///   LevelUIManager (placed in each level scene) owns all level UI and
///   calls GameManager.Instance methods for navigation / pause / win / lose.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ── Main Menu Panels ───────────────────────────────────────────────────
    [Header("Main Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelsPanel;
    public GameObject settingsPanel;

    // ── Audio ──────────────────────────────────────────────────────────────
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip buttonClickSound;
    public AudioClip backgroundMusic;
    public AudioClip winMusic;
    public AudioClip loseMusic;

    [Header("Audio Toggle Icons")]
    public GameObject soundOnIcon;
    public GameObject soundOffIcon;
    public GameObject musicOnIcon;
    public GameObject musicOffIcon;

    // ── Level Settings ─────────────────────────────────────────────────────
    [Header("Level Settings")]
    public int totalLevels = 10;

    // ── PlayerPrefs Keys ───────────────────────────────────────────────────
    private const string KEY_SOUND          = "SoundEnabled";
    private const string KEY_MUSIC          = "MusicEnabled";
    private const string KEY_VIBRATION      = "VibrationEnabled";
    private const string KEY_CURRENT_LEVEL  = "CurrentLevel";
    private const string KEY_UNLOCKED_LEVEL = "UnlockedLevel";
    private const string KEY_FIRST_TIME     = "FirstTimeSetupComplete";

    // ── Runtime State ──────────────────────────────────────────────────────
    private bool isPaused = false;
    public bool IsGamePaused => isPaused;

    // ══════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            transform.position = Vector3.zero;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;

            HandleFirstTimeDeviceReset();
            LoadAudioSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Hook buttons present in the very first scene before sceneLoaded fires
        HookUpAllButtonsInScene();

        // Show main menu panel on startup if we're in the main menu
        OpenMainMenuPanel();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Scene Load Handler
    // ══════════════════════════════════════════════════════════════════════

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DOTween.KillAll(false);
        DOTween.Clear(false);

        Time.timeScale = 1f;
        isPaused = false;

        // Restore looping background music for any level scene
        if (musicSource != null && scene.name != "_StartMenu" && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
        }

        // Re-apply saved audio mute state
        bool musicOn = PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
        bool soundOn = PlayerPrefs.GetInt(KEY_SOUND, 1) == 1;
        if (sfxSource   != null) sfxSource.mute   = !soundOn;
        if (musicSource != null) musicSource.mute = !musicOn;

        if (musicSource != null && musicOn && !musicSource.isPlaying)
            musicSource.Play();

        // Re-hook all buttons in the new scene
        HookUpAllButtonsInScene();

        // If we returned to the main menu, show the main panel
        if (scene.name == "_StartMenu")
            OpenMainMenuPanel();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Button Sound Hookup
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attaches the global click SFX to every Button in the loaded scene.
    /// Call again after spawning new UI panels at runtime.
    /// </summary>
    public void HookUpAllButtonsInScene()
    {
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button btn in allButtons)
        {
            if (!btn.gameObject.scene.isLoaded) continue;   // skip prefab-only objects
            btn.onClick.RemoveListener(PlayGlobalButtonClickSound);
            btn.onClick.AddListener(PlayGlobalButtonClickSound);
        }
    }

    private void PlayGlobalButtonClickSound() => PlaySFX(buttonClickSound);

    // ══════════════════════════════════════════════════════════════════════
    // Main Menu Panel Navigation
    // ══════════════════════════════════════════════════════════════════════

    /// Shows the root main menu panel, hides the others.
    private void OpenMainMenuPanel()
    {
        SetPanel(mainMenuPanel, true);
        SetPanel(levelsPanel,   false);
        SetPanel(settingsPanel, false);
    }

    /// Called by the Play button on the main menu.
    public void OnPlay()
    {
        int levelToLoad = PlayerPrefs.GetInt(KEY_CURRENT_LEVEL, 1);
        Debug.Log($"[GameManager] Resuming from Level {levelToLoad}");
        LoadLevel(levelToLoad);
    }

    /// Called by the Levels button on the main menu.
    public void OnLevels()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(levelsPanel,   true);
        SetPanel(settingsPanel, false);
    }

    /// Called by the Settings button on the main menu.
    public void OnSettings()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(levelsPanel,   false);
        SetPanel(settingsPanel, true);
    }

    /// Called by any Back / Close button that should return to the main panel.
    public void OnCloseToMenu()
    {
        SetPanel(levelsPanel,   false);
        SetPanel(settingsPanel, false);
        SetPanel(mainMenuPanel, true);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Audio API
    // ══════════════════════════════════════════════════════════════════════

    public void PlaySFX(AudioClip clip)
    {
        if (PlayerPrefs.GetInt(KEY_SOUND, 1) == 1 && sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayWinAudio()
    {
        if (musicSource == null || winMusic == null) return;
        musicSource.Stop();
        musicSource.clip = winMusic;
        musicSource.loop = false;
        musicSource.Play();
    }

    public void PlayLoseAudio()
    {
        if (musicSource == null || loseMusic == null) return;
        musicSource.Stop();
        musicSource.clip = loseMusic;
        musicSource.loop = false;
        musicSource.Play();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Vibration API
    // ══════════════════════════════════════════════════════════════════════

    public void TriggerLightVibration()
    {
        if (PlayerPrefs.GetInt(KEY_VIBRATION, 1) != 1) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void TriggerMediumVibration()
    {
        if (PlayerPrefs.GetInt(KEY_VIBRATION, 1) != 1) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void TriggerHeavyVibration()
    {
        if (PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1)
            StartCoroutine(DoubleVibrationRoutine());
    }

    private System.Collections.IEnumerator DoubleVibrationRoutine()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        yield return new WaitForSeconds(0.1f);
        Handheld.Vibrate();
#else
        yield return null;
#endif
    }

    // ══════════════════════════════════════════════════════════════════════
    // Game State — Win / Lose
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Call this from your gameplay logic when the player wins.</summary>
    public void CompleteCurrentLevel()
    {
        int currentLevel  = SceneManager.GetActiveScene().buildIndex;
        int highestUnlock = PlayerPrefs.GetInt(KEY_UNLOCKED_LEVEL, 1);

        if (currentLevel >= highestUnlock)
        {
            PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, currentLevel + 1);
            PlayerPrefs.Save();
        }

        PlayWinAudio();
        TriggerHeavyVibration();

        if (LevelUIManager.Instance != null)
            LevelUIManager.Instance.ShowWinScreen();
    }

    /// <summary>Call this from your gameplay logic when the player loses.</summary>
    public void FailCurrentLevel()
    {
        PlayLoseAudio();
        TriggerMediumVibration();

        if (LevelUIManager.Instance != null)
            LevelUIManager.Instance.ShowLoseScreen();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Pause / Resume  (called by LevelUIManager buttons)
    // ══════════════════════════════════════════════════════════════════════

    public void OnPause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (LevelUIManager.Instance != null)
            LevelUIManager.Instance.ShowPauseScreen();
    }

    public void OnContinue()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (LevelUIManager.Instance != null)
            LevelUIManager.Instance.HidePauseScreen();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Level Navigation
    // ══════════════════════════════════════════════════════════════════════

    public void OnRetryLevel()
    {
        ClearTweens();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnNextLevel()
    {
        ClearTweens();
        int next = SceneManager.GetActiveScene().buildIndex + 1;

        if (next < SceneManager.sceneCountInBuildSettings)
        {
            int highest = PlayerPrefs.GetInt(KEY_UNLOCKED_LEVEL, 1);
            if (next > highest) PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, next);
            PlayerPrefs.SetInt(KEY_CURRENT_LEVEL, next);
            PlayerPrefs.Save();
            SceneManager.LoadScene(next);
        }
        else
        {
            OnMainMenu();   // all levels finished
        }
    }

    public void OnMainMenu()
    {
        ClearTweens();
        SceneManager.LoadScene("_StartMenu");
    }

    public void LoadLevel(int levelNumber)
    {
        ClearTweens();
        PlayerPrefs.SetInt(KEY_CURRENT_LEVEL, levelNumber);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Level " + levelNumber);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Settings Toggles  (wired from both Main Menu and in-level settings)
    // ══════════════════════════════════════════════════════════════════════

    public void OnSoundOn()      { PlayerPrefs.SetInt(KEY_SOUND,     1); PlayerPrefs.Save(); ApplySoundSetting(true);  }
    public void OnSoundOff()     { PlayerPrefs.SetInt(KEY_SOUND,     0); PlayerPrefs.Save(); ApplySoundSetting(false); }
    public void OnMusicOn()      { PlayerPrefs.SetInt(KEY_MUSIC,     1); PlayerPrefs.Save(); ApplyMusicSetting(true);  }
    public void OnMusicOff()     { PlayerPrefs.SetInt(KEY_MUSIC,     0); PlayerPrefs.Save(); ApplyMusicSetting(false); }
    public void OnVibrationOn()  { PlayerPrefs.SetInt(KEY_VIBRATION, 1); PlayerPrefs.Save(); }
    public void OnVibrationOff() { PlayerPrefs.SetInt(KEY_VIBRATION, 0); PlayerPrefs.Save(); }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ══════════════════════════════════════════════════════════════════════
    // Private Helpers
    // ══════════════════════════════════════════════════════════════════════

    private void ClearTweens()
    {
        DOTween.KillAll(false);
        DOTween.Clear(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private void ApplySoundSetting(bool enabled)
    {
        if (sfxSource    != null) sfxSource.mute    = !enabled;
        if (soundOnIcon  != null) soundOnIcon.SetActive(enabled);
        if (soundOffIcon != null) soundOffIcon.SetActive(!enabled);
    }

    private void ApplyMusicSetting(bool enabled)
    {
        if (musicSource  != null) musicSource.mute  = !enabled;
        if (musicOnIcon  != null) musicOnIcon.SetActive(enabled);
        if (musicOffIcon != null) musicOffIcon.SetActive(!enabled);
    }

    private void LoadAudioSettings()
    {
        ApplySoundSetting(PlayerPrefs.GetInt(KEY_SOUND, 1) == 1);
        ApplyMusicSetting(PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1);
    }

    private void HandleFirstTimeDeviceReset()
    {
        if (!PlayerPrefs.HasKey(KEY_FIRST_TIME))
        {
            PlayerPrefs.SetInt(KEY_CURRENT_LEVEL,  1);
            PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, 1);
            PlayerPrefs.SetInt(KEY_FIRST_TIME,     1);
            PlayerPrefs.Save();
            Debug.Log("[GameManager] First launch — progress reset to Level 1.");
        }
    }
}
