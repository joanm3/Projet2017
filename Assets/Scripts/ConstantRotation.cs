using UnityEngine;
using System.Collections;

public class ConstantRotation : MonoBehaviour {

    public enum AxisRotation { x, y, z };
    public AxisRotation axisRotation = AxisRotation.x;
    public float rotationAngleForce = 2f;


    // Update is called once per frame
    void Update()
    {



        switch (axisRotation)
        {
            case AxisRotation.x:
                transform.eulerAngles += new Vector3(rotationAngleForce * Time.deltaTime, 0f, 0f);
                break;
            case AxisRotation.y:
                transform.eulerAngles += new Vector3(0f, rotationAngleForce * Time.deltaTime, 0f);
                break;
            case AxisRotation.z:
                transform.eulerAngles += new Vector3(0f, 0f, rotationAngleForce * Time.deltaTime);
                break;


        }

    }
}
