//using UnityEngine;
//#if ENABLE_INPUT_SYSTEM
//using UnityEngine.InputSystem;
//#endif

//// Put this on the Axolotl. Needs: Collider, Animator (bool "IsWalking"), AxolotlColor.
////
//// SIMPLE MODEL:
////   Press on the axolotl, slide a direction -> he walks to the next connected
////   lotus in that direction and STOPS there. Slide again -> next step.
////   One slide = one step. He only moves to touching (connected) lotuses.
//[RequireComponent(typeof(Animator))]
//public class AxolotlMover : MonoBehaviour
//{
//    private enum State { Idle, Walking, TurningToFace, Entering }

//    [Header("References")]
//    public Camera cam;
//    public Animator animator;
//    public Collider tapCollider;
//    public AxolotlColor axolotlColor;

//    [Header("Movement")]
//    public float moveSpeed = 3f;
//    public float arriveDistance = 0.05f;

//    [Header("Rotation")]
//    public float rotationSpeed = 540f;
//    public float facingOffset = 0f;
//    public bool faceWalkDirection = true;

//    [Header("Input")]
//    [Tooltip("Minimum forward-ness to accept a flick (prevents going backward). 0 = any forward, lower = more lenient. Try 0.0 to 0.2.")]
//    [Range(-1f, 1f)] public float directionTolerance = 0.0f;
//    [Tooltip("Minimum finger flick (pixels) to register. Lower = more responsive.")]
//    public float slideThreshold = 20f;

//    [Header("Animator")]
//    public string isWalkingParam = "IsWalking";

//    private State state = State.Idle;
//    private Lotus currentLotus;
//    private Lotus targetLotus;
//    private Portal targetPortal;

//    private bool isHeld = false;
//    private Vector3 heldDir;               // current pointing direction while held
//    private Vector2 pressStart;
//    private Quaternion faceCameraRot;   // resting rotation = faces the player/screen

//    // --- ICE ---
//    // When frozen (encased in ice), the axolotl cannot be controlled or moved.
//    private bool frozen = false;
//    public void SetFrozen(bool value) => frozen = value;
//    public bool IsFrozen => frozen;

//    private void Awake()
//    {
//        if (cam == null) cam = Camera.main;
//        if (animator == null) animator = GetComponent<Animator>();
//        if (tapCollider == null) tapCollider = GetComponent<Collider>();
//        if (axolotlColor == null) axolotlColor = GetComponent<AxolotlColor>();
//    }

//    private void Start()
//    {
//        faceCameraRot = transform.rotation;   // whatever you set in editor = facing player
//        // If we weren't already placed (e.g. by a bubble), find our lotus.
//        if (currentLotus == null)
//        {
//            currentLotus = FindNearestLotus(transform.position);
//            if (currentLotus != null)
//            {
//                currentLotus.SetOccupant(this);
//                transform.position = currentLotus.StandPosition;
//            }
//        }
//    }

//    // Explicitly place this axolotl on a lotus (used by the bubble on landing).
//    public void PlaceOnLotus(Lotus lotus)
//    {
//        if (lotus == null) return;
//        currentLotus = lotus;
//        lotus.SetOccupant(this);
//        transform.position = lotus.StandPosition;
//        faceCameraRot = transform.rotation;
//        state = State.Idle;
//    }

//    private void Update()
//    {
//        HandleInput();
//        if (state == State.Walking) WalkToTarget();
//        else if (state == State.TurningToFace)
//        {
//            SetWalking(false);
//            if (RotateToward(faceCameraRot))
//                state = State.Idle;   // settled facing player -> accept input
//        }
//    }

//    // ---------- INPUT: hold + point a direction -> he walks the path ----------
//    private void HandleInput()
//    {
//        // Iced axolotls can't be grabbed or moved.
//        if (frozen) { isHeld = false; heldDir = Vector3.zero; return; }

//        if (GetPointerDown(out Vector2 down))
//        {
//            if (IsPointerOnAxolotl(down))
//            {
//                isHeld = true;
//                pressStart = down;
//                heldDir = Vector3.zero;
//            }
//        }

