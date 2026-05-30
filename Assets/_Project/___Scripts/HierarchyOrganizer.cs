#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// Editor tool: groups scene objects into tidy parent folders.
// Menu:  Tools > TinyTails > Organize Hierarchy
public static class HierarchyOrganizer
{
    [MenuItem("Tools/TinyTails/Organize Hierarchy")]
    public static void Organize()
    {
        GroupByComponent<AxolotlMover>("--- Axolotls ---");
        GroupByComponent<Lotus>("--- Lotuses ---");
        GroupByComponent<Portal>("--- Portals ---");
        GroupByComponent<Bubble>("--- Bubbles ---");

        // Trays: match by NAME so it catches Tray, Tray (1), Tray (2)... even
        // if they don't all have the CollectionTray script.
        GroupByName("Tray", "--- Trays ---");

        Debug.Log("Hierarchy organized.");
    }

    private static void GroupByComponent<T>(string parentName) where T : Component
    {
        T[] items = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        if (items.Length == 0) return;

        Transform parent = GetOrCreateParent(parentName);
        foreach (var item in items)
            Reparent(item.transform, parent, parentName);
    }

    // Groups any root object whose name starts with the given prefix.
    private static void GroupByName(string namePrefix, string parentName)
    {
        Transform parent = GetOrCreateParent(parentName);

        // Only look at root objects (no parent) to avoid grabbing children.
        foreach (var root in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (root == null) continue;
            if (root.parent != null) continue;                 // only top-level
            if (root == parent) continue;
            if (!root.name.StartsWith(namePrefix)) continue;
            // don't grab the folder containers themselves
            if (root.name.StartsWith("---")) continue;

            Reparent(root, parent, parentName);
        }
    }

    private static Transform GetOrCreateParent(string parentName)
    {
        GameObject parent = GameObject.Find(parentName);
        if (parent == null)
        {
            parent = new GameObject(parentName);
            Undo.RegisterCreatedObjectUndo(parent, "Create " + parentName);
        }
        return parent.transform;
    }

    private static void Reparent(Transform item, Transform parent, string label)
    {
        if (item == null || parent == null) return;
        if (item.parent == parent) return;          // already grouped
        Undo.SetTransformParent(item, parent, "Organize " + label);
    }
}
#endif
