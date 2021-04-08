using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubicBezier
{
    //https://cubic-bezier.com/
    //https://easings.net/

    public static readonly CubicBezier EaseInOut = new CubicBezier(0.42f, 0, 0.58f, 1);
    public static readonly CubicBezier EaseIn = new CubicBezier(0.42f, 0, 1, 1);
    public static readonly CubicBezier EaseOut = new CubicBezier(0, 0, 0.58f, 1);
    public static readonly CubicBezier LateStart = new CubicBezier(0.73f, 0.21f, 0.87f, 0.31f);

    private readonly int _numSamples;

    private readonly Vector2[] _samples;
    private readonly Vector2[] _inverseSamples;

    private CubicBezier(float p1X, float p1Y, float p2X, float p2Y)
    {
        _numSamples = 25;
        var p1 = new Vector2(p1X, p1Y);
        var p2 = new Vector2(p2X, p2Y);

        _samples = new Vector2[_numSamples + 1];
        _inverseSamples = new Vector2[_numSamples + 1];
        for (var i = 0; i <= _numSamples; i++)
        {
            var value = MathUtils.CubicBezier(Vector3.zero, p1, p2, Vector2.one, (float) i / (float) _numSamples);
            if (i > 0 && (value.x < _samples[i - 1].x || value.y < _samples[i - 1].y))
            {
                Debug.LogError("Bezier is not a one-to-one function.");
                _samples = null;
                _inverseSamples = null;
                return;
            }

            _samples[i] = value;
            _inverseSamples[i] = new Vector2(value.y, value.x);
        }
    }

    public float Sample(float t)
    {
        return Sample(t, _samples, _numSamples);
    }

    public float SampleInverse(float t)
    {
        return Sample(t, _inverseSamples, _numSamples);
    }

    /// <summary>
    /// Move a time value to 0 or 1. Uses Sample and SampleInverse internally so the move direction (0 to 1, or 1 to 0) can change at any time without jumping
    /// </summary>
    /// <param name="tCurrent">Current time from 0 to 1. This is the actual value put into the lerp function, not the time passed into the cubic bezier</param>
    /// <param name="animationTime">Time it should take to go from 0 to 1 or 1 to 0. Time.deltaTime is used internally</param>
    /// <param name="increase">True to move time towards 1, False to move time towards 0</param>
    /// <returns>The updated t value</returns>
    public float MoveTowards01(float tCurrent, float animationTime, bool increase)
    {
        if (Mathf.Approximately(tCurrent, 0) && !increase || Mathf.Approximately(tCurrent, 1) && increase)
        {
            return tCurrent;
        }

        float t = SampleInverse(tCurrent);
        float step = (1f / animationTime) * Time.deltaTime;
        t = Mathf.MoveTowards(t, increase ? 1f : 0f, step);
        return Sample(t);
    }

    private static float Sample(float t, Vector2[] samples, int numSamples)
    {
        if (samples == null)
        {
            Debug.Log("No samples.");
            return 0f;
        }

        if (t <= 0f)
            return 0f;
        if (t >= 1f)
            return 1f;

        var l = 0;
        var r = numSamples - 1;
        var i = 0;
        while (l <= r)
        {
            i++;
            if (i > 100)
            {
                Debug.LogError("Binary search failed to find search within 100 iterations.");
                return 0f;
            }

            var m = l + (r - l) / 2;
            if (t >= samples[m].x && t <= samples[m + 1].x)
            {
                return MathUtils.lineSegmentFunction(samples[m], samples[m + 1], t);
            }
            else if (samples[m].x < t)
            {
                l = m + 1;
            }
            else
            {
                r = m - 1;
            }
        }

        Debug.LogError("Bezier no sample found.");
        return 1f;
    }
}