using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGridGenerator : MonoBehaviour
{
    [Header("Prefab Layout Assignment")]
    public GameObject levelButtonPrefab;

    private void Start()
    {
        GenerateLevelButtons();
    }

    public void GenerateLevelButtons()
    {
        // Clear out any old placeholder buttons sitting in the Grid Container layout
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Read build profile settings to discover available stages automatically
        int totalScenes = SceneManager.sceneCountInBuildSettings;
        int nonLevelScenesCount = 1; // Change to 2 if you have both "_StartMenu" and "MainMenu"
        int totalLevels = totalScenes - nonLevelScenesCount;

        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject newButton = Instantiate(levelButtonPrefab, transform);
            LevelSelectionButton buttonController = newButton.GetComponent<LevelSelectionButton>();
            
            if (buttonController == null)
            {
                buttonController = newButton.AddComponent<LevelSelectionButton>();
            }

            buttonController.SetupButton(i);
        }
    }
}