//        if (isHeld && GetPointerHeld(out Vector2 cur))
//        {
//            Vector2 slide = cur - pressStart;
//            if (slide.magnitude >= slideThreshold)
//                heldDir = ScreenSlideToWorldDir(slide);   // current pointing direction
//            else
//                heldDir = Vector3.zero;                   // not pointing yet

//            // If standing idle and pointing somewhere, start walking that way.
//            if (state == State.Idle && heldDir.sqrMagnitude > 0.001f)
//                TryStartStep(heldDir);
//        }

//        if (GetPointerUp())
//        {
//            isHeld = false;
//            heldDir = Vector3.zero;
//            // he will finish the current step, then face player & idle (handled on arrival)
//        }
//    }

//    // Begin walking toward the next lotus/portal in the given direction.
//    private void TryStartStep(Vector3 dir)
//    {
//        Portal portal = PickPortalInDirection(currentLotus, dir);
//        if (portal != null)
//        {
//            // release any stale lotus reservation (we're heading to a portal)
//            if (targetLotus != null && targetLotus != currentLotus) targetLotus.Clear();
//            targetPortal = portal;
//            targetLotus = null;
//            state = State.Walking;
//            SetWalking(true);
//            return;
//        }
//        Lotus next = PickNeighborInDirection(currentLotus, dir);
//        if (next != null)
//        {
//            // release a previous, un-reached target reservation so it doesn't leak.
//            if (targetLotus != null && targetLotus != next && targetLotus != currentLotus)
//                targetLotus.Clear();

//            targetLotus = next;
//            targetPortal = null;
//            targetLotus.SetOccupant(this);
//            state = State.Walking;
//            SetWalking(true);

//            // Trigger soft vibration on moving towards a regular lotus node
//            if (GameManager.Instance != null) GameManager.Instance.TriggerLightVibration();
//        }
//    }

//    // ---------- WALK to target (lotus or portal), then stop/enter ----------
//    private void WalkToTarget()
//    {
//        // Walking to a PORTAL?
//        if (targetPortal != null)
//        {
//            Vector3 pdest = targetPortal.StandPosition;
//            Vector3 pflat = new Vector3(pdest.x, transform.position.y, pdest.z);
//            Vector3 pto = pflat - transform.position;

//            if (faceWalkDirection) SmoothFace(pto);

//            if (pto.magnitude <= arriveDistance)
//            {
//                // Arrived at the portal. Enter if color matches.
//                if (targetPortal.Accepts(axolotlColor))
//                {
//                    // Free ALL lotuses this axolotl holds RIGHT NOW (before the
//                    // portal animation), so the board updates immediately.
//                    foreach (var lotus in Lotus.All)
//                        if (lotus != null && lotus.occupant == this)
//                            lotus.Clear();
//                    currentLotus = null;

//                    Portal p = targetPortal;
//                    targetPortal = null;
//                    state = State.Entering;
//                    SetWalking(false);
//                    p.EnterPortal(transform, OnEnteredPortal);
//                    return;
//                }
//                else
//                {
//                    // Wrong color — don't enter; just stop and face player.
//                    targetPortal = null;
//                    state = State.TurningToFace;
//                    SetWalking(false);
//                    return;
//                }
//            }

//            transform.position += pto.normalized * moveSpeed * Time.deltaTime;
//            SetWalking(true);
//            return;
//        }

//        if (targetLotus == null) { state = State.TurningToFace; SetWalking(false); return; }

//        Vector3 dest = targetLotus.StandPosition;
//        Vector3 flatDest = new Vector3(dest.x, transform.position.y, dest.z);
//        Vector3 toDest = flatDest - transform.position;

//        if (faceWalkDirection) SmoothFace(toDest);

//        if (toDest.magnitude <= arriveDistance)
//        {
//            transform.position = flatDest;
//            if (currentLotus != null && currentLotus != targetLotus) currentLotus.Clear();
//            currentLotus = targetLotus;
//            targetLotus = null;

//            // Still holding and pointing a direction? Try to continue to the next lotus.
//            if (isHeld && heldDir.sqrMagnitude > 0.001f)
//            {
//                TryStartStep(heldDir);
//                // Did we actually get a new target? If so, keep walking (no turn).
//                if (targetLotus != null || targetPortal != null) return;
//            }

//            // Released, or no lotus further in this direction -> stop & face player.
//            state = State.TurningToFace;
//            SetWalking(false);
//            return;
//        }

