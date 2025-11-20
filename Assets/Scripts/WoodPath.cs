using Generation.Curves;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using static GizmoManager;

public class WoodPath : MeshCreator
{
    [SerializeField]
    public Path path = new();
    [SerializeField]
    private Mesh prefab;

    public Vector3 MeshOrigin;
    public float MeshScale;
    public Vector2 TextureScale;

    private MeshBuilder meshBuilder = new();

    //editor context
    public int selectedPointIndex;
    public Vector3 selectedPoint => path.GetPoint(selectedPointIndex);
    public void Generate()
    {
        meshBuilder.Clear();
        RecalculateMesh();
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        path.Draw();
    }

    public override void RecalculateMesh()
    {
        if (path == null)
            return;
        List<Vector3> points = path.points;

        Debug.Log("Recalculating spline mesh");

        MeshBuilder builder = new MeshBuilder();
        if (points.Count < 2)
        {
            GetComponent<MeshFilter>().mesh = builder.CreateMesh(true);
            return;
        }
        Bounds bounds = prefab.bounds;
        Vector3 max = bounds.max;
        max.y = bounds.max.z;
        max.z = -bounds.min.y;

        Vector3 min = bounds.min;
        min.y = bounds.min.z;
        min.z = -bounds.max.y;

        // First, compute directions & orientations for each line segment of the curve:
        // TODO: for a better looking curve: for each curve point, first compute the *average direction* (normalized) of *both* incident line segments,
        //   and then use that direction to compute the orientation of the point:
        var localOrientation = new List<Quaternion>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            // Compute a unit length vector from the current point to the next:
            Vector3 lineSegmentDirection = (points[i + 1] - points[i]).normalized;
            // Store a matching orientation (computing an orientation requires a forward direction vector and an up direction vector):
            localOrientation.Add(Quaternion.LookRotation(lineSegmentDirection, Vector3.up));
        }
        var pointsOrientation = new List<Quaternion>();
        pointsOrientation.Add(localOrientation[0]);
        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 lineSegmentDirection1 = (points[i] - points[i - 1]).normalized;
            Vector3 lineSegmentDirection2 = (points[i + 1] - points[i]).normalized;
            pointsOrientation.Add(Quaternion.LookRotation((lineSegmentDirection1 + lineSegmentDirection2) * 0.5f, Vector3.up));
        }
        pointsOrientation.Add(localOrientation[^1]);

        // Loop over all line segments in the curve:
        int instanceCnt = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            // For each line segment, add a rotated version of the input mesh to the output mesh, using the localOrientation as rotation:
            float segmentLen = (points[i + 1] - points[i]).magnitude;
            int segmentCnt = (int) (segmentLen / ((max.z - min.z) * MeshScale));
            for (int seg = 0; seg < segmentCnt; seg++)
            {
                int numVerts = prefab.vertexCount;
                for (int j = 0; j < prefab.vertexCount; j++)
                {
                    var transformedVert = prefab.vertices[j];
                    transformedVert.x = prefab.vertices[j].x;
                    transformedVert.y = prefab.vertices[j].z;
                    transformedVert.z = -prefab.vertices[j].y;

                    // Map z coordinate to a number t from 0 to 1 (assuming the mesh bounds are correct):
                    float t = (transformedVert.z - min.z) / (max.z - min.z);

                    // Center and scale the input mesh vertices, using the values given in the inspector:
                    Vector3 inputV = (transformedVert - MeshOrigin) * MeshScale;
                    // Set the z-coordinate to zero:
                    inputV.Scale(new Vector3(1, 1, 0));

                    // Use the value t to linearly interpolate between the start and end points of the line segment:
                    // Choose one of the two lines below - they are completely equivalent!
                    // Vector3 interpolatedLineSegmentPoint = Vector3.Lerp(points[i], points[i+1], t);
                    Vector3 interpolatedLineSegmentPoint = Vector3.Lerp(points[i], points[i + 1], (t + seg) / segmentCnt); // Lerp = the weighted average between two vectors

                    // TODO: interpolate the orientations as well, not just the points!
                    Vector3 rotatedXYModelCoordinate = Quaternion.Slerp(pointsOrientation[i], pointsOrientation[i + 1], (t + seg) / segmentCnt) * inputV;

                    builder.AddVertex(
                        interpolatedLineSegmentPoint + rotatedXYModelCoordinate,
                        prefab.uv[j] / TextureScale
                    );
                }
                // TODO: Take submeshes into account:
                int numSubmeshes = prefab.subMeshCount;
                for (int sm = 0; sm < numSubmeshes; sm++)
                {
                    var tris = prefab.GetTriangles(sm);
                    int numTris = tris.Length;
                    for (int j = 0; j < numTris; j += 3)
                    {
                        builder.AddTriangle(
                            tris[j] + numVerts * instanceCnt,
                            tris[j + 1] + numVerts * instanceCnt,
                            tris[j + 2] + numVerts * instanceCnt,
                            sm
                        );
                    }
                }
                instanceCnt++;
            }
        }

        Mesh newMesh = builder.CreateMesh(true);
        ReplaceMesh(newMesh, false);
    }
}
