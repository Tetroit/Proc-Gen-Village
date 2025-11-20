using System.Collections.Generic;
using TetraUtils;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Generation
{
    public class CityGenerator : MonoBehaviour
    {
        /// <summary>
        /// Generation area in local space
        /// </summary>
        public Rect area;
        /// <summary>
        /// Generation area but in global space, basically same as <seealso cref="area"/> 
        /// </summary>
        public Rect areaGS
        {
            get
            {
                Vector3 gcoord = transform.TransformPoint(new Vector3(area.x, 0, area.y));
                return new Rect(new Vector2(gcoord.x, gcoord.z), area.size);
            }
        }

        [SerializeField] float gizmoBoxThichness = 10;
        [SerializeField] Terrain terrain;
        [SerializeField] float waterLevel = 5.4f;
        [InspectorName("Maximum Height Difference")]
        [SerializeField] float maxHeightDiff = 5;
        [SerializeField] float maxHouseSize, minHouseSize;
        [Tooltip("The more samples the more packed houses will be")]
        [SerializeField] int houseSamples;
        [SerializeField] TerrainGenerator terrainGenerator;

        [SerializeField] List<GameObject> housePrefabs;
        public float blockSize = 5;

        /// <summary>
        /// house areas in local space
        /// </summary>
        public List<HouseArea> areas = new();
        [SerializeField] GizmoManager gizmoManager;

        [HideInInspector]
        public string status = "On Hold";

        public void Generate()
        {
            GizmoManager.Init(gizmoManager);
            status = "Initializing...";
            gizmoManager.Clear(transform);
            terrainGenerator.ResetMaps();
            for (int i=0; i<transform.childCount;)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            //GizmoManager.StageLine(new Vector3(0, 0, 0), new Vector3(0, 100, 0), Color.green, transform);
            areas.Clear();
            status = "Generating house areas";
            GenerateAreas(maxHouseSize, minHouseSize);
            status = "Generating terrain splatmaps";
            terrainGenerator.GeneratePaths(areas, terrain, area, waterLevel);
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 1, 1));
            Gizmos.DrawWireCube(new Vector3(area.center.x, gizmoBoxThichness, area.center.y), new Vector3(area.width, gizmoBoxThichness, area.height));

            foreach (var area in areas)
            {
                //area.obb.DrawAsCube(gizmoBoxThichness, area.elevation);
            }
            //Gizmos.color = Color.yellow;
            //for (int i=0; i<10; i++)
            //{
            //    for (int j=0; j<10; j++)
            //    {
            //        Vector3 p = new Vector3(j*20, 0, i * 20);
            //        p.y = terrain.SampleHeight(transform.TransformPoint(p));
            //        Gizmos.DrawSphere(p, 1);
            //    }
            //}
        }

        public bool IsWater(Vector3 pos, bool isLocalSpace = true)
        {
            float h = terrain.SampleHeight(transform.TransformPoint(pos));
            if (h < waterLevel)
                return true;
            return false;
        }
        public Vector3 OBBtoGlobal(Vector2 pos)
        {
            return new Vector3(pos.x, 0, pos.y);
        }
        public void GenerateAreas(float minSize, float maxSize)
        {
            for (int i = 0; i < houseSamples; i++)
            {
                //check if house overlaps with others
                OBB sample = OBB.RandomOBB(area, minSize, maxSize);
                bool overlaps = false;

                foreach (var area in areas)
                {
                    if (GeometryUtils.OBBOverlaping(sample, area.obb))
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (overlaps) continue;

                //check if house is on the water
                Vector3 centerCast = OBBtoGlobal(sample.center);
                float centerHeight = terrain.SampleHeight(transform.TransformPoint(centerCast));
                //gizmoManager.StagePoint(new Vector3 (centerCast.x, centerHeight, centerCast.z), Color.blue, 1, transform);
                if (IsWater(centerCast)) continue;

                HouseArea newArea = new HouseArea(sample, this);
                bool isValid = true;

                //check for maximum slope

                float max = float.MinValue;
                float min = float.MaxValue;
                foreach (var corner in sample.corners)
                {
                    Vector3 coord = OBBtoGlobal(corner);
                    float hsample = terrain.SampleHeight(transform.TransformPoint(coord));

                    //gizmoManager.StagePoint(new Vector3(coord.x, hsample, coord.z), Color.red, 1, transform);
                    if (hsample > max) max = hsample;
                    if (hsample < min) min = hsample;
                }
                if (max - min > maxHeightDiff) continue;
                if (!isValid) continue;

                newArea.elevation = max;
                newArea.rotation = Mathf.Atan2(sample.a1.y, sample.a1.x);
                areas.Add(newArea);

                var house = Instantiate(DataUtils.PickRandom(housePrefabs), transform);
                var houseGenerator = house.GetComponent<IHouseGenerator>();
                houseGenerator.Generate(newArea);

            }
        }
    }
}
