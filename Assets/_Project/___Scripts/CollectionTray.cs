using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Put this on an empty "Tray" object at the bottom of the screen.
// Fill slotPoints with the point GameObjects (left to right) where axolotls sit.
// Fill the 4 little "tray axolotl" prefabs (one per color) that get placed in slots.
//
// Behavior:
//   - When an axolotl enters its portal, the portal calls AddAxolotl(color).
//   - A small colored axolotl is placed in the tray, GROUPED by color
//     (same colors always sit together, e.g. yellow yellow pink blue).
//   - Others slide over (DOTween) to make room.
//   - When 3 of the same color are grouped, they merge into the middle one
//     and disappear, then the rest shift left to close the gap.
//   - Fixed number of slots: if full, OnTrayFull fires (use for lose/own logic).
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
        if (entries.Count >= slotPoints.Count)
        {
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

        Entry e = new Entry { color = color, go = go };
        entries.Insert(insertIndex, e);

        // Re-layout everyone to their slot positions (animated).
        Relayout(animateNewIndex: insertIndex);

        // Check for a merge of this color — but wait for the new one to sit first.
        if (CountColorRun(color) >= mergeCount)
            StartCoroutine(MergeAfterDelay(color));
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

    public bool IsFull => entries.Count >= slotPoints.Count;
    public int Count => entries.Count;
}
