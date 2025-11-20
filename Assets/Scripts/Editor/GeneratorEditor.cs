using UnityEngine;
using UnityEditor;
using System.Security.Policy;
using Generation;


[CustomEditor(typeof(CityGenerator))]
public class GeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CityGenerator generator = target as CityGenerator;

        GUILayout.Label("Generated areas: " + generator.areas.Count);
        GUILayout.Label("Status: " + generator.status);

        if (GUILayout.Button("Generate"))
            generator.Generate();
        base.OnInspectorGUI();
    }
}
