using UnityEngine;

public static class MathUtils
{
    public static float cubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        Vector2 position = Mathf.Pow(1 - t, 3) * p0 + 3 * Mathf.Pow(1 - t, 2) * t * p1 + 3 * (1 - t) * t * t * p2 + t * t * t * p3;
        return position.y;
    }

    public static float interpolate(float t)
    {
        float switchPoint = 0.25f;
        if (t < switchPoint)
        {
            Vector2 b1 = new Vector2(0.5f, 0f);
            Vector2 b2 = new Vector2(0.5f, 1f);
            return cubicBezier(Vector2.zero, b1, b2, Vector2.one, t / switchPoint) * switchPoint;
        }
        else
        {
            Vector2 b1 = new Vector2(1f, 0f);
            Vector2 b2 = new Vector2(1f, 0f);
            return cubicBezier(Vector2.zero, b1, b2, Vector2.one, (t - switchPoint) / (1 - switchPoint)) * (1 - switchPoint) + switchPoint;
        }
    }
}