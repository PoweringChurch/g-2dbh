using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Godot;

public static class CollisionUtils
{
    public static bool PolygonVsCircle(Vector2[] localPoints, Vector2 polyPos, Vector2 circleCenter, float radius, float polygonForward, bool flipPolygon = false)
    {
        Vector2 localCircleCenter = circleCenter - polyPos;
        if (polygonForward != 0)
            localCircleCenter = localCircleCenter.Rotated(-polygonForward);
        if (flipPolygon)
            localCircleCenter.X = -localCircleCenter.X;
        if (PointInPolygon(localPoints, localCircleCenter, polygonForward))
            return true;
        float radiusSq = radius * radius;
        int n = localPoints.Length;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = localPoints[i];
            Vector2 b = localPoints[(i + 1) % n];
            if (DistSqPointToSegment(localCircleCenter, a, b) <= radiusSq)
                return true;
        }
        return false;
    }
    public static bool PointInPolygon(Vector2[] points, Vector2 p, float polygonForward = 0)
    {
        if (polygonForward != 0)
            p = p.Rotated(-polygonForward);
        bool inside = false;
        int n = points.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 pi = points[i];
            Vector2 pj = points[j];
            bool intersects = (pi.Y > p.Y) != (pj.Y > p.Y) &&
                p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y) + pi.X;
            if (intersects)
                inside = !inside;
        }
        return inside;
    }
    public static float DistSqPointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.LengthSquared();
        if (lenSq < 0.0001f)
            return (p - a).LengthSquared(); // degenerate segment, treat as a point
        float t = Math.Clamp((p - a).Dot(ab) / lenSq, 0f, 1f);
        Vector2 closest = a + ab * t;
        return (p - closest).LengthSquared();
    }
    public static float[][] Vect2sToFloatArr(IEnumerable<Vector2> vects)
    {
        return [.. vects.Select(v => new[] { v.X, v.Y })];
    }
    public static List<Vector2> FloatArrToVect2s(float[][] floats)
    {
        if (floats == null) return [];
        return [.. floats.Select(f => new Vector2(f[0], f[1]))];
    }
}