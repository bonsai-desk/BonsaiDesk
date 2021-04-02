using UnityEngine;

public class Hole
{
    public Transform holeObject;
    private float _radius;

    public float radius
    {
        get { return _radius; }
        set
        {
            if (holeObject == null)
                Debug.LogError("Cannot set radius before setting holeObject.");
            _radius = value;
            holeObject.localScale = new Vector3(_radius * 2f, holeObject.localScale.y, _radius * 2f);
        }
    }
}