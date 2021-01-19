using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Vuplex.WebView;

public class AutoBrowser : Browser
{
    private Vector3 _defaultLocalPosition;
    private Vector3 _belowTableLocalPosition;
    public Rigidbody screenRigidBody;

    protected override void Start()
    {
        base.Start();
        Debug.Log("auto browser start");
        _defaultLocalPosition = transform.localPosition;
        _belowTableLocalPosition = _defaultLocalPosition;
        _belowTableLocalPosition.y = -_bounds.y / 2f;
    }
    
    public override Vector2Int ChangeAspect(Vector2 newAspect)
    {
        var aspectRatio = newAspect.x / newAspect.y;
        var localScale = new Vector3(_bounds.y * aspectRatio, _bounds.y, 1);
        if (localScale.x > _bounds.x)
            localScale = new Vector3(_bounds.x, _bounds.x * (1f / aspectRatio), 1);

        var resolution = AutoResolution(localScale, distanceEstimate, pixelPerDegree, newAspect);

        if (!Mathf.Approximately(1, _webViewPrefab.WebView.Resolution))
        {
            _webViewPrefab.WebView.SetResolution(1);
        }
        
        _webViewPrefab.WebView.Resize(resolution.x, resolution.y);
        
        Debug.Log($"[BONSAI] ChangeAspect resolution {resolution}");

        boundsTransform.localScale = localScale;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        RebuildOverlay(resolution);
#endif

        return resolution;
    }
    
    public void SetHeight(float t)
    {
        var heightT = t;
        transform.localPosition = Vector3.Lerp(_belowTableLocalPosition, _defaultLocalPosition, Mathf.Clamp01(heightT));

        var height = boundsTransform.localScale.y;
        var halfHeight = height / 2f;

        var scaleT = (transform.localPosition.y + halfHeight) / height;
        scaleT = Mathf.Clamp01(scaleT);
        
        holePuncherTransform.localScale = new Vector3(1, scaleT,1);
        holePuncherTransform.localPosition = new Vector3(0, (1-scaleT) / 2, 0);

        if (Mathf.Approximately(t, 0))
        {
            //TODO is this laggy? also this runs even if you don't have authority over the screen
            screenRigidBody.velocity = Vector3.zero;
            screenRigidBody.angularVelocity = Vector3.zero;
            transform.GetChild(0).localPosition = Vector3.zero;
            transform.GetChild(0).localRotation = Quaternion.identity;
        }
    }

}