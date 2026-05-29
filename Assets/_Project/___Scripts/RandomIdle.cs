using UnityEngine;

// Put this on each axolotl so their idle animations are NOT synced.
// At start it gives the Animator a random start time (offset) within the idle clip,
// and a slightly random playback speed, so a group of axolotls looks natural
// instead of all bobbing in unison.
//
// Note: if the axolotl is iced, AxolotlIce overrides animator.speed while frozen.
// This script only sets the *initial* desync; it does not fight the ice slowdown.
public class RandomIdle : MonoBehaviour
{
    [Header("Idle state")]
    [Tooltip("Name of the idle state in the Animator (usually the default state).")]
    public string idleStateName = "Idle";
    [Tooltip("Animator layer the idle state is on (0 = base layer).")]
    public int layer = 0;

    [Header("Random ranges")]
    [Tooltip("Random playback speed range (1 = normal). e.g. 0.85 - 1.15")]
    public float minSpeed = 0.85f;
    public float maxSpeed = 1.15f;

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

        // random speed
        animator.speed = Random.Range(minSpeed, maxSpeed);

        // random start offset within the idle clip
        if (randomizeStartOffset)
        {
            float randomOffset = Random.value; // 0..1 normalized time
            animator.Play(idleStateName, layer, randomOffset);
        }
    }
}
