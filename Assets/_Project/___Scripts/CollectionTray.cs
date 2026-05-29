using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CollectionTray : MonoBehaviour
{
    [Header("Slots (left to right)")]
    public List<Transform> slotPoints = new List<Transform>();

    [Header("Tray axolotl prefabs (one per color)")]
    public GameObject bluePrefab;
    public GameObject purplePrefab;
    public GameObject pinkPrefab;
    public GameObject yellowPrefab;

    [Header("Animation")]
    public float slideTime = 0.25f;
    public Ease slideEase = Ease.OutQuad;
    public float dropTime = 0.3f;
    public int mergeCount = 3;
    [Tooltip("Scale of axolotls while sitting in the tray (0.75 = 25% smaller).")]
    public float trayScale = 0.75f;
    [Tooltip("Wait after the 3rd one sits before they merge (seconds).")]
    public float mergeDelay = 0.4f;

    [Header("Facing")]
    [Tooltip("Extra Y rotation added to each axolotl so it faces the player. Try 180 if you see its back.")]
    public float facingYOffset = 180f;

    [Header("Effects")]
    public GameObject mergeEffect;   // optional particle on merge

    [Header("Events")]
    public System.Action OnTrayFull;
    public System.Action<AxolotlColor.ColorType> OnMerged;

    public static CollectionTray Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // One entry per occupied slot, in slot order (index 0 = leftmost).
    private class Entry
    {
        public AxolotlColor.ColorType color;
        public GameObject go;
    }
    private readonly List<Entry> entries = new List<Entry>();

    // Add a newly-arrived axolotl of this color to the tray.
    public void AddAxolotl(AxolotlColor.ColorType color)
    {
        Debug.Log("[Tray] AddAxolotl called: " + color + " | current Count=" + entries.Count + " | slotPoints.Count=" + slotPoints.Count);

        if (entries.Count >= slotPoints.Count)
        {
            Debug.Log("[Tray] TRAY FULL - firing OnTrayFull event!");
            OnTrayFull?.Invoke();
            return;
        }

        // Find insert index: right AFTER the last entry of the same color,
        // so same colors group together. If none exist, insert at the end.
        int insertIndex = entries.Count;
        int lastSame = -1;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].color == color) lastSame = i;
        if (lastSame >= 0) insertIndex = lastSame + 1;

        // Spawn the little tray axolotl at the slot, facing as the slot point faces
        // (set each stand point's rotation so the axolotl faces the player).
        GameObject prefab = PrefabFor(color);
        Transform slot = slotPoints[Mathf.Min(insertIndex, slotPoints.Count - 1)];
        Vector3 spawnPos = slot.position + Vector3.up * 2f; // drop in from above
        Quaternion spawnRot = slot.rotation * Quaternion.Euler(0f, facingYOffset, 0f);  // face player
        // NOT parented to the tray, so its scale stays predictable.
        GameObject go = prefab != null
            ? Instantiate(prefab, spawnPos, spawnRot)
            : new GameObject("TrayAxolotl_" + color);

        // Shrink to tray scale (25% smaller by default).
        if (prefab != null)
            go.transform.localScale = prefab.transform.localScale * trayScale;

        // Force-apply the correct color so the tray axolotl matches the collected one,
        // even if all 4 prefab slots point to the same base prefab.
        var ac = go.GetComponent<AxolotlColor>();
        if (ac == null) ac = go.GetComponentInChildren<AxolotlColor>();
        if (ac != null)
        {
            ac.colorType = color;
            ac.Apply();
        }

        // IMPORTANT: a tray axolotl is just decoration. Strip gameplay components
        // so it does NOT claim a lotus on the board (that caused stuck lotuses).
        var ice = go.GetComponent<AxolotlIce>();
        if (ice != null) Destroy(ice);

        var placer = go.GetComponent<AxolotlPlacer>();
        if (placer != null) Destroy(placer);

        var mover = go.GetComponent<AxolotlMover>();
        if (mover != null) Destroy(mover);

        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Entry e = new Entry { color = color, go = go };
        entries.Insert(insertIndex, e);
        Debug.Log("[Tray] Entry ADDED. New Count=" + entries.Count);

        // Re-layout everyone to their slot positions (animated).
        Relayout(animateNewIndex: insertIndex);

        //CheckWinCondition();

        // Check for a merge of this color — but wait for the new one to sit first.
        if (CountColorRun(color) >= mergeCount)
        {
            StartCoroutine(MergeAfterDelay(color));
        }
        else if (entries.Count >= slotPoints.Count)
        {
            // Tray hit max capacity AND no merge is happening -> LOSE
            Debug.Log("[Tray] Tray reached max (" + entries.Count + "/" + slotPoints.Count + ") with no merge - firing OnTrayFull!");
            OnTrayFull?.Invoke();
        }
    }

    private System.Collections.IEnumerator MergeAfterDelay(AxolotlColor.ColorType color)
    {
        // Let the third one finish dropping into its seat, then a small beat.
        yield return new WaitForSeconds(dropTime + mergeDelay);
        TryMerge(color);
    }

    // How many of this color are grouped together right now.
    private int CountColorRun(AxolotlColor.ColorType color)
    {
        int count = 0; bool started = false;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].color == color) { count++; started = true; }
            else if (started) break;
        }
        return count;
    }

    private void Relayout(int animateNewIndex = -1)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Vector3 target = slotPoints[i].position;
            Transform t = entries[i].go.transform;

            if (i == animateNewIndex)
                t.DOMove(target, dropTime).SetEase(Ease.OutBounce); // new one drops in
            else
                t.DOMove(target, slideTime).SetEase(slideEase);      // others slide over
        }
    }

    private void TryMerge(AxolotlColor.ColorType color)
    {
        // Because we group by color, all same-color entries are contiguous.
        // Find the run of this color.
        int start = -1, count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].color == color)
            {
                if (start == -1) start = i;
                count++;
            }
            else if (start != -1)
            {
                break; // run ended
            }
        }

        if (count < mergeCount) return;

        // Merge the first `mergeCount` of them into the middle one's position.
        int mid = start + mergeCount / 2;
        Vector3 mergePos = slotPoints[mid].position;

        Sequence seq = DOTween.Sequence();
        for (int i = start; i < start + mergeCount; i++)
        {
            Transform t = entries[i].go.transform;
            seq.Join(t.DOMove(mergePos, slideTime).SetEase(Ease.InQuad));
            seq.Join(t.DOScale(Vector3.zero, slideTime).SetEase(Ease.InBack));
        }

        // Capture the GameObjects to destroy.
        List<GameObject> toRemove = new List<GameObject>();
        for (int i = start; i < start + mergeCount; i++)
            toRemove.Add(entries[i].go);

        seq.OnComplete(() =>
        {
            if (mergeEffect != null)
                Instantiate(mergeEffect, mergePos, Quaternion.identity);

            foreach (var g in toRemove) if (g != null) Destroy(g);

            entries.RemoveRange(start, mergeCount);
            OnMerged?.Invoke(color);

            // Shift the rest into the freed slots (to the left).
            Relayout();
        });
    }

    private GameObject PrefabFor(AxolotlColor.ColorType c)
    {
        switch (c)
        {
            case AxolotlColor.ColorType.Blue:   return bluePrefab;
            case AxolotlColor.ColorType.Purple: return purplePrefab;
            case AxolotlColor.ColorType.Pink:   return pinkPrefab;
            case AxolotlColor.ColorType.Yellow: return yellowPrefab;
            default: return null;
        }
    }

    //private void CheckWinCondition()
    //{
    //    // 1. Find all axolotls currently moving/placed on the board
    //    AxolotlMover[] activeMovers = Object.FindObjectsByType<AxolotlMover>(FindObjectsSortMode.None);

    //    int collectibleAxolotlsLeft = 0;

    //    foreach (var mover in activeMovers)
    //    {
    //        // Ignore decorative axolotls already sitting in this tray
    //        if (mover.transform.IsChildOf(this.transform)) continue;

    //        // Check if the axolotl has an Ice script attached
    //        var iceComponent = mover.GetComponent<AxolotlIce>();

    //        // If it doesn't have ice, OR it has ice but it's already broken/unfrozen, it's playable!
    //        if (iceComponent == null || !iceComponent.IsFrozen)
    //        {
    //            collectibleAxolotlsLeft++;
    //        }
    //    }

    //    Debug.Log($"[Win Check] Playable axolotls left on board: {collectibleAxolotlsLeft}");

    //    // If 0 active, playable axolotls are left on the board, trigger level completion!
    //    if (collectibleAxolotlsLeft == 0)
    //    {
    //        Debug.Log("[Win Check] Board completely cleared! Triggering CompleteCurrentLevel.");
    //        if (GameManager.Instance != null)
    //        {
    //            GameManager.Instance.CompleteCurrentLevel();
    //        }
    //    }
    //}

    public bool IsFull => entries.Count >= slotPoints.Count;
    public int Count => entries.Count;
}
