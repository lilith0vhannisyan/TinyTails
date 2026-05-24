using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Put this on each Bubble.
// - Holds an ORDERED queue of axolotl colors (set in the editor, e.g. Blue, Blue, Yellow, Pink).
// - Shows a count (remaining) and the next axolotl's head peeking out.
// - When a NEIGHBOR lotus becomes free, it pops the next axolotl out and it jumps onto that lotus.
// - Conflicts (two bubbles want the same lotus) are resolved by BubbleManager's priority order.
public class Bubble : MonoBehaviour
{
    [Header("Queue (front = index 0 = next to release)")]
    public List<AxolotlColor.ColorType> queue = new List<AxolotlColor.ColorType>();

    [Header("Spawned axolotl prefabs (per color)")]
    public GameObject bluePrefab;
    public GameObject purplePrefab;
    public GameObject pinkPrefab;
    public GameObject yellowPrefab;

    [Header("Neighbors")]
    [Tooltip("Lotuses within (closest distance * this) count as reachable from this bubble.")]
    public float neighborTolerance = 1.3f;
    public List<Lotus> neighbors = new List<Lotus>();
    public bool autoDetectNeighbors = true;

    [Header("Display")]
    [Tooltip("World-space TextMeshPro showing the remaining count.")]
    public TMPro.TextMeshPro countLabel;
    [Tooltip("The sphere's Renderer — it gets tinted to the next axolotl's color.")]
    public Renderer sphereRenderer;
    [Tooltip("URP/Lit = _BaseColor. Standard = _Color.")]
    public string sphereColorProperty = "_BaseColor";

    [Header("Sphere colors per axolotl color")]
    public Color blueColor   = new Color(0.40f, 0.65f, 1.00f, 0.6f);
    public Color purpleColor = new Color(0.70f, 0.45f, 0.95f, 0.6f);
    public Color pinkColor   = new Color(1.00f, 0.55f, 0.80f, 0.6f);
    public Color yellowColor = new Color(1.00f, 0.85f, 0.35f, 0.6f);

    [Header("Jump animation")]
    public float jumpHeight = 1.2f;
    public float jumpTime = 0.5f;
    public Ease jumpEase = Ease.OutQuad;

    [Header("Released axolotl facing")]
    public float facingYOffset = 180f;   // so it faces the player when it lands

    private bool releasing = false;

    public bool IsEmpty => queue.Count == 0;
    public AxolotlColor.ColorType NextColor => queue.Count > 0 ? queue[0] : default;

    private void Start()
    {
        if (autoDetectNeighbors) DetectNeighbors();
        UpdateDisplay();
        BubbleManager.Register(this);
    }

    private void OnDestroy() => BubbleManager.Unregister(this);

    public void DetectNeighbors()
    {
        neighbors.Clear();
        float closest = float.MaxValue;
        foreach (var l in Lotus.All)
        {
            float d = Vector3.Distance(transform.position, l.StandPosition);
            if (d < closest) closest = d;
        }
        if (closest == float.MaxValue) return;
        float maxN = closest * neighborTolerance;
        foreach (var l in Lotus.All)
            if (Vector3.Distance(transform.position, l.StandPosition) <= maxN)
                neighbors.Add(l);
    }

    // Is there a free neighbor lotus this bubble could release onto?
    public Lotus FirstFreeNeighbor()
    {
        foreach (var l in neighbors)
            if (l != null && l.IsFree) return l;
        return null;
    }

    // Release the next axolotl onto the given lotus.
    public void ReleaseOnto(Lotus lotus)
    {
        if (IsEmpty || releasing || lotus == null || !lotus.IsFree) return;

        releasing = true;
        AxolotlColor.ColorType color = queue[0];
        queue.RemoveAt(0);

        // reserve the lotus immediately so nothing else grabs it
        // (occupant set when the axolotl's mover takes over on landing)
        GameObject prefab = PrefabFor(color);
        Vector3 start = transform.position;
        Quaternion rot = Quaternion.Euler(0f, facingYOffset, 0f);

        GameObject go = prefab != null ? Instantiate(prefab, start, rot)
                                       : new GameObject("Axolotl_" + color);

        // Make sure its color label matches.
        var ac = go.GetComponent<AxolotlColor>();
        if (ac != null) { ac.colorType = color; ac.Apply(); }

        // Disable its mover until it lands, so the player can't grab mid-jump.
        var mover = go.GetComponent<AxolotlMover>();
        if (mover != null) mover.enabled = false;

        // Reserve the lotus IMMEDIATELY (before the jump) so no other bubble or
        // axolotl grabs it during the 0.5s jump.
        if (mover != null) lotus.SetOccupant(mover);

        Vector3 land = lotus.StandPosition;
        go.transform.DOJump(land, jumpHeight, 1, jumpTime).SetEase(jumpEase)
          .OnComplete(() =>
          {
              if (mover != null) mover.enabled = true;   // now player-controllable
              if (mover != null) mover.PlaceOnLotus(lotus);  // clean placement + occupancy
              else go.transform.position = land;
              releasing = false;
              UpdateDisplay();
              // After releasing, a chain reaction may free other spots; let manager re-check.
              BubbleManager.NotifyChanged();
          });

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // count text
        if (countLabel != null) countLabel.text = queue.Count.ToString();

        // tint the sphere to the NEXT axolotl's color
        if (sphereRenderer != null && !IsEmpty)
        {
            Color c = ColorFor(queue[0]);
            var mpb = new MaterialPropertyBlock();
            sphereRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(sphereColorProperty, c);
            sphereRenderer.SetPropertyBlock(mpb);
        }
    }

    private Color ColorFor(AxolotlColor.ColorType c)
    {
        switch (c)
        {
            case AxolotlColor.ColorType.Blue:   return blueColor;
            case AxolotlColor.ColorType.Purple: return purpleColor;
            case AxolotlColor.ColorType.Pink:   return pinkColor;
            case AxolotlColor.ColorType.Yellow: return yellowColor;
            default: return Color.white;
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        foreach (var n in neighbors)
            if (n != null) Gizmos.DrawLine(transform.position, n.StandPosition);
    }
}