//        Vector3 d = toDest.normalized;
//        transform.position += d * moveSpeed * Time.deltaTime;
//        SetWalking(true);
//    }

//    // Pick a matching-color portal neighbor in the flick direction.
//    private Portal PickPortalInDirection(Lotus from, Vector3 worldDir)
//    {
//        if (from == null || from.portalNeighbors == null) return null;
//        worldDir.y = 0f;
//        if (worldDir.sqrMagnitude < 0.0001f) return null;
//        worldDir.Normalize();

//        Portal best = null;
//        float bestDot = -2f;
//        foreach (var p in from.portalNeighbors)
//        {
//            if (p == null) continue;
//            if (!p.Accepts(axolotlColor)) continue;   // only matching-color portals
//            Vector3 to = p.StandPosition - from.StandPosition; to.y = 0f;
//            if (to.sqrMagnitude < 0.0001f) continue;
//            float dot = Vector3.Dot(worldDir, to.normalized);
//            if (dot > bestDot) { bestDot = dot; best = p; }
//        }
//        if (bestDot < directionTolerance) return null;
//        return best;
//    }

//    // Pick the connected free neighbor in the direction your flick points.
//    // The flick is snapped to the actual neighbor angles, so any flick roughly
//    // toward a connected lotus reaches THAT lotus (precise + every neighbor reachable).
//    private Lotus PickNeighborInDirection(Lotus from, Vector3 worldDir)
//    {
//        if (from == null) return null;
//        worldDir.y = 0f;
//        if (worldDir.sqrMagnitude < 0.0001f) return null;
//        worldDir.Normalize();

//        // Among all FREE connected neighbors, pick the one whose direction from
//        // 'from' is closest to your flick. No fixed tolerance gate — we always
//        // pick the best-matching neighbor, so it's never "rejected" for being
//        // slightly off-angle. This makes every connected lotus reachable.
//        Lotus best = null;
//        float bestDot = -2f;   // start below any real dot (-1..1)

//        foreach (var n in from.neighbors)
//        {
//            if (n == null || n == from) continue;
//            if (!n.IsFree) continue;

//            Vector3 to = n.StandPosition - from.StandPosition; to.y = 0f;
//            if (to.sqrMagnitude < 0.0001f) continue;

//            float dot = Vector3.Dot(worldDir, to.normalized);
//            if (dot > bestDot) { bestDot = dot; best = n; }
//        }

//        // Require the best match to be at least generally forward (not behind you),
//        // so flicking up doesn't send him to a lotus that's actually downward.
//        if (bestDot < directionTolerance) return null;
//        return best;
//    }

//    // Convert a screen-space slide into a world ground direction using the camera.
//    private Vector3 ScreenSlideToWorldDir(Vector2 slide)
//    {
//        Vector3 camF = cam.transform.forward; camF.y = 0f; camF.Normalize();
//        Vector3 camR = cam.transform.right;   camR.y = 0f; camR.Normalize();
//        Vector3 world = camR * slide.x + camF * slide.y;
//        world.y = 0f;
//        return world.normalized;
//    }

//    private void SmoothFace(Vector3 moveDir)
//    {
//        moveDir.y = 0f;
//        if (moveDir.sqrMagnitude < 0.0001f) return;
//        Quaternion target = Quaternion.LookRotation(moveDir.normalized, Vector3.up)
//                            * Quaternion.Euler(0f, facingOffset, 0f);
//        transform.rotation = Quaternion.RotateTowards(
//            transform.rotation, target, rotationSpeed * Time.deltaTime);
//    }

//    private bool RotateToward(Quaternion target)
//    {
//        transform.rotation = Quaternion.RotateTowards(
//            transform.rotation, target, rotationSpeed * Time.deltaTime);
//        return Quaternion.Angle(transform.rotation, target) <= 2f;
//    }

//    private void SetWalking(bool walking)
//    {
//        if (animator != null && !string.IsNullOrEmpty(isWalkingParam))
//            animator.SetBool(isWalkingParam, walking);
//    }

//    private void OnEnteredPortal()
//    {
//        // Brute-force safety: free EVERY lotus that still thinks this axolotl
//        // is its occupant, so none get stuck "busy" after he disappears.
//        foreach (var lotus in Lotus.All)
//            if (lotus != null && lotus.occupant == this)
//                lotus.Clear();

