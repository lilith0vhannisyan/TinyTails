using UnityEngine;
using DG.Tweening;

// Put this on an axolotl that should start ENCASED IN ICE.
// - Shows a number (e.g. 5). Every time ANY axolotl exits ANY portal, the number drops by 1.
// - While iced, the axolotl cannot be moved (AxolotlMover checks IsFrozen).
// - While iced, the idle animation plays slower (idleSpeedWhileIced).
// - When the number reaches 0, the ice breaks (scale-pop + optional particle), and
//   the axolotl becomes movable again.
//
// Requires: AxolotlMover on the same object (it reads IsFrozen).
[RequireComponent(typeof(AxolotlMover))]
public class AxolotlIce : MonoBehaviour
{
    [Header("Ice ON/OFF")]
    [Tooltip("Tick this to make the axolotl start ENCASED IN ICE. Leave unticked for a normal axolotl.")]
    public bool startIced = false;

    [Header("Ice counter")]
    [Tooltip("How many portal-exits must happen before this ice breaks.")]
    public int breakAfter = 5;

    [Header("Visuals")]
    [Tooltip("The ice cube/shell object wrapped around the axolotl. It is destroyed when ice breaks.")]
    public GameObject iceObject;
    [Tooltip("World-space TextMeshPro showing the remaining count.")]
    public TMPro.TextMeshPro countLabel;
    [Tooltip("Optional particle prefab spawned when the ice shatters.")]
    public GameObject breakEffect;

    [Header("Idle slowdown while iced")]
    [Tooltip("Animator speed while frozen (1 = normal, 0.3 = much slower).")]
    public float idleSpeedWhileIced = 0.3f;

    [Header("Break animation")]
    public float breakPopScale = 1.2f;
    public float breakPopTime = 0.25f;
    [Tooltip("Pause (seconds) showing the final '0' before the ice shatters, so the player notices.")]
    public float breakDelay = 0.4f;

    [Header("Ice appear")]
    [Tooltip("The scale the ice should be when fully shown. Set this to your ice's normal scale (e.g. 1,1,1).")]
    public Vector3 iceShownScale = Vector3.one;
    [Tooltip("Animate the ice popping in. If off, it just appears instantly.")]
    public bool animateAppear = true;

    [Header("Count-change pop (when number drops)")]
    [Tooltip("How big the number punches when it changes (0.5 = +50%).")]
    public float countPunchScale = 0.5f;
    [Tooltip("How long the punch lasts.")]
    public float countPunchTime = 0.3f;
    [Tooltip("Optional color flash when the number drops.")]
    public bool flashColor = true;
    public Color flashTo = Color.cyan;

    private int remaining;
    private AxolotlMover mover;
    private Animator animator;
    private float normalAnimSpeed = 1f;
    private bool subscribed = false;
    private Vector3 labelBaseScale = Vector3.one;
    private Color labelBaseColor = Color.white;
    private Vector3 iceLocalPos;
    private Quaternion iceLocalRot;
    private bool breaking = false;

    // The mover reads this to know it can't be controlled.
    public bool IsFrozen { get; private set; }

    private void Awake()
    {
        mover = GetComponent<AxolotlMover>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Capture the ice's REAL scale from the editor right now, before we touch it.
        // This works even if the ice child is inactive (its localScale is still readable).
        if (iceObject != null)
        {
            Vector3 s = iceObject.transform.localScale;
            if (s != Vector3.zero) iceShownScale = s;   // remember the true size
            iceLocalPos = iceObject.transform.localPosition;     // remember exact place
            iceLocalRot = iceObject.transform.localRotation;
        }

        // Remember the label's base look so the punch can return to it.
        if (countLabel != null)
        {
            labelBaseScale = countLabel.transform.localScale;
            labelBaseColor = countLabel.color;
        }
    }

