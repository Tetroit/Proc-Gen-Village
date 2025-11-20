using TetraUtils;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    /// <summary>
    /// Previous alphamaps info is stored in this array with
    /// dimentions:
    /// 0 - x,
    /// 1 - y, 
    /// 2 - splatmap weight,
    /// as matches <seealso cref="TerrainData.GetAlphamaps"/> output
    /// </summary>
    float[,,] backup;
    [SerializeField] int roadID = 0;
    [SerializeField] float roadThreshold;
    [SerializeField] float roadNarrowness = 1f;
    [SerializeField] Terrain terrainContext;
    [SerializeField] List<GameObject> details = new List<GameObject>();
    public float detailRadius;
    public Vector2 sizeRange;
    public float treeDensity = 0;
    public int treeSamples = 1000;
    /// <summary>
    /// Generated tree objects
    /// </summary>
    List<GameObject> generated = new List<GameObject>();

    /// <summary>
    /// village area in LOCAL space
    /// </summary>
    RectInt boundsContext = new RectInt();
    /// <summary>
    /// village area in GLOBAL space
    /// </summary>
    RectInt boundsContextGS = new RectInt();

    /// <summary>
    /// Size coefficient from world to alphamap
    /// </summary>
    Vector2 worldToAlphamap;
    /// <summary>
    /// Size coefficient from alphamap to world
    /// </summary>
    Vector2 alphamapToWorld;

    float waterLevel;
    /// <summary>
    /// Transforms point world map space to terrain's alphamap space
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="ignoreBounds"></param>
    /// <returns></returns>
    public Vector2Int MapToAlphamap(Vector2 pos, bool ignoreBounds = false)
    {
        Vector2Int res = new Vector2Int((int)(pos.x * worldToAlphamap.x), (int)(pos.y * worldToAlphamap.y));
        if (ignoreBounds)
            return res;
        return res - boundsContext.position;
    }
    /// <summary>
    /// Transforms point terrain's alphamap space to map space
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="ignoreBounds"></param>
    /// <returns></returns>
    public Vector2 AlphamapToMap(Vector2Int pos, bool ignoreBounds = false)
    {
        if (ignoreBounds)
            return pos * alphamapToWorld;
        return (pos + boundsContext.position) * alphamapToWorld;
    }
    public Vector3 SamplePosOnAlphamap(Vector2Int p, bool inWorldsSpace = false) => 
        SamplePosOnTerrain(AlphamapToMap(p), inWorldsSpace = false);
    /// <summary>
    /// Transforms point from terrain map to actual 3D coordinates, 
    /// use <paramref name="inWorldSpace"/> to express in terrain object space or world space
    /// </summary>
    /// <param name="p"></param>
    /// <param name="inWorldSpace"></param>
    /// <returns></returns>
    public Vector3 SamplePosOnTerrain(Vector2 p, bool inWorldSpace = false)
    {
        Vector3 global = transform.TransformPoint(GeometryUtils.MapToWorld(p));
        Vector3 res = new Vector3(global.x, terrainContext.SampleHeight(global), global.z);
        if (inWorldSpace)
            return res;
        return transform.InverseTransformPoint(res);

    }
    /// <summary>
    /// Samples gradient from heightmap (dy/dx, dy/dz)
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Vector2 SampleGradient(Vector3 pos)
    {
        pos = transform.TransformPoint(pos);
        var posNX = pos; posNX.x -= 0.5f;
        var posNZ = pos; posNZ.z -= 0.5f;
        var posPX = pos; posPX.x += 0.5f;
        var posPZ = pos; posPZ.z += 0.5f;
        var deltaY_x = terrainContext.SampleHeight(posPX) - terrainContext.SampleHeight(posNX);
        var deltaY_z = terrainContext.SampleHeight(posPZ) - terrainContext.SampleHeight(posNZ);
        //deltaX and deltaZ are 1 so we dont need to divide by deltaX and deltaZ
        return new Vector2(deltaY_x, deltaY_z);
    }
    /// <summary>
    /// Generates roads
    /// </summary>
    /// <param name="houses">Houses in LOCAL space </param>
    /// <param name="terrain"></param>
    /// <param name="bounds">Village area in LOCAL space</param>
    /// <param name="waterLevel"></param>
    public void GeneratePaths(List<HouseArea> houses, Terrain terrain, Rect bounds, float waterLevel)
    {
        this.waterLevel = waterLevel;
        terrainContext = terrain;
        TerrainData data = terrain.terrainData;

        Debug.Log(data.alphamapWidth + " " + data.alphamapHeight);
        Debug.Log(data.size);

        worldToAlphamap = new Vector2(data.alphamapWidth, data.alphamapHeight) / new Vector2(data.size.x, data.size.z);
        alphamapToWorld = Vector2.one/worldToAlphamap;

        boundsContextGS = new RectInt(
            MapToAlphamap(bounds.position + GeometryUtils.WorldToMap(transform.position), true),
            MapToAlphamap(bounds.size, true));
        boundsContext = new RectInt(
            MapToAlphamap(bounds.position, true),
            MapToAlphamap(bounds.size, true));
        Debug.Log(boundsContext);

        backup = data.GetAlphamaps(boundsContextGS.x, boundsContextGS.y, boundsContext.width, boundsContext.height);

        if (GizmoManager.Instance != null)
            GizmoManager.Instance.StageLine(
                SamplePosOnAlphamap(Vector2Int.zero),
                SamplePosOnAlphamap(new Vector2Int(backup.GetLength(1), backup.GetLength(0))), Color.yellow, transform);
        DataUtils.CopyArray(backup, out float[,,] newMap);

        //all magic happens outside of Hogwarts
        PaintRoads(houses, ref newMap);
        data.SetAlphamaps(boundsContextGS.x, boundsContextGS.y, newMap);

        GenerateTrees(houses, bounds);
    }
    /// <summary>
    /// Checks if the terrain at <paramref name="pos"/> below water lever
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="isLocalSpace"></param>
    /// <returns></returns>
    public bool IsWater(Vector2 pos)
    {
        float h = terrainContext.SampleHeight(transform.TransformPoint(GeometryUtils.MapToWorld(pos)));
        if (h < waterLevel)
            return true;
        return false;
    }
    /// <summary>
    /// Repaints terrain alpha map in road
    /// </summary>
    /// <param name="houses">Houses OBBs</param>
    /// <param name="map">Alphamaps</param>
    public void PaintRoads(List<HouseArea> houses, ref float[,,] map)
    {
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                Vector2Int alphamapPos = new Vector2Int(j, i);

                Vector2 pos = AlphamapToMap(alphamapPos);
                if (IsWater(pos))
                    continue;
                float distance = GetDistanceInfo(houses, pos, out Vector2 c1, out Vector2 c2, out float second);

                if (distance > roadThreshold)
                {
                    //don't paint road on high slopes
                    Vector2 grad = SampleGradient(GeometryUtils.MapToWorld(pos));
                    float maxSlope = grad.magnitude;
                    float roadFac = Mathf.Abs(distance - second);
                    float slopeFac = 1 - Mathf.Clamp01(maxSlope);
                    //if (GizmoManager.Instance != null)
                    //    GizmoManager.Instance.StagePoint(WorldmapToTerrain(pos), new Color(Mathf.Abs(grad.x), Mathf.Abs(grad.y), 0), 1f, transform);
                    //should be road
                    if (roadFac < roadNarrowness)
                    {
                        roadFac = (1 - (roadFac / roadNarrowness)) * slopeFac;
                        if (GizmoManager.Instance != null) 
                        {
                            GizmoManager.Instance.StagePoint(SamplePosOnTerrain(pos), new Color(1 - slopeFac, roadFac, 0), 1f, transform);
                        }
                        SetRoad(new Vector2Int(i, j), ref map, roadFac);
                    }
                    //free area (for plants etc)
                    else
                    {
                        if (GizmoManager.Instance != null)
                        {
                            GizmoManager.Instance.StagePoint(SamplePosOnTerrain(pos), new Color(1, slopeFac, 0), 1f, transform);
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// Places trees taking into account roads and houses
    /// </summary>
    /// <param name="houses">Houses OBBs</param>
    /// <param name="bounds">Generation area</param>
    public void GenerateTrees(List<HouseArea> houses, Rect bounds)
    {
        for (int i=0; i<1000; i++)
        {
            Vector2 pos = GeometryUtils.Random2(bounds.min, bounds.max);
            float distance = GetDistanceInfo(houses, pos, out Vector2 c1, out Vector2 c2, out float second);

            if (IsWater(pos))
                continue;

            if (Mathf.Abs(distance - second) > roadNarrowness && distance > roadThreshold)
            {
                bool taken = false;
                foreach (var tree in generated)
                {
                    if ((GeometryUtils.WorldToMap(tree.transform.localPosition) - pos).magnitude < detailRadius)
                    {
                        taken = true;
                        break;
                    }
                }
                if (taken) continue;

                var fac = Random.value;
                if (fac < treeDensity)
                {
                    var instance = Instantiate(DataUtils.PickRandom(details), transform);
                    instance.transform.localPosition = SamplePosOnTerrain(pos);
                    instance.transform.localScale = Vector3.one * Random.Range(sizeRange.x, sizeRange.y);
                    instance.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    generated.Add(instance);
                }
            }

        }
    }
    /// <summary>
    /// Resets alphamap for terrain, but saves previous changes
    /// </summary>
    public void ResetMaps()
    {
        GizmoManager.Instance.Clear(transform);
        foreach (var child in generated)
        {
            DestroyImmediate(child);
        }
        generated.Clear();
        if (terrainContext == null ||
            backup == null)
        {
            Debug.Log("No previous modificactions found");
            return;
        }

        if (terrainContext.terrainData.alphamapLayers != backup.GetLength(2))
        {
            Debug.LogWarning("Terrain layers count mismatch");
            return;
        }
        terrainContext.terrainData.SetAlphamaps(boundsContextGS.x, boundsContextGS.y, backup);
        terrainContext = null;
    }
    /// <summary>
    /// Resets the terrain
    /// </summary>
    public void ResetWithoutBackup()
    {
        GizmoManager.Instance.Clear(transform);
        foreach (var child in generated)
        {
            DestroyImmediate(child);
        }
        generated.Clear();
        terrainContext = null;
    }
    /// <summary>
    /// Checks distance to 2 closest houses from <paramref name="point"/>
    /// </summary>
    /// <param name="houses">An array of OBBs that are considered as obstacles</param>
    /// <param name="point">Point on terrain map</param>
    /// <param name="closest1">First closest house point</param>
    /// <param name="closest2">Second closest house point</param>
    /// <param name="second">Distance to the second closest house</param>
    /// <returns>Distance to the first closest house</returns>
    public float GetDistanceInfo(List<HouseArea> houses, Vector2 point, out Vector2 closest1, out Vector2 closest2, out float second)
    {
        float min = float.MaxValue;
        second = float.MaxValue;
        closest1 = Vector2.zero;
        closest2 = Vector2.zero;


        foreach (var house in houses)
        {
            Vector2 p = house.obb.center;
            float distance = house.obb.ToClosestPoint(point);

            Vector2 locp = house.obb.InLocal(point);
            if (locp.x < 1 && locp.y < 1 && locp.x > 0 && locp.y > 0)
            {
                second = 0;
                return 0;
            }
            if (distance < min)
            {
                second = min;
                min = distance;
                closest2 = closest1;
                closest1 = point - p;
            }
            else if (distance < second)
            {
                second = distance;
                closest2 = point - p;
            }
        }
        return min;
    }
    /// <summary>
    /// Paints pixel to road material
    /// </summary>
    /// <param name="p">Pixel position</param>
    /// <param name="map">Alphamaps</param>
    /// <param name="fac">Paint strength</param>
    public void SetRoad(Vector2Int p, ref float[,,] map, float fac = 1)
    {
        for (int i = 0; i< map.GetLength(2); i++)
        {
            map[p.x, p.y, i] *= (1-fac);
        }
        map[p.x, p.y, roadID] += fac;
    }
}
