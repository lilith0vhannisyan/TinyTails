using System.Collections.Generic;
using UnityEngine;

// Put this ONCE in the scene (e.g. on an empty "BubbleManager" object).
// Set the priorityOrder list in the editor: drag bubbles in the order they should
// win when two bubbles compete for the same freed lotus (index 0 = highest priority).
//
// Whenever a lotus becomes free, call BubbleManager.NotifyChanged() (the Lotus does this
// automatically on Clear). The manager then lets the highest-priority bubble that has a
// free neighbor release its next axolotl, avoiding two bubbles grabbing the same lotus.
public class BubbleManager : MonoBehaviour
{
    [Header("Priority order (index 0 = highest priority)")]
    public List<Bubble> priorityOrder = new List<Bubble>();

    private static BubbleManager instance;
    private static readonly List<Bubble> all = new List<Bubble>();

    private void Awake() => instance = this;
    private void OnDestroy() { if (instance == this) instance = null; }

    public static void Register(Bubble b)   { if (!all.Contains(b)) all.Add(b); }
    public static void Unregister(Bubble b) { all.Remove(b); }

    // Called whenever board state changes (a lotus freed, an axolotl landed, etc.)
    public static void NotifyChanged()
    {
        if (instance != null) instance.Resolve();
    }

    // Let bubbles release onto free lotuses, respecting priority and avoiding overlap.
    private void Resolve()
    {
        // Build the ordered bubble list: explicit priorityOrder first, then any others.
        List<Bubble> ordered = new List<Bubble>();
        foreach (var b in priorityOrder)
            if (b != null && !ordered.Contains(b)) ordered.Add(b);
        foreach (var b in all)
            if (b != null && !ordered.Contains(b)) ordered.Add(b);

        // Track lotuses claimed this pass so two bubbles don't take the same one.
        HashSet<Lotus> claimed = new HashSet<Lotus>();

        foreach (var bubble in ordered)
        {
            if (bubble == null || bubble.IsEmpty) continue;

            Lotus free = null;
            foreach (var n in bubble.neighbors)
            {
                if (n == null || !n.IsFree) continue;
                if (claimed.Contains(n)) continue;
                free = n;
                break;
            }

            if (free != null)
            {
                claimed.Add(free);
                bubble.ReleaseOnto(free);
            }
        }
    }

    // Bubbles should NOT release at level start. They only release when a lotus
    // becomes free DURING play (the player collects an axolotl -> Lotus.Clear()
    // -> NotifyChanged() -> Resolve()). So no Start() Resolve here.
    // private void Start() => Resolve();
}