    private void OnEnable()
    {
        if (startIced)
            Freeze(breakAfter);     // start frozen with the editor count
        else
        {
            // normal axolotl — make sure ice visuals are hidden
            IsFrozen = false;
            if (iceObject != null) iceObject.SetActive(false);
            if (countLabel != null) countLabel.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // ---- PUBLIC: call this anytime to FREEZE the axolotl with a given count ----
    public void Freeze(int count)
    {
        remaining = count;
        IsFrozen = true;
        if (mover != null) mover.SetFrozen(true);

        if (animator != null)
        {
            normalAnimSpeed = 1f;
            animator.speed = idleSpeedWhileIced;
        }

        if (iceObject != null)
        {
            iceObject.SetActive(true);
            iceObject.transform.localPosition = iceLocalPos;
            iceObject.transform.localRotation = iceLocalRot;

            if (animateAppear)
            {
                // Kill any lingering tweens on this transform first
                iceObject.transform.DOKill();
                iceObject.transform.localScale = iceShownScale * 0.8f;
                iceObject.transform.DOScale(iceShownScale, breakPopTime).SetEase(Ease.OutBack)
                    .OnUpdate(() =>
                    {
                        // Safe Check: position lock only triggers if the ice still exists
                        if (iceObject != null) iceObject.transform.localPosition = iceLocalPos;
                    });
            }
            else
            {
                iceObject.transform.localScale = iceShownScale;
            }
        }
        if (countLabel != null) countLabel.gameObject.SetActive(true);

        UpdateLabel();
        Subscribe();
    }

    //public void Freeze(int count)
    //{
    //    remaining = count;
    //    IsFrozen = true;
    //    if (mover != null) mover.SetFrozen(true);

    //    if (animator != null)
    //    {
    //        normalAnimSpeed = 1f;
    //        animator.speed = idleSpeedWhileIced;   // slow idle while iced
    //    }

    //    // show the ice
    //    if (iceObject != null)
    //    {
    //        iceObject.SetActive(true);

    //        // Always restore the exact place you set up in the editor.
    //        iceObject.transform.localPosition = iceLocalPos;
    //        iceObject.transform.localRotation = iceLocalRot;

    //        if (animateAppear)
    //        {
    //            // Pop from 80% -> 100% of the real scale (NOT from zero), so an
    //            // off-center pivot doesn't make the ice look offset.
    //            iceObject.transform.localScale = iceShownScale * 0.8f;
    //            iceObject.transform.DOScale(iceShownScale, breakPopTime).SetEase(Ease.OutBack)
    //                .OnUpdate(() =>
    //                {
    //                    // keep it locked to the set position while scaling
    //                    if (iceObject != null) iceObject.transform.localPosition = iceLocalPos;
    //                });
    //        }
    //        else
    //        {
    //            iceObject.transform.localScale = iceShownScale;   // instant, guaranteed visible
    //        }
    //    }
    //    if (countLabel != null) countLabel.gameObject.SetActive(true);

    //    UpdateLabel();
    //    Subscribe();
    //}

    // ---- PUBLIC: call this anytime to UNFREEZE immediately (with break effect) ----
    public void Unfreeze()
    {
        Break();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        AxolotlMover.OnPortalReached += OnAnyPortalExit;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        AxolotlMover.OnPortalReached -= OnAnyPortalExit;
        subscribed = false;
    }

    private void OnAnyPortalExit(AxolotlMover who)
    {
        if (!IsFrozen) return;
        // Don't count this axolotl's own exit (it's iced, it can't exit anyway,
        // but guard against odd cases).
        if (who == mover) return;

        remaining--;
        UpdateLabel();
        PunchCount();   // animate the number so the player notices it dropped

        if (remaining <= 0)
        {
            // Stop counting further exits, but keep the axolotl frozen (visually iced)
            // and wait a beat showing '0' before the ice actually shatters.
            if (breaking) return;
            breaking = true;
            Unsubscribe();
            Invoke(nameof(Break), breakDelay);
        }
    }

    // DOTween pop + optional color flash on the count label.
    private void PunchCount()
    {
        if (countLabel == null) return;

        Transform t = countLabel.transform;
        t.DOKill();
        t.localScale = labelBaseScale;
        t.DOPunchScale(labelBaseScale * countPunchScale, countPunchTime, 6, 0.8f);

        if (flashColor)
        {
            // Flash to the highlight color, then lerp back (no DOTween TMP module needed).
            countLabel.color = flashTo;
            DOTween.To(() => 0f, x =>
            {
                countLabel.color = Color.Lerp(flashTo, labelBaseColor, x);
            }, 1f, countPunchTime);
        }
    }

    private void UpdateLabel()
    {
        if (countLabel != null) countLabel.text = Mathf.Max(0, remaining).ToString();
    }

    private void Break()
    {
        if (!IsFrozen) return;

        IsFrozen = false;
        breaking = false;
        if (mover != null) mover.SetFrozen(false);
        Unsubscribe();

        if (animator != null) animator.speed = 1f;

        if (breakEffect != null)
            Instantiate(breakEffect, transform.position, Quaternion.identity);

        if (iceObject != null)
        {
            iceObject.transform.DOKill();
            iceObject.transform.DOScale(iceObject.transform.localScale * breakPopScale, breakPopTime * 0.4f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Safe Check: verify ice object hasn't been destroyed by a scene reload
                    if (iceObject != null)
                    {
                        iceObject.transform.DOScale(Vector3.zero, breakPopTime * 0.6f)
                            .SetEase(Ease.InBack)
                            .OnComplete(() => { if (iceObject != null) Destroy(iceObject); });
                    }
                });
        }

        if (countLabel != null) countLabel.gameObject.SetActive(false);
    }

    //private void Break()
    //{
    //    if (!IsFrozen) return;   // already fully broken

    //    IsFrozen = false;
    //    breaking = false;
    //    if (mover != null) mover.SetFrozen(false);
    //    Unsubscribe();

    //    // restore animation speed to normal
    //    if (animator != null) animator.speed = 1f;

    //    // spawn shatter particle
    //    if (breakEffect != null)
    //        Instantiate(breakEffect, transform.position, Quaternion.identity);

    //    // pop + destroy the ice shell (DOTween effect)
    //    if (iceObject != null)
    //    {
    //        iceObject.transform.DOScale(iceObject.transform.localScale * breakPopScale, breakPopTime * 0.4f)
    //            .SetEase(Ease.OutQuad)
    //            .OnComplete(() =>
    //            {
    //                iceObject.transform.DOScale(Vector3.zero, breakPopTime * 0.6f)
    //                    .SetEase(Ease.InBack)
    //                    .OnComplete(() => { if (iceObject != null) Destroy(iceObject); });
    //            });
    //    }

    //    if (countLabel != null) countLabel.gameObject.SetActive(false);
    //}
}
