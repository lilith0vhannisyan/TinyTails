using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

// Put this on an empty "LevelTutorial" object in Level 1 (or any tutorial level).
//
// IMPORTANT: drag the SCENE hand icon (a UI Image already inside the Canvas),
// NOT a prefab from the Project folder. Prefab assets aren't in the scene
// and the script can't show them.
//
// For each step:
//   - Freezes every axolotl except the step's allowedAxolotl.
//   - Shows the step's tutorialText.
//   - Animates the HAND along the path of lotus neighbors from the axolotl
//     to the portal (so it follows the actual walking path, not a straight line).
//   - The step completes when the allowed axolotl enters a portal.
// At the end: shows "Now try yourself!" for a moment, then hides the UI.
public class LevelTutorial : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        [Tooltip("The ONLY axolotl the player can move during this step.")]
        public AxolotlMover allowedAxolotl;
        [Tooltip("The portal/target the hand should slide TOWARD.")]
        public Portal handTargetPortal;
        [Tooltip("Text shown during this step.")]
        [TextArea] public string tutorialText;
    }

    [Header("UI references (use SCENE objects, not prefabs)")]
    public TMP_Text tutorialText;
    [Tooltip("Optional background image behind the text (panel/frame). Hidden when tutorial ends.")]
    public GameObject tutorialTextBackground;
    public RectTransform handIcon;
    public Canvas canvas;
    [Tooltip("If empty, uses Camera.main.")]
    public Camera worldCamera;

    [Header("Hand animation")]
    public float handStepTime = 0.6f;
    public float handEndPauseTime = 0.4f;
    public Ease handEase = Ease.InOutSine;
    [Tooltip("How long the smooth return from portal back to start takes.")]
    public float handReturnTime = 0.5f;
    [Tooltip("If the axolotl moved more than this from its start position, hand stops looping and goes to the portal.")]
    public float playerStartedThreshold = 0.3f;

    [Header("Ending message")]
    public string endMessage = "Now try yourself!";
    public float endMessageDuration = 1.8f;

    [Header("Tutorial steps (in order)")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    private int currentStep = -1;
    private Coroutine handCoroutine;
    private bool waitingForCompletion = false;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        AxolotlMover.OnPortalReached += HandlePortalReached;
    }

    private void OnDisable()
    {
        AxolotlMover.OnPortalReached -= HandlePortalReached;
        StopHandLoop();
    }

    private void Start()
    {
        FreezeAllAxolotls();
        AdvanceStep();
    }

    private void HandlePortalReached(AxolotlMover m)
    {
        if (!waitingForCompletion) return;
        if (currentStep < 0 || currentStep >= steps.Count) return;
        if (m != steps[currentStep].allowedAxolotl) return;

        waitingForCompletion = false;
        AdvanceStep();
    }

    private void AdvanceStep()
    {
        currentStep++;
        if (currentStep >= steps.Count)
        {
            StartCoroutine(EndTutorialRoutine());
            return;
        }

        var step = steps[currentStep];
        if (tutorialText != null) tutorialText.text = step.tutorialText;

        FreezeAllAxolotls();
        if (step.allowedAxolotl != null) step.allowedAxolotl.SetFrozen(false);

        // Set BEFORE starting the coroutine, otherwise the while-loop sees it false and exits.
        waitingForCompletion = true;
        StartHand(step);
    }

    private IEnumerator EndTutorialRoutine()
    {
        UnfreezeAllAxolotls();
        StopHandLoop();
        if (handIcon != null) handIcon.gameObject.SetActive(false);

        if (tutorialText != null)
        {
            tutorialText.text = endMessage;
            tutorialText.gameObject.SetActive(true);
            tutorialText.transform.localScale = Vector3.one;
            tutorialText.transform.DOPunchScale(Vector3.one * 0.25f, 0.5f, 6, 0.8f);
        }
        // background stays visible behind the ending message
        if (tutorialTextBackground != null) tutorialTextBackground.SetActive(true);

        yield return new WaitForSecondsRealtime(endMessageDuration);

        // Hide and DESTROY everything tutorial-related.
        if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(false);
            Destroy(tutorialText.gameObject);
        }
        if (tutorialTextBackground != null)
        {
            tutorialTextBackground.SetActive(false);
            Destroy(tutorialTextBackground);
        }
        if (handIcon != null)
        {
            Destroy(handIcon.gameObject);
        }

        enabled = false;
    }

    private void FreezeAllAxolotls()
    {
        foreach (var m in FindObjectsByType<AxolotlMover>(FindObjectsSortMode.None))
            if (m != null) m.SetFrozen(true);
    }

    private void UnfreezeAllAxolotls()
    {
        foreach (var m in FindObjectsByType<AxolotlMover>(FindObjectsSortMode.None))
            if (m != null) m.SetFrozen(false);
    }

    private void StartHand(TutorialStep step)
    {
        StopHandLoop();
        if (handIcon == null)
        {
            Debug.LogError("[Tutorial] handIcon is NULL! Drag a SCENE hand UI Image (not a prefab asset) into the Hand Icon slot.");
            return;
        }
        if (step.allowedAxolotl == null || step.handTargetPortal == null)
        {
            Debug.LogError("[Tutorial] step.allowedAxolotl or step.handTargetPortal is NULL!");
            return;
        }

        // Force the hand's anchors+pivot to CENTER so DOAnchorPos moves it predictably,
        // independent of how the prefab was set up.
        handIcon.anchorMin = new Vector2(0.5f, 0.5f);
        handIcon.anchorMax = new Vector2(0.5f, 0.5f);
        handIcon.pivot = new Vector2(0.5f, 0.5f);

        handIcon.gameObject.SetActive(true);
        Debug.Log("[Tutorial] Starting hand for step " + currentStep +
                  " | axolotl=" + step.allowedAxolotl.name +
                  " | portal=" + step.handTargetPortal.name);

        handCoroutine = StartCoroutine(HandLoop(step));
    }

    private void StopHandLoop()
    {
        if (handCoroutine != null) StopCoroutine(handCoroutine);
        handCoroutine = null;
        if (handIcon != null) handIcon.DOKill();
    }

    // BFS through lotus neighbors to build a path of world positions
    // from the axolotl's current lotus to the target portal.
    private List<Vector3> BuildPath(AxolotlMover axo, Portal target)
    {
        var result = new List<Vector3>();
        if (axo == null || target == null) return result;

        // find the nearest lotus to the axolotl
        Lotus start = null;
        float bestSqr = float.MaxValue;
        foreach (var l in Lotus.All)
        {
            if (l == null) continue;
            float d = (l.StandPosition - axo.transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; start = l; }
        }
        if (start == null)
        {
            result.Add(axo.transform.position);
            result.Add(target.StandPosition);
            return result;
        }

        var prev = new Dictionary<Lotus, Lotus>();
        var queue = new Queue<Lotus>();
        queue.Enqueue(start);
        prev[start] = null;
        Lotus endLotus = null;

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur.portalNeighbors != null && cur.portalNeighbors.Contains(target))
            {
                endLotus = cur;
                break;
            }
            foreach (var n in cur.neighbors)
            {
                if (n == null || prev.ContainsKey(n)) continue;
                prev[n] = cur;
                queue.Enqueue(n);
            }
        }

        if (endLotus == null)
        {
            result.Add(axo.transform.position);
            result.Add(target.StandPosition);
            return result;
        }

        var pathLotuses = new List<Lotus>();
        var cursor = endLotus;
        while (cursor != null) { pathLotuses.Add(cursor); cursor = prev[cursor]; }
        pathLotuses.Reverse();

        result.Add(axo.transform.position);
        foreach (var l in pathLotuses) result.Add(l.StandPosition);
        result.Add(target.StandPosition);
        return result;
    }

    private IEnumerator HandLoop(TutorialStep step)
    {
        // Remember where the axolotl started, so we know when the player has moved it.
        Vector3 axoStartPos = step.allowedAxolotl.transform.position;

        // Initial: place hand at the start position.
        List<Vector3> initialPath = BuildPath(step.allowedAxolotl, step.handTargetPortal);
        if (initialPath.Count < 2)
        {
            Debug.LogWarning("[Tutorial] Path too short, skipping hand loop.");
            yield break;
        }
        handIcon.anchoredPosition = WorldToCanvas(initialPath[0]);

        while (waitingForCompletion && currentStep >= 0 && step == steps[currentStep])
        {
            // Did the player grab the axolotl and start moving it?
            bool playerStarted = (step.allowedAxolotl.transform.position - axoStartPos).sqrMagnitude
                                 > playerStartedThreshold * playerStartedThreshold;

            if (playerStarted)
            {
                // Player has it — animate hand to the portal and stay there until step completes.
                Vector2 portalCanvasPos = WorldToCanvas(step.handTargetPortal.StandPosition);
                Tween tw = handIcon.DOAnchorPos(portalCanvasPos, handStepTime).SetEase(handEase);
                yield return tw.WaitForCompletion();

                // Hold at portal until step is complete (waitingForCompletion becomes false).
                while (waitingForCompletion && step == steps[currentStep])
                    yield return null;

                yield break; // step done, exit
            }

            // Otherwise keep looping the guidance.
            List<Vector3> path = BuildPath(step.allowedAxolotl, step.handTargetPortal);
            if (path.Count < 2) yield break;

            // Walk the hand along the path step by step.
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 toPos = WorldToCanvas(path[i]);
                Tween tw = handIcon.DOAnchorPos(toPos, handStepTime).SetEase(handEase);
                yield return tw.WaitForCompletion();
                if (!waitingForCompletion || step != steps[currentStep]) yield break;

                // Re-check player started mid-walk
                if ((step.allowedAxolotl.transform.position - axoStartPos).sqrMagnitude
                    > playerStartedThreshold * playerStartedThreshold) break;
            }

            // Pause at the end.
            yield return new WaitForSecondsRealtime(handEndPauseTime);
            if (!waitingForCompletion || step != steps[currentStep]) yield break;

            // SMOOTHLY return to the start position (instead of snapping).
            Vector2 backToStart = WorldToCanvas(path[0]);
            Tween back = handIcon.DOAnchorPos(backToStart, handReturnTime).SetEase(handEase);
            yield return back.WaitForCompletion();
        }
    }

    // Convert a world position to anchored position inside the canvas.
    // Works for ScreenSpaceOverlay, ScreenSpaceCamera, and WorldSpace canvases.
    private Vector2 WorldToCanvas(Vector3 worldPos)
    {
        if (canvas == null || worldCamera == null) return Vector2.zero;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, canvasCam, out localPoint);

        return localPoint;
    }
}
