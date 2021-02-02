using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableBrowserParent : MonoBehaviour {

    public bool flat = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TogglePosition() {
        if (flat) {
            transform.localPosition = new Vector3(0f, 0.078f, 0.221f);
            transform.localEulerAngles = new Vector3(-61.7f, 0f, 0f);
        }
        else {
            transform.localPosition = new Vector3(0f, 0f, 0f);
            transform.localEulerAngles = new Vector3(0f, 0f, 0f);
        }

        flat = !flat;
    }
    
    
}
