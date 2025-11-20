using UnityEngine;
using UnityEditor;
using System.Security.Policy;
using Generation;


[CustomEditor(typeof(RectangularHouse))]
public class RectHouseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RectangularHouse generator = target as RectangularHouse;

        if (GUILayout.Button("Generate"))
            generator.Generate();
        base.OnInspectorGUI();
    }
}