//        OnPortalReached?.Invoke(this);
//        gameObject.SetActive(false);
//    }
//    public static System.Action<AxolotlMover> OnPortalReached;

//    private Lotus FindNearestLotus(Vector3 pos)
//    {
//        Lotus best = null; float bestSqr = float.MaxValue;
//        foreach (var lotus in Lotus.All)
//        {
//            if (lotus == null) continue;
//            if (!lotus.IsFree) continue;   // don't claim a lotus another axolotl already holds
//            float d = (lotus.StandPosition - pos).sqrMagnitude;
//            if (d < bestSqr) { bestSqr = d; best = lotus; }
//        }
//        // Fallback: if somehow all are occupied, take the closest regardless.
//        if (best == null)
//        {
//            foreach (var lotus in Lotus.All)
//            {
//                if (lotus == null) continue;
//                float d = (lotus.StandPosition - pos).sqrMagnitude;
//                if (d < bestSqr) { bestSqr = d; best = lotus; }
//            }
//        }
//        return best;
//    }

//    private bool IsPointerOnAxolotl(Vector2 screenPos)
//    {
//        if (cam == null || tapCollider == null) return false;
//        Ray ray = cam.ScreenPointToRay(screenPos);
//        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
//            return hit.collider == tapCollider;
//        return false;
//    }

//#if ENABLE_INPUT_SYSTEM
//    private bool prevPressed = false;
//    private bool TryGetPointer(out Vector2 pos, out bool pressed)
//    {
//        pos = Vector2.zero; pressed = false;
//        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
//        { pos = Touchscreen.current.primaryTouch.position.ReadValue(); pressed = true; return true; }
//        if (Mouse.current != null)
//        { pos = Mouse.current.position.ReadValue(); pressed = Mouse.current.leftButton.isPressed; return true; }
//        return false;
//    }
//    private bool GetPointerDown(out Vector2 pos) { TryGetPointer(out pos, out bool p); return p && !prevPressed; }
//    private bool GetPointerHeld(out Vector2 pos) { TryGetPointer(out pos, out bool p); return p; }
//    private bool GetPointerUp() { TryGetPointer(out _, out bool p); bool up = !p && prevPressed; prevPressed = p; return up; }
//#else
//    private bool GetPointerDown(out Vector2 pos)
//    {
//        if (Input.touchCount > 0) { Touch t = Input.GetTouch(0); pos = t.position; return t.phase == TouchPhase.Began; }
//        pos = Input.mousePosition; return Input.GetMouseButtonDown(0);
//    }
//    private bool GetPointerHeld(out Vector2 pos)
//    {
//        if (Input.touchCount > 0) { Touch t = Input.GetTouch(0); pos = t.position; return t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary; }
//        pos = Input.mousePosition; return Input.GetMouseButton(0);
//    }
//    private bool GetPointerUp()
//    {
//        if (Input.touchCount > 0) { Touch t = Input.GetTouch(0); return t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled; }
//        return Input.GetMouseButtonUp(0);
//    }
//#endif
//}
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Put this on the Axolotl. Needs: Collider, Animator (bool "IsWalking"), AxolotlColor.
//
// SIMPLE MODEL:
//    Press on the axolotl, slide a direction -> he walks to the next connected
//    lotus in that direction and STOPS there. Slide again -> next step.
//    One slide = one step. He only moves to touching (connected) lotuses.
[RequireComponent(typeof(Animator))]
public class AxolotlMover : MonoBehaviour
{
    private enum State { Idle, Walking, TurningToFace, Entering }

    [Header("References")]
    public Camera cam;
    public Animator animator;
    public Collider tapCollider;
    public AxolotlColor axolotlColor;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float arriveDistance = 0.05f;

    [Header("Rotation")]
    public float rotationSpeed = 540f;
    public float facingOffset = 0f;
    public bool faceWalkDirection = true;

    [Header("Input")]
    [Tooltip("Minimum forward-ness to accept a flick (prevents going backward). 0 = any forward, lower = more lenient. Try 0.0 to 0.2.")]
    [Range(-1f, 1f)] public float directionTolerance = 0.0f;
    [Tooltip("Minimum finger flick (pixels) to register. Lower = more responsive.")]
    public float slideThreshold = 20f;

