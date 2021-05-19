using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mixpanel;

public class MixpanelTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Test()
    {
        Mixpanel.Track("Press Button");
    }
    
    public void Test2()
    {
        Mixpanel.Track("Test Track 2");
    }
}