using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Put this on the Axolotl (alongside AxolotlMover).
// It snaps the axolotl onto a lotus so you don't hand-place him every level.
//
// X and Z match the lotus's StandPoint exactly; Y = lotus Y + yOffset (e.g. 0.2).
//
// Two ways to use it:
//   1) EDITOR: click "Snap To Nearest Lotus" button in the Inspector while building.
//   2) RUNTIME: optionally snaps on Start (toggle below).
[ExecuteAlways]
public class AxolotlPlacer : MonoBehaviour
{
    [Tooltip("Height above the lotus the axolotl should rest at.")]
    public float yOffset = 0.2f;

    [Tooltip("If set, always snap to THIS lotus. If empty, snaps to the nearest lotus.")]
    public Lotus targetLotus;

    [Tooltip("Snap automatically when the game starts.")]
    public bool snapOnStart = true;

    [Tooltip("Snap automatically while editing in the Scene view (live).")]
    public bool snapInEditor = false;

    private void Start()
    {
        if (Application.isPlaying && snapOnStart)
            Snap();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Live-snap while building the level (only in edit mode, not playing).
        if (!Application.isPlaying && snapInEditor)
            Snap();
    }
#endif

    public void Snap()
    {
        Lotus lotus = targetLotus != null ? targetLotus : FindNearestLotus();
        if (lotus == null) return;

        Vector3 p = lotus.StandPosition;
        transform.position = new Vector3(p.x, p.y + yOffset, p.z);
    }

    private Lotus FindNearestLotus()
    {
        Lotus best = null;
        float bestSqr = float.MaxValue;

        // Lotus.All is only populated at runtime (OnEnable). In the editor we
        // search the scene directly so the button works while building.
        Lotus[] all;
        if (Application.isPlaying) all = Lotus.All.ToArray();
        else                       all = Object.FindObjectsByType<Lotus>(FindObjectsSortMode.None);

        foreach (var lotus in all)
        {
            float d = (lotus.StandPosition - transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = lotus; }
        }
        return best;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AxolotlPlacer))]
public class AxolotlPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AxolotlPlacer placer = (AxolotlPlacer)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Snap To Lotus Now"))
        {
            Undo.RecordObject(placer.transform, "Snap Axolotl To Lotus");
            placer.Snap();
            EditorUtility.SetDirty(placer.transform);
        }
    }
}
#endif
