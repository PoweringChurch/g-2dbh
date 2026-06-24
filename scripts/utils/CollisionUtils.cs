using System;
using Godot;

public static class CollisionUtils
{
    public static bool PolygonVsCircle(Vector2[] localPoints, Vector2 bulletPos, Vector2 circleCenter, float radius)
    {
        Vector2 localCircleCenter = circleCenter - bulletPos;
        if (PointInPolygon(localPoints, localCircleCenter))
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
    public static bool PointInPolygon(Vector2[] points, Vector2 p)
    {
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
}