using UnityEngine;

// Put this on each axolotl so their idle animations are NOT synced.
// Each axolotl picks ONE speed from the `speeds` array at random,
// plus an optional random start offset within the idle clip.
// This way you control exactly what speeds are allowed (not too slow, not too fast).
//
// Note: if the axolotl is iced, AxolotlIce overrides animator.speed while frozen.
public class RandomIdle : MonoBehaviour
{
    [Header("Idle state")]
    [Tooltip("Name of the idle state in the Animator (usually the default state).")]
    public string idleStateName = "Idle";
    [Tooltip("Animator layer the idle state is on (0 = base layer).")]
    public int layer = 0;

    [Header("Random speeds")]
    [Tooltip("List of allowed playback speeds. Each axolotl picks ONE at random.")]
    public float[] speeds = new float[] { 0.9f, 1.0f, 1.1f, 1.2f };

    [Tooltip("If true, also randomize the START offset within the idle clip.")]
    public bool randomizeStartOffset = true;

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) return;

        // If this axolotl is iced, let the ice control the speed; skip desync now.
        var ice = GetComponent<AxolotlIce>();
        if (ice != null && ice.IsFrozen) return;

        // Pick a random speed from the array.
        if (speeds != null && speeds.Length > 0)
        {
            int idx = Random.Range(0, speeds.Length);
            animator.speed = speeds[idx];
        }

        // Random start offset within the idle clip.
        if (randomizeStartOffset)
        {
            float randomOffset = Random.value; // 0..1 normalized time
            animator.Play(idleStateName, layer, randomOffset);
        }
    }
}
