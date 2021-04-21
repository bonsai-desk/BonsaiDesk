using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoCubeArm : MonoBehaviour
{
    public static VideoCubeArm Instance;

    private Transform _target;
    public float activateHeight = 1;
    public float lerpDistance = 0.5f;
    public float endMoveDistance = 0.2f;
    
    void Start()
    {
        Instance = this;
    }
    
    void Update()
    {
        if (!_target)
        {
            return;
        }
        
        var lerp = 1 - Mathf.Clamp01((activateHeight - _target.position.y - endMoveDistance) / lerpDistance);
        lerp = CubicBezier.EaseOut.Sample(lerp);
        transform.position = new Vector3(_target.position.x, activateHeight + (1 - lerp) * 0.3f, _target.position.z);
    }

    public void SetCubePosition(Transform cube)
    {
        if (!_target || cube.position.y > _target.position.y)
        {
            _target = cube;
        }
    }
}