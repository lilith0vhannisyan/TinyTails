using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionButton : MonoBehaviour
{
    [Header("UI Element Hooks")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image lockIconImage; // Changed from GameObject to Image component
    [SerializeField] private Sprite customLockSprite;

    private int internalLevelNumber = 1;
    private Button targetButton;

    public void SetupButton(int levelNum)
    {
        internalLevelNumber = levelNum;
        targetButton = GetComponent<Button>();

        if (levelText != null)
        {
            levelText.text = levelNum.ToString();
        }

        // Fetch progression state from memory (Defaults to Level 1 unlocked)
        int highestUnlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        if (levelNum <= highestUnlockedLevel)
        {
            // Level is UNLOCKED
            if (targetButton != null) targetButton.interactable = true;
            if (lockIconImage != null) lockIconImage.enabled = false; // Hide lock image component
            if (levelText != null) levelText.gameObject.SetActive(true);
        }
        else
        {
            // Level is LOCKED
            if (targetButton != null) targetButton.interactable = false;
            if (lockIconImage != null) lockIconImage.enabled = true;  // Show lock image component
            if (levelText != null) levelText.gameObject.SetActive(false); // Hide numbers behind lock
        }

        // Safe navigation routing setup
        if (targetButton != null && GameManager.Instance != null)
        {
            targetButton.onClick.RemoveAllListeners();
            targetButton.onClick.AddListener(() => GameManager.Instance.LoadLevel(internalLevelNumber));
        }

        if (customLockSprite != null) lockIconImage.sprite = customLockSprite;
    }
}