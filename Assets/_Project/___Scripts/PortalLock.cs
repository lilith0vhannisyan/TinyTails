using UnityEngine;
using DG.Tweening;

// Put this on a Portal that should start LOCKED.
// - Shows a number (e.g. 3). Every time ANY axolotl exits ANY portal, the number drops by 1.
//   The number punches/flashes when it drops so the player notices.
// - While locked, the portal rejects axolotls (Portal checks IsLocked).
// - When the number reaches 0, the lock OPENS:
//     1) the whole lock rises a little,
//     2) the tile (lid) lifts in Y to mimic opening,
//     3) everything shrinks/vanishes via DOTween and is destroyed.
//
// Lock has two parts you drag in: the BODY (base) and the TILE (lid that lifts).
//
// Requires: Portal on the same object (it reads IsLocked).
[RequireComponent(typeof(Portal))]
public class PortalLock : MonoBehaviour
{
    [Header("Lock ON/OFF")]
    [Tooltip("Tick this to make the portal start LOCKED. Leave unticked for a normal portal.")]
    public bool startLocked = false;

    [Header("Lock counter")]
    [Tooltip("How many portal-exits must happen before this lock opens.")]
    public int openAfter = 3;

    [Header("Lock parts")]
    [Tooltip("The lock body (base).")]
    public GameObject lockBody;
    [Tooltip("The lock tile/lid that lifts up in Y when opening.")]
    public Transform lockTile;
    [Tooltip("World-space TextMeshPro showing the remaining count.")]
    public TMPro.TextMeshPro countLabel;
    [Tooltip("Optional particle spawned when the lock opens.")]
    public GameObject openEffect;

    [Header("Open animation")]
    [Tooltip("How much the WHOLE lock rises first (Y).")]
    public float lockRiseHeight = 0.25f;
    public float lockRiseTime = 0.25f;
    [Tooltip("How much the TILE lifts in Y to mimic opening.")]
    public float tileLiftHeight = 0.5f;
    public float tileLiftTime = 0.3f;
    [Tooltip("How long the final vanish (shrink) takes.")]
    public float vanishTime = 0.3f;
    public Ease riseEase = Ease.OutQuad;
    public Ease tileEase = Ease.OutBack;
    [Tooltip("Pause showing '0' before the lock opens.")]
    public float openDelay = 0.35f;

    [Header("Count-change pop (when number drops)")]
    public float countPunchScale = 0.5f;
    public float countPunchTime = 0.3f;
    public bool flashColor = true;
    public Color flashTo = Color.red;

    private int remaining;
    private Portal portal;
    private bool subscribed = false;
    private bool opening = false;

    private Vector3 labelBaseScale = Vector3.one;
    private Color labelBaseColor = Color.white;

    // The Portal reads this to know it must reject axolotls.
    public bool IsLocked { get; private set; }

    private void Awake()
    {
        portal = GetComponent<Portal>();
        if (countLabel != null)
        {
            labelBaseScale = countLabel.transform.localScale;
            labelBaseColor = countLabel.color;
        }
    }

    private void OnEnable()
    {
        if (!startLocked)
        {
            IsLocked = false;
            if (lockBody != null) lockBody.SetActive(false);
            if (lockTile != null) lockTile.gameObject.SetActive(false);
            if (countLabel != null) countLabel.gameObject.SetActive(false);
            return;
        }

        remaining = openAfter;
        IsLocked = true;
        if (portal != null) portal.SetLocked(true);

        if (lockBody != null) lockBody.SetActive(true);
        if (lockTile != null) lockTile.gameObject.SetActive(true);
        if (countLabel != null) countLabel.gameObject.SetActive(true);

        UpdateLabel();
        Subscribe();
    }

    private void OnDisable() => Unsubscribe();

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
        if (!IsLocked || opening) return;

        remaining--;
        UpdateLabel();
        PunchCount();

        if (remaining <= 0)
        {
            opening = true;
            Unsubscribe();
            Invoke(nameof(Open), openDelay);   // show '0' a beat, then open
        }
    }

    private void UpdateLabel()
    {
        if (countLabel != null) countLabel.text = Mathf.Max(0, remaining).ToString();
    }

    // DOTween pop + color flash on the count label.
    private void PunchCount()
    {
        if (countLabel == null) return;

        Transform t = countLabel.transform;
        t.DOKill();
        t.localScale = labelBaseScale;
        t.DOPunchScale(labelBaseScale * countPunchScale, countPunchTime, 6, 0.8f);

        if (flashColor)
        {
            countLabel.color = flashTo;
            DOTween.To(() => 0f, x =>
            {
                if (countLabel != null) countLabel.color = Color.Lerp(flashTo, labelBaseColor, x);
            }, 1f, countPunchTime);
        }
    }

    private void Open()
    {
        IsLocked = false;
        if (portal != null) portal.SetLocked(false);

        if (countLabel != null) countLabel.gameObject.SetActive(false);

        if (openEffect != null)
            Instantiate(openEffect, transform.position, Quaternion.identity);

        Sequence seq = DOTween.Sequence();

        // 1) the WHOLE lock rises a little
        Transform whole = transform;
        if (lockBody != null) whole = lockBody.transform;   // rise the body group
        Vector3 wholeUp = whole.position + Vector3.up * lockRiseHeight;
        seq.Append(whole.DOMove(wholeUp, lockRiseTime).SetEase(riseEase));

        // 2) the TILE lifts further in Y (mimics opening the lid)
        if (lockTile != null)
        {
            Vector3 tileUp = lockTile.position + Vector3.up * (lockRiseHeight + tileLiftHeight);
            seq.Append(lockTile.DOMove(tileUp, tileLiftTime).SetEase(tileEase));
        }

        // 3) everything shrinks away (vanish), then destroy
        if (lockTile != null)
            seq.Append(lockTile.DOScale(Vector3.zero, vanishTime).SetEase(Ease.InBack));
        if (lockBody != null)
            seq.Join(lockBody.transform.DOScale(Vector3.zero, vanishTime).SetEase(Ease.InBack));

        seq.OnComplete(() =>
        {
            if (lockTile != null) Destroy(lockTile.gameObject);
            if (lockBody != null) Destroy(lockBody);
        });

        enabled = false;
    }
}
