using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeConstantForceBoolOnTriggerEnter : MonoBehaviour
{

    public ConstantRotation cr;

    public ConstantRotation.AxisRotation axisRotation = ConstantRotation.AxisRotation.x;
    public float rotationAngleForce = 2f;
    public bool rotateToPositif;
    public float targetAngle = 0f;


    void OnTriggerEnter()
    {
        cr.enabled = true;

        cr.rotateToPositif = rotateToPositif;
        cr.axisRotation = axisRotation;
        cr.rotationAngleForce = rotationAngleForce;
        cr.targetAngle = targetAngle;


    }
}
