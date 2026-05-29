using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Required for TextMeshPro components

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("UI References")]
    [Tooltip("Drag the TextMeshPro text object displaying the level number here.")]
    public TextMeshProUGUI levelNumberText;

    private void Awake()
    {
        // Simple Singleton pattern so you can call LevelManager from anywhere
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateLevelUI();
    }

    public void UpdateLevelUI()
    {
        if (levelNumberText == null) return;

        // Reads the scene position directly from your Build Settings (0, 1, 2...)
        int currentLevelIndex = SceneManager.GetActiveScene().buildIndex;

        levelNumberText.text = currentLevelIndex.ToString();
    }

    public void LoadNextLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        // Safety check to ensure there is a next level configured in Build Settings
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("You reached the final level! Redirecting back to main menu or loop.");
            // SceneManager.LoadScene(0); // Optional: loop back to scene index 0
        }
    }

}