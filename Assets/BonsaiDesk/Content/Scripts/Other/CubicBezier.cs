using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubicBezier
{
    public static readonly CubicBezier Linear = new CubicBezier(0, 0, 1, 1);
    
    private readonly int _numSamples;

    private readonly Vector2[] _samples;

    private CubicBezier(float p1X, float p1Y, float p2X, float p2Y)
    {
        _numSamples = 5;
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
        
        for (var i = 1; i <= _numSamples; i++)
        {
            if (_samples[i].x > t)
            {
                return MathUtils.lineSegmentFunction(_samples[i - 1], _samples[i], t);
            }
        }

        Debug.LogError("Bezier no sample found.");
        return 1f;
    }
}