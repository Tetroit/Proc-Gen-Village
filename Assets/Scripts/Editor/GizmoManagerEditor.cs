using Generation;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GizmoManager))]
public class GizmoManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GizmoManager manager = target as GizmoManager;
        GUILayout.Label("Meshes: " + manager.calls.Count);

        foreach (var call in manager.calls.Values)
        {
            GUILayout.Label("\tLines:" + call.lines.Count);
            GUILayout.Label("\tPoints:" + call.points.Count);
        }
        base.OnInspectorGUI();
    }
}
