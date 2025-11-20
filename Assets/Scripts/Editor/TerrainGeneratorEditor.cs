using UnityEngine;
using UnityEditor;
using System.Security.Policy;
using Generation;


[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator generator = target as TerrainGenerator;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Reset"))
            generator.ResetMaps();
        if (GUILayout.Button("Reset Without Backup"))
            generator.ResetWithoutBackup();

        GUILayout.EndHorizontal();
        base.OnInspectorGUI();
    }

    public void OnSceneGUI()
    {

        TerrainGenerator generator = target as TerrainGenerator;

        
    }
}
