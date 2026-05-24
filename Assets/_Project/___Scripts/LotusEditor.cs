#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Adds a button to the Lotus inspector to rebuild neighbor links for ALL lotuses,
// so you can see the yellow connection lines while building the level (no Play needed).
[CustomEditor(typeof(Lotus))]
public class LotusEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click below after placing/moving lotuses to refresh the yellow connection lines.",
            MessageType.Info);

        if (GUILayout.Button("Rebuild Neighbors (all lotuses)"))
        {
            // Make sure the registry is populated in edit mode.
            Lotus[] all = Object.FindObjectsByType<Lotus>(FindObjectsSortMode.None);
            Lotus.All.Clear();
            foreach (var l in all)
                if (!Lotus.All.Contains(l)) Lotus.All.Add(l);

            Lotus.RebuildAll();

            foreach (var l in all) EditorUtility.SetDirty(l);
            SceneView.RepaintAll();
        }
    }
}
#endif
