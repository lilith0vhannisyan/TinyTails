using System.Collections.Generic;
using UnityEngine;

// Put this on every Lotus. Add a child empty "StandPoint" at the center; drag it in.
//
// Neighbors (connected touching lotuses) are auto-detected from the closest lotus
// distance, and now also computed in the EDITOR so you can SEE the yellow links
// without pressing Play. Use the "Rebuild Neighbors (all lotuses)" button.
[ExecuteAlways]
public class Lotus : MonoBehaviour
{
    [Tooltip("Empty child at the lotus center where the axolotl stands.")]
    public Transform standPoint;

    [Tooltip("Neighbors = lotuses within (closest distance * this). ~1.3 catches hex neighbors only.")]
    public float neighborTolerance = 1.3f;

    public List<Lotus> neighbors = new List<Lotus>();
    [Tooltip("Portals reachable from this lotus (auto-detected). The axolotl walks to a matching one.")]
    public List<Portal> portalNeighbors = new List<Portal>();
    public bool autoDetect = true;

    public AxolotlMover occupant { get; private set; }

    // Free if no occupant, OR if the occupant was destroyed/disabled without clearing
    // (safety against lotuses getting permanently stuck "busy").
    public bool IsFree
    {
        get
        {
            // Unity's == treats destroyed objects as null, so this catches both
            // "no occupant" and "occupant was destroyed".
            if (occupant == null) { occupant = null; return true; }
            // Occupant exists but is disabled (e.g. entered portal) -> free it.
            if (!occupant.gameObject.activeInHierarchy)
            {
                occupant = null;
                return true;
            }
            return false;
        }
    }

    public static readonly List<Lotus> All = new List<Lotus>();

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        if (standPoint == null) standPoint = transform;
    }

    private void OnDisable() => All.Remove(this);

    private void Start()
    {
        if (autoDetect && Application.isPlaying) DetectNeighbors();
    }

    public void DetectNeighbors()
    {
        neighbors.Clear();
        portalNeighbors.Clear();

        float closest = float.MaxValue;
        foreach (var o in All)
        {
            if (o == this) continue;
            float d = Vector3.Distance(StandPosition, o.StandPosition);
            if (d < closest) closest = d;
        }
        if (closest == float.MaxValue) return;

        float maxNeighbor = closest * neighborTolerance;
        foreach (var o in All)
        {
            if (o == this) continue;
            if (Vector3.Distance(StandPosition, o.StandPosition) <= maxNeighbor)
                neighbors.Add(o);
        }

        // Also detect nearby PORTALS as walkable neighbors.
        foreach (var p in Portal.All)
        {
            if (p == null) continue;
            if (Vector3.Distance(StandPosition, p.StandPosition) <= maxNeighbor)
                portalNeighbors.Add(p);
        }
    }

    // Rebuild neighbors for EVERY lotus in the scene (used by the editor button).
    public static void RebuildAll()
    {
        foreach (var l in All) l.DetectNeighbors();
    }

    public Vector3 StandPosition => standPoint != null ? standPoint.position : transform.position;

    public void SetOccupant(AxolotlMover mover) => occupant = mover;
    public void Clear()
    {
        occupant = null;
        // A freed lotus may let a bubble release an axolotl.
        BubbleManager.NotifyChanged();
    }

    private void OnDrawGizmos()
    {
        // draw always (not just when selected) so links are visible while building
        Vector3 p = StandPosition;
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(p, 0.25f);

        Gizmos.color = Color.yellow;
        if (neighbors != null)
            foreach (var n in neighbors)
                if (n != null) Gizmos.DrawLine(p, n.StandPosition);
    }
}
