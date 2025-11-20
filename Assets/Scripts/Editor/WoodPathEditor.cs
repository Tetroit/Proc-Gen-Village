using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WoodPath))]
public class WoodPathEditor : Editor
{
    WoodPath woodPath;
    public override void OnInspectorGUI()
    {
        woodPath = (WoodPath)target;
        if (GUILayout.Button("Generate"))
        {
            woodPath.Generate();
        }

        base.OnInspectorGUI();
    }
    private void OnSceneGUI()
    {
        woodPath = (WoodPath)target;
        ProcessSceneEvents();

        if (woodPath.selectedPointIndex >= woodPath.path.size)
            woodPath.selectedPointIndex = woodPath.path.size - 1;

        Vector3 p = woodPath.path.GetPoint(woodPath.selectedPointIndex);
        p = woodPath.transform.TransformPoint(p);
        p = Handles.PositionHandle(p, Quaternion.identity);
        woodPath.path.points[woodPath.selectedPointIndex] = woodPath.transform.InverseTransformPoint(p);
    }

    public void ProcessSceneEvents()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Period)
        {
            woodPath.selectedPointIndex++;
            if (woodPath.selectedPointIndex >= woodPath.path.size)
                woodPath.selectedPointIndex = woodPath.path.size - 1;
        }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Comma)
        {
            woodPath.selectedPointIndex--;
            if (woodPath.selectedPointIndex < 0)
                woodPath.selectedPointIndex = 0;
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Semicolon)
        {
            if (woodPath.selectedPointIndex >= woodPath.path.size - 1)
                woodPath.selectedPointIndex = woodPath.path.size - 2;
            woodPath.path.Subdivide(woodPath.selectedPointIndex);
            woodPath.selectedPointIndex++;
        }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Quote)
        {
            woodPath.path.RemovePoint(woodPath.selectedPointIndex);
            if (woodPath.selectedPointIndex >= woodPath.path.size)
                woodPath.selectedPointIndex = woodPath.path.size - 1;
        }
    }
}
