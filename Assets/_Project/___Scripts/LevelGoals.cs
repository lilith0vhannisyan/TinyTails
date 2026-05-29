using UnityEngine;

// Put this on a "LevelGoals" empty GameObject in each level scene.
// Drag your CollectionTray into the slot.
//
// WIN  = there are no axolotls left on the board AND the tray is empty
//        (every axolotl got collected and every 3-match has merged away).
// LOSE = the tray is full (6 slots occupied) — no room for more axolotls.
//
// It calls GameManager.CompleteCurrentLevel() / FailCurrentLevel().
public class LevelGoals : MonoBehaviour
{
    [Header("References")]
    public CollectionTray tray;

    [Header("Tuning")]
    [Tooltip("Small delay before checking win, so portal animation and merge can finish.")]
    public float winCheckDelay = 0.5f;

    private bool levelEnded = false;

    private void Awake()
    {
        if (tray == null) tray = FindFirstObjectByType<CollectionTray>();
        if (tray == null) Debug.LogError("[LevelGoals] Tray is NULL! Drag CollectionTray into the Tray slot.");
        else              Debug.Log("[LevelGoals] Tray found. Slot count: " + tray.slotPoints.Count);
    }

    private void OnEnable()
    {
        AxolotlMover.OnPortalReached += HandlePortalReached;
        if (tray != null)
        {
            tray.OnTrayFull += HandleTrayFull;
            tray.OnMerged   += HandleMerged;
        }
        Debug.Log("[LevelGoals] Subscribed to events.");
    }

    private void OnDisable()
    {
        AxolotlMover.OnPortalReached -= HandlePortalReached;
        if (tray != null)
        {
            tray.OnTrayFull -= HandleTrayFull;
            tray.OnMerged   -= HandleMerged;
        }
    }

    private void HandlePortalReached(AxolotlMover m)
    {
        Debug.Log("[LevelGoals] Portal reached by " + m.name + ". Will check win in " + winCheckDelay + "s.");
        Invoke(nameof(CheckWin), winCheckDelay);
    }

    private void HandleMerged(AxolotlColor.ColorType color)
    {
        Debug.Log("[LevelGoals] Merge happened: " + color + ". Will check win.");
        Invoke(nameof(CheckWin), winCheckDelay);
    }

    private void HandleTrayFull()
    {
        Debug.Log("[LevelGoals] TRAY FULL event fired!");
        if (levelEnded) { Debug.Log("[LevelGoals] Already ended, skipping."); return; }
        levelEnded = true;
        Debug.Log("[LevelGoals] LEVEL LOST.");
        if (GameManager.Instance != null) GameManager.Instance.FailCurrentLevel();
        else Debug.LogError("[LevelGoals] GameManager.Instance is NULL!");
    }

    private void CheckWin()
    {
        if (levelEnded) return;

        int boardAxolotls = 0;
        foreach (var m in FindObjectsByType<AxolotlMover>(FindObjectsSortMode.None))
        {
            if (m != null && m.gameObject.activeInHierarchy) boardAxolotls++;
        }

        int trayCount = tray != null ? tray.Count : 0;
        Debug.Log("[LevelGoals] CheckWin: board=" + boardAxolotls + " tray=" + trayCount);

        if (boardAxolotls == 0 && trayCount == 0)
        {
            levelEnded = true;
            Debug.Log("[LevelGoals] LEVEL WON!");
            if (GameManager.Instance != null) GameManager.Instance.CompleteCurrentLevel();
            else Debug.LogError("[LevelGoals] GameManager.Instance is NULL!");
        }
    }
}
