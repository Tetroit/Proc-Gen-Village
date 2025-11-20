using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GizmoManager : MonoBehaviour
{
    public struct LineInfo
    {
        public Vector3 p1;
        public Vector3 p2;
        public Color color;
        public LineInfo(Vector3 p1, Vector3 p2, Color color)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.color = color;
        }
    }
    public struct PointInfo
    {
        public Vector3 p;
        public float size;
        public Color color;
        public PointInfo(Vector3 p, Color color, float size = 0.1f)
        {
            this.p = p;
            this.color = color;
            this.size = size;
        }
    }
    public class MeshCall
    {
        public Matrix4x4 transform;
        public List<LineInfo> lines = new();
        public List<PointInfo> points = new();
        public void StageLine(Vector3 p1, Vector3 p2, Color color)
        {
            lines.Add(new LineInfo(p1, p2, color));
        }
        public void StagePoint(Vector3 p, Color color)
        {
            points.Add(new PointInfo(p, color));
        }
        public void StagePoint(Vector3 p, Color color, float size)
        {
            points.Add(new PointInfo(p, color, size));
        }
    }
    public Dictionary<Transform, MeshCall> calls = new();
    public static GizmoManager Instance {get; private set;}
    public static void Init(GizmoManager manager)
    {
        if (Instance == null)
            Instance = manager;
        else if (Instance != manager)
            DestroyImmediate(manager.gameObject);
    }
    void OnDrawGizmos()
    {
        for (int i = 0; i < calls.Count; i++)
        {
            var call = calls.ElementAt(i);
            if (call.Key == null)
            {
                calls.Remove(call.Key);
                continue;
            }
            Gizmos.matrix = call.Key.localToWorldMatrix;
            foreach (var line in call.Value.lines)
            {
                Gizmos.color = line.color;
                Gizmos.DrawLine(line.p1, line.p2);
            }
            foreach (var point in call.Value.points)
            {
                Gizmos.color = point.color;
                Gizmos.DrawSphere(point.p, point.size);
            }
        }
    }
    public void StageLine(Vector3 p1, Vector3 p2, Color color, Transform transform)
    {
        if (!calls.ContainsKey(transform))
            calls.Add(transform, new MeshCall());

        calls[transform].StageLine(p1, p2, color);
    }
    public void StagePoint(Vector3 p, Color color, Transform transform)
    {
        if (!calls.ContainsKey(transform))
            calls.Add(transform, new MeshCall());

        calls[transform].StagePoint(p, color);
    }
    public void StagePoint(Vector3 p, Color color, float size, Transform transform)
    {
        if (!calls.ContainsKey(transform))
            calls.Add(transform, new MeshCall());

        calls[transform].StagePoint(p, color, size);
    }
    public void Clear(Transform transform)
    {
        if (calls.ContainsKey(transform) && transform != null)
            calls.Remove(transform);
    }
    public void ClearWithChildren(Transform transform)
    {
        Clear(transform);
        foreach(Transform child in transform)
            ClearWithChildren(child);
    }
}
