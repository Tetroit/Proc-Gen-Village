using System.Collections.Generic;
using System.Linq;
using UnityEngine;



[CreateAssetMenu(fileName = "PrefabList", menuName = "Scriptable Objects/PrefabList")]
public class PrefabList : ScriptableObject
{
    [SerializeField] List<PrefabInfo> items;
    [System.Serializable]
    struct PrefabInfo
    {
        public string name;
        public GameObject prefab;
    }
    public Dictionary<string, GameObject> prefabs => items.ToDictionary(x => x.name, x => x.prefab);
    public GameObject GetPrefab(string name)
    {
        if (prefabs.TryGetValue(name, out var prefab))
            return prefab;
        Debug.LogError($"Prefab {name} not found in PrefabList");
        return null;
    }
}
