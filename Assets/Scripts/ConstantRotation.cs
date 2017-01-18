using UnityEngine;
using System.Collections;

public class ConstantRotation : MonoBehaviour
{

    public enum AxisRotation { x, y, z };
    public AxisRotation axisRotation = AxisRotation.x;
    public float rotationAngleForce = 2f;
    public bool rotateToPositif = true;
    public float targetAngle = 0f;

    [SerializeField]
    private float actualRotation;

    // Update is called once per frame
    void Update()
    {

        float value = rotationAngleForce * Time.deltaTime;

        //Debug.Log(Mathf.Abs(actualRotation));


        bool smallDistance = Mathf.Abs(actualRotation - targetAngle) < rotationAngleForce;

        Debug.Log(Mathf.Abs(actualRotation - targetAngle));
        Debug.Log(smallDistance);
        if (!smallDistance)
        {
            switch (axisRotation)
            {
                case AxisRotation.x:
                    if (rotateToPositif)
                        transform.eulerAngles += new Vector3(value, transform.eulerAngles.y, transform.eulerAngles.z);
                    else
                        transform.eulerAngles -= new Vector3(value, transform.eulerAngles.y, transform.eulerAngles.z);

                    actualRotation = transform.eulerAngles.x;
                    break;
                case AxisRotation.y:
                    if (rotateToPositif)
                        transform.eulerAngles += new Vector3(transform.eulerAngles.x, value, transform.eulerAngles.z);
                    else
                        transform.eulerAngles -= new Vector3(transform.eulerAngles.x, value, transform.eulerAngles.z);

                    actualRotation = transform.eulerAngles.y;
                    break;
                case AxisRotation.z:
                    if (rotateToPositif)

                        transform.eulerAngles += new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, value);
                    else
                        transform.eulerAngles -= new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, value);

                    actualRotation = transform.eulerAngles.z;
                    break;
            }
        }
        else
        {
            switch (axisRotation)
            {
                case AxisRotation.x:
                    transform.eulerAngles = new Vector3(targetAngle, transform.eulerAngles.y, transform.eulerAngles.z);
                    break;
                case AxisRotation.y:
                    transform.eulerAngles = new Vector3(transform.eulerAngles.x, targetAngle, transform.eulerAngles.z);
                    break;
                case AxisRotation.z:
                    transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, targetAngle);
                    break;
            }
        }

    }
}
