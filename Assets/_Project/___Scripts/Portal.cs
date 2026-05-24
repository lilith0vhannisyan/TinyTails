using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;   // requires DOTween (free on the Asset Store)

// Put this on each Portal. The portal is a WALKABLE target (like a lotus):
// the axolotl can path to it from a neighboring lotus, and enters if the color matches.
public class Portal : MonoBehaviour
{
    [Header("Portal color")]
    public AxolotlColor.ColorType portalColor = AxolotlColor.ColorType.Pink;

    [Header("Where the axolotl stands/floats into")]
    [Tooltip("Empty child at the portal center. Defaults to this transform.")]
    public Transform standPoint;

    [Header("Enter animation")]
    public float riseHeight = 0.6f;
    public float riseTime = 0.25f;
    public float moveTime = 0.5f;
    public float spinDegrees = 360f;     // optional spin as he goes in
    public Ease riseEase = Ease.OutBack;
    public Ease moveEase = Ease.InOutSine;

    [Header("Disappear effect")]
    [Tooltip("Optional particle prefab spawned at the portal when he vanishes.")]
    public GameObject vanishEffect;

    [Header("Tray")]
    [Tooltip("The collection tray this portal feeds. If empty, uses the first tray in the scene.")]
    public CollectionTray tray;

    // All portals, so the mover can treat them as walkable neighbors.
    public static readonly List<Portal> All = new List<Portal>();

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        if (standPoint == null) standPoint = transform;
    }
    private void OnDisable() => All.Remove(this);

    private void Awake()
    {
        if (standPoint == null) standPoint = transform;
        if (tray == null) tray = FindFirstObjectByType<CollectionTray>();
    }

    public Vector3 StandPosition => standPoint != null ? standPoint.position : transform.position;
    public Vector3 Position => StandPosition;

    public bool Accepts(AxolotlColor axo)
    {
        return axo != null && axo.colorType == portalColor;
    }

    // Plays the enter+disappear animation, then notifies the tray. onComplete fires after.
    public void EnterPortal(Transform axolotl, System.Action onComplete = null)
    {
        Vector3 start = axolotl.position;
        Vector3 risePos = start + Vector3.up * riseHeight;
        Vector3 endPos = Position;
        Vector3 startScale = axolotl.localScale;

        Sequence seq = DOTween.Sequence();
        // 1) pop up
        seq.Append(axolotl.DOMove(risePos, riseTime).SetEase(riseEase));
        // 2) float into the center while shrinking + spinning (disappear)
        seq.Append(axolotl.DOMove(endPos, moveTime).SetEase(moveEase));
        seq.Join(axolotl.DOScale(Vector3.zero, moveTime).SetEase(Ease.InBack));
        if (spinDegrees != 0f)
            seq.Join(axolotl.DORotate(new Vector3(0f, spinDegrees, 0f), moveTime, RotateMode.LocalAxisAdd));
        // 3) done -> spawn effect, tell the tray, finish
        seq.OnComplete(() =>
        {
            if (vanishEffect != null)
                Instantiate(vanishEffect, endPos, Quaternion.identity);

            if (tray != null)
                tray.AddAxolotl(portalColor);

            axolotl.localScale = startScale; // reset in case object is reused/pooled
            onComplete?.Invoke();
        });
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor(portalColor);
        Vector3 p = standPoint != null ? standPoint.position : transform.position;
        Gizmos.DrawWireSphere(p, 0.4f);
    }

    private Color GizmoColor(AxolotlColor.ColorType t)
    {
        switch (t)
        {
            case AxolotlColor.ColorType.Blue:   return Color.cyan;
            case AxolotlColor.ColorType.Purple: return new Color(0.6f, 0.3f, 0.9f);
            case AxolotlColor.ColorType.Pink:   return new Color(1f, 0.4f, 0.7f);
            case AxolotlColor.ColorType.Yellow: return Color.yellow;
            default: return Color.white;
        }
    }
}
