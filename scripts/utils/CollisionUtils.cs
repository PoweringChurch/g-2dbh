using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Godot;

public static class CollisionUtils
{
    public static Vector2[] TranslatePolygon(Vector2[] localPoints, Vector2 position, float polygonForward, bool flipPolygonAlongX = false, bool flipPolygonAlongY = false)
    {
        if (localPoints == null) return null;

        int count = localPoints.Length;
        Vector2[] translatedPoints = new Vector2[count];

        float cos = Mathf.Cos(polygonForward);
        float sin = Mathf.Sin(polygonForward);

        float flipModifierX = flipPolygonAlongX ? -1f : 1f;
        float flipModifierY = flipPolygonAlongY ? -1f : 1f;

        for (int i = 0; i < count; i++)
        {
            Vector2 point = localPoints[i];

            float x = point.X * flipModifierX;
            float y = point.Y * flipModifierY;

            translatedPoints[i] = new Vector2(
                (x * cos - y * sin) + position.X,
                (x * sin + y * cos) + position.Y
            );
        }
        return translatedPoints;
    }
    public static bool PolygonVsCircle(Vector2[] localPoints, Vector2 polyPos, Vector2 circleCenter, float radius, float polygonForward, bool flip = false)
    {
        Vector2 localCircleCenter = circleCenter - polyPos;
        if (polygonForward != 0)
            localCircleCenter = localCircleCenter.Rotated(-polygonForward);
        if (flip == true)
            localCircleCenter = -localCircleCenter;
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
}