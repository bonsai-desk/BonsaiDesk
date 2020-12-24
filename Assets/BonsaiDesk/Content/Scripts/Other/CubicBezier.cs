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
    
    private readonly int _numSamples;

    private readonly Vector2[] _samples;

    private CubicBezier(float p1X, float p1Y, float p2X, float p2Y)
    {
        _numSamples = 25;
        var p1 = new Vector2(p1X, p1Y);
        var p2 = new Vector2(p2X, p2Y);
        
        _samples = new Vector2[_numSamples + 1];
        for (var i = 0; i <= _numSamples; i++)
        {
            var value = MathUtils.CubicBezier(Vector3.zero, p1, p2, Vector2.one, (float) i / (float) _numSamples);
            if (i > 0 && value.x < _samples[i - 1].x)
            {
                Debug.LogError("Bezier is not a function.");
                _samples = null;
                return;
            }
            _samples[i] = value;
        }
    }

    public float Sample(float t)
    {
        if (_samples == null)
        {
            Debug.Log("No samples.");
            return 0f;
        }
        if (t <= 0f)
            return 0f;
        if (t >= 1f)
            return 1f;
        
        var l = 0;
        var r = _numSamples - 1;
        var i = 0;
        while (l <= r)
        {
            i++;
            if (i > 1000)
            {
                Debug.LogError("Binary search failed.");
                return 0f;
            }
            var m = l + (r - l) / 2;
            if (t >= _samples[m].x && t <= _samples[m + 1].x)
            {
                return MathUtils.lineSegmentFunction(_samples[m], _samples[m + 1], t);
            }
            else if (_samples[m].x < t)
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