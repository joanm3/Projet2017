using UnityEngine;
using System.Collections;

public class ConstantRotation : MonoBehaviour
{

    public enum AxisRotation { x, y, z };
    public AxisRotation axisRotation = AxisRotation.x;
    public float rotationAngleForce = 2f;
    public bool rotateToPositif = true;
    public float targetAngle = 0f;
    public Transform target;
    public bool ApplyOnTriggerEnter = false;

    [SerializeField]
    private float actualRotation;

    private bool m_startRotation = true;


    void Start()
    {
        if (target == null)
            target = this.transform;
        if (ApplyOnTriggerEnter)
            m_startRotation = false;
    }


    // Update is called once per frame
    void Update()
    {

        if (!m_startRotation)
            return;

        float value = rotationAngleForce * Time.deltaTime;

        //Debug.Log(Mathf.Abs(actualRotation));


        bool smallDistance = Mathf.Abs(actualRotation - targetAngle) < rotationAngleForce;

//        Debug.Log(Mathf.Abs(actualRotation - targetAngle));
 //       Debug.Log(smallDistance);
        if (!smallDistance)
        {
            switch (axisRotation)
            {
                case AxisRotation.x:
                    if (rotateToPositif)
                        target.eulerAngles += new Vector3(value, transform.eulerAngles.y, transform.eulerAngles.z);
                    else
                        target.eulerAngles -= new Vector3(value, transform.eulerAngles.y, transform.eulerAngles.z);

                    actualRotation = target.eulerAngles.x;
                    break;
                case AxisRotation.y:
                    if (rotateToPositif)
                        target.eulerAngles += new Vector3(transform.eulerAngles.x, value, transform.eulerAngles.z);
                    else
                        target.eulerAngles -= new Vector3(transform.eulerAngles.x, value, transform.eulerAngles.z);

                    actualRotation = target.eulerAngles.y;
                    break;
                case AxisRotation.z:
                    if (rotateToPositif)

                        target.eulerAngles += new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, value);
                    else
                        target.eulerAngles -= new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, value);

                    actualRotation = target.eulerAngles.z;
                    break;
            }
        }
        else
        {
            //switch (axisRotation)
            //{
            //    case AxisRotation.x:
            //        target.eulerAngles = new Vector3(targetAngle, transform.eulerAngles.y, transform.eulerAngles.z);
            //        break;
            //    case AxisRotation.y:
            //        target.eulerAngles = new Vector3(transform.eulerAngles.x, targetAngle, transform.eulerAngles.z);
            //        break;
            //    case AxisRotation.z:
            //        target.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, targetAngle);
            //        break;
            //}
        }

    }



    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
            m_startRotation = true;
    }


}
