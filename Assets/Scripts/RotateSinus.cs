using UnityEngine;
using System.Collections;

public class RotateSinus : MonoBehaviour {


    public enum AxisRotation { x, y, z};

    public AxisRotation axisRotation = AxisRotation.z;

    public float forceAngle = 70f;

    public float velocityNotConstant = 0.5f; 

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
    {



        switch(axisRotation)
        {
            case AxisRotation.x:
                transform.rotation = Quaternion.Euler(Mathf.Sin(Time.realtimeSinceStartup * velocityNotConstant) * forceAngle, 0f, 0f);
                break;
            case AxisRotation.y:
                transform.rotation = Quaternion.Euler(0, Mathf.Sin(Time.realtimeSinceStartup * velocityNotConstant) * forceAngle, 0f);
                break;
            case AxisRotation.z:
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.realtimeSinceStartup * velocityNotConstant) * forceAngle);
                break;


        }

    }
}