    [Header("Animator")]
    public string isWalkingParam = "IsWalking";

    private State state = State.Idle;
    private Lotus currentLotus;
    private Lotus targetLotus;
    private Portal targetPortal;

    private bool isHeld = false;
    private Vector3 heldDir;            // current pointing direction while held
    private Vector2 pressStart;
    private Quaternion faceCameraRot;   // resting rotation = faces the player/screen

    // Expose walking state safely for AxolotlPlacer initialization guards
    public bool IsWalking => state == State.Walking || state == State.Entering;

    // --- ICE ---
    // When frozen (encased in ice), the axolotl cannot be controlled or moved.
    private bool frozen = false;
    public void SetFrozen(bool value) => frozen = value;
    public bool IsFrozen => frozen;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (animator == null) animator = GetComponent<Animator>();
        if (tapCollider == null) tapCollider = GetComponent<Collider>();
        if (axolotlColor == null) axolotlColor = GetComponent<AxolotlColor>();
    }

    private void Start()
    {
        faceCameraRot = transform.rotation;   // whatever you set in editor = facing player
        // If we weren't already placed (e.g. by a bubble), find our lotus.
        if (currentLotus == null)
        {
            currentLotus = FindNearestLotus(transform.position);
            if (currentLotus != null)
            {
                currentLotus.SetOccupant(this);
                transform.position = currentLotus.StandPosition;
            }
        }
    }

    // Explicitly place this axolotl on a lotus (used by the bubble on landing).
    public void PlaceOnLotus(Lotus lotus)
    {
        if (lotus == null) return;
        currentLotus = lotus;
        lotus.SetOccupant(this);
        transform.position = lotus.StandPosition;
        faceCameraRot = transform.rotation;
        state = State.Idle;
    }

    private void Update()
    {
        HandleInput();
        if (state == State.Walking) WalkToTarget();
        else if (state == State.TurningToFace)
        {
            SetWalking(false);
            if (RotateToward(faceCameraRot))
                state = State.Idle;   // settled facing player -> accept input
        }
    }

    // ---------- INPUT: hold + point a direction -> he walks the path ----------
    private void HandleInput()
    {
        // Iced axolotls can't be grabbed or moved.
        if (frozen) { isHeld = false; heldDir = Vector3.zero; return; }

        if (GetPointerDown(out Vector2 down))
        {
            if (IsPointerOnAxolotl(down))
            {
                isHeld = true;
                pressStart = down;
                heldDir = Vector3.zero;
            }
        }

        if (isHeld && GetPointerHeld(out Vector2 cur))
        {
            Vector2 slide = cur - pressStart;
            if (slide.magnitude >= slideThreshold)
                heldDir = ScreenSlideToWorldDir(slide);   // current pointing direction
            else
                heldDir = Vector3.zero;                   // not pointing yet

            // If standing idle and pointing somewhere, start walking that way.
            if (state == State.Idle && heldDir.sqrMagnitude > 0.001f)
                TryStartStep(heldDir);
        }

        if (GetPointerUp())
        {
            isHeld = false;
            heldDir = Vector3.zero;
            // he will finish the current step, then face player & idle (handled on arrival)
        }
    }

    // Begin walking toward the next lotus/portal in the given direction.
    private void TryStartStep(Vector3 dir)
    {
        Portal portal = PickPortalInDirection(currentLotus, dir);
        if (portal != null)
        {
            // release any stale lotus reservation (we're heading to a portal)
            if (targetLotus != null && targetLotus != currentLotus) targetLotus.Clear();
            targetPortal = portal;
            targetLotus = null;
            state = State.Walking;
            SetWalking(true);
            return;
        }
        Lotus next = PickNeighborInDirection(currentLotus, dir);
        if (next != null)
        {
            // release a previous, un-reached target reservation so it doesn't leak.
            if (targetLotus != null && targetLotus != next && targetLotus != currentLotus)
                targetLotus.Clear();

            targetLotus = next;
            targetPortal = null;
            targetLotus.SetOccupant(this);
            state = State.Walking;
            SetWalking(true);

            // Trigger soft vibration on moving towards a regular lotus node
            if (GameManager.Instance != null) GameManager.Instance.TriggerLightVibration();
        }
    }

    // ---------- WALK to target (lotus or portal), then stop/enter ----------
    private void WalkToTarget()
    {
        float maxMovementThisFrame = moveSpeed * Time.deltaTime;

        // Walking to a PORTAL?
        if (targetPortal != null)
        {
            Vector3 pdest = targetPortal.StandPosition;
            Vector3 pflat = new Vector3(pdest.x, transform.position.y, pdest.z);
            Vector3 pto = pflat - transform.position;
            float distanceToPortal = pto.magnitude;

            if (faceWalkDirection) SmoothFace(pto);

            // FIX: If movement step overshoots target or hits arrival margin, snap directly
            if (maxMovementThisFrame >= distanceToPortal || distanceToPortal <= arriveDistance)
            {
                transform.position = pflat;

                // Arrived at the portal. Enter if color matches.
                if (targetPortal.Accepts(axolotlColor))
                {
                    // Free ALL lotuses this axolotl holds RIGHT NOW (before the
                    // portal animation), so the board updates immediately.
                    foreach (var lotus in Lotus.All)
                        if (lotus != null && lotus.occupant == this)
                            lotus.Clear();
                    currentLotus = null;

                    Portal p = targetPortal;
                    targetPortal = null;
                    state = State.Entering;
                    SetWalking(false);
                    p.EnterPortal(transform, OnEnteredPortal);
                    return;
                }
                else
                {
                    // Wrong color — don't enter; just stop and face player.
                    targetPortal = null;
                    state = State.TurningToFace;
                    SetWalking(false);
                    return;
                }
            }

            transform.position += pto.normalized * maxMovementThisFrame;
            SetWalking(true);
            return;
        }

        // Safety fallback
        if (targetLotus == null) { state = State.TurningToFace; SetWalking(false); return; }

        Vector3 dest = targetLotus.StandPosition;
        Vector3 flatDest = new Vector3(dest.x, transform.position.y, dest.z);
        Vector3 toDest = flatDest - transform.position;
        float distanceToLotus = toDest.magnitude;

        if (faceWalkDirection) SmoothFace(toDest);

        // FIX: If movement step overshoots target or hits arrival margin, snap directly
        if (maxMovementThisFrame >= distanceToLotus || distanceToLotus <= arriveDistance)
        {
            transform.position = flatDest;
            if (currentLotus != null && currentLotus != targetLotus) currentLotus.Clear();
            currentLotus = targetLotus;
            targetLotus = null;

            // Still holding and pointing a direction? Try to continue to the next lotus.
            if (isHeld && heldDir.sqrMagnitude > 0.001f)
            {
                TryStartStep(heldDir);
                // Did we actually get a new target? If so, keep walking (no turn).
                if (targetLotus != null || targetPortal != null) return;
            }

            // Released, or no lotus further in this direction -> stop & face player.
            state = State.TurningToFace;
            SetWalking(false);
            return;
        }

        transform.position += toDest.normalized * maxMovementThisFrame;
        SetWalking(true);
    }

    // Pick a matching-color portal neighbor in the flick direction.
    private Portal PickPortalInDirection(Lotus from, Vector3 worldDir)
    {
        if (from == null || from.portalNeighbors == null) return null;
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 0.0001f) return null;
        worldDir.Normalize();

        Portal best = null;
        float bestDot = -2f;
        foreach (var p in from.portalNeighbors)
        {
            if (p == null) continue;
            if (!p.Accepts(axolotlColor)) continue;   // only matching-color portals
            Vector3 to = p.StandPosition - from.StandPosition; to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;
            float dot = Vector3.Dot(worldDir, to.normalized);
            if (dot > bestDot) { bestDot = dot; best = p; }
        }
        if (bestDot < directionTolerance) return null;
        return best;
    }

    // Pick the connected free neighbor in the direction your flick points.
    private Lotus PickNeighborInDirection(Lotus from, Vector3 worldDir)
    {
        if (from == null) return null;
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 0.0001f) return null;
        worldDir.Normalize();

        Lotus best = null;
        float bestDot = -2f;   // start below any real dot (-1..1)

        foreach (var n in from.neighbors)
        {
            if (n == null || n == from) continue;
            if (!n.IsFree) continue;

            Vector3 to = n.StandPosition - from.StandPosition; to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;

            float dot = Vector3.Dot(worldDir, to.normalized);
            if (dot > bestDot) { bestDot = dot; best = n; }
        }

        if (bestDot < directionTolerance) return null;
        return best;
    }

    // Convert a screen-space slide into a world ground direction using the camera.
    private Vector3 ScreenSlideToWorldDir(Vector2 slide)
    {
        Vector3 camF = cam.transform.forward; camF.y = 0f; camF.Normalize();
        Vector3 camR = cam.transform.right; camR.y = 0f; camR.Normalize();
        Vector3 world = camR * slide.x + camF * slide.y;
        world.y = 0f;
        return world.normalized;
    }

    private void SmoothFace(Vector3 moveDir)
    {
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(moveDir.normalized, Vector3.up)
                            * Quaternion.Euler(0f, facingOffset, 0f);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    private bool RotateToward(Quaternion target)
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, rotationSpeed * Time.deltaTime);
        return Quaternion.Angle(transform.rotation, target) <= 2f;
    }

    private void SetWalking(bool walking)
    {
        if (animator != null && !string.IsNullOrEmpty(isWalkingParam))
            animator.SetBool(isWalkingParam, walking);
    }

    private void OnEnteredPortal()
    {
        // Brute-force safety: free EVERY lotus that still thinks this axolotl
        // is its occupant, so none get stuck "busy" after he disappears.
        foreach (var lotus in Lotus.All)
            if (lotus != null && lotus.occupant == this)
                lotus.Clear();

        OnPortalReached?.Invoke(this);
        gameObject.SetActive(false);
    }
    public static System.Action<AxolotlMover> OnPortalReached;

    private Lotus FindNearestLotus(Vector3 pos)
    {
        Lotus best = null; float bestSqr = float.MaxValue;
        foreach (var lotus in Lotus.All)
        {
            if (lotus == null) continue;
            if (!lotus.IsFree) continue;   // don't claim a lotus another axolotl already holds
            float d = (lotus.StandPosition - pos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = lotus; }
        }
        // Fallback: if somehow all are occupied, take the closest regardless.
        if (best == null)
        {
            foreach (var lotus in Lotus.All)
            {
                if (lotus == null) continue;
                float d = (lotus.StandPosition - pos).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = lotus; }
            }
        }
        return best;
    }

    private bool IsPointerOnAxolotl(Vector2 screenPos)
    {
        if (cam == null || tapCollider == null) return false;
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            return hit.collider == tapCollider;
        return false;
    }

