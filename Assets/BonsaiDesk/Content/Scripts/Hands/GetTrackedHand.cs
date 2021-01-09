using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetTrackedHand : MonoBehaviour
{
    public OVRSkeleton.SkeletonType skeletonType;
    
    private bool _initialized = false;

    // Update is called once per frame
    void Update()
    {
        if (!_initialized && PlayerHands.hands.GetHand(skeletonType).Tracking())
        {
            _initialized = true;
            // StartCoroutine(Init());
            Init();
        }
    }

    private void Init()
    {
        // yield return new WaitForSeconds(3f);
        var mapper = GetComponent<OVRHandTransformMapper>();
        mapper.targetObject = PlayerHands.hands.GetHand(skeletonType).oVRHand.transform;
        mapper.moveObjectToTarget = false;
        mapper.TryAutoMapBoneTargetsAPIHand();
    }
}