#if ENABLE_INPUT_SYSTEM
    private bool prevPressed = false;
    private bool TryGetPointer(out Vector2 pos, out bool pressed)
    {
        pos = Vector2.zero; pressed = false;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        { pos = Touchscreen.current.primaryTouch.position.ReadValue(); pressed = true; return true; }
        if (Mouse.current != null)
        { pos = Mouse.current.position.ReadValue(); pressed = Mouse.current.leftButton.isPressed; return true; }
        return false;
    }
    private bool GetPointerDown(out Vector2 pos) { TryGetPointer(out pos, out bool p); return p && !prevPressed; }
    private bool GetPointerHeld(out Vector2 pos) { TryGetPointer(out pos, out bool p); return p; }
    private bool GetPointerUp() { TryGetPointer(out _, out bool p); bool up = !p && prevPressed; prevPressed = p; return up; }
#else
    private bool GetPointerDown(out Vector2 pos)
    {
        if (Input.touchCount > 0) { Touch t = Input.GetTouch(0); pos = t.position; return t.phase == TouchPhase.Began; }
        pos = Input.mousePosition; return Input.GetMouseButtonDown(0);
    }
    private bool GetPointerHeld(out Vector2 pos)
    {
        if (Input.touchCount > 0) { Touch t = Input.GetTouch(0); pos = t.position; return t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary; }
        pos = Input.mousePosition; return Input.GetMouseButton(0);
    }
    private bool GetPointerUp()
    {
        if (Input.touchCount > 0) { Touch t = Input.GetTouch(0); return t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled; }
        return Input.GetMouseButtonUp(0);
    }
#endif
}