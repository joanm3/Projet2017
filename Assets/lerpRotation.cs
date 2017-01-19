using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lerpRotation : MonoBehaviour
{

    public Transform target;
    public Vector3 startingAngles;
    public float y;
    public bool startChangeRotation = false;

    void Start()
    {
        y = -35f;
        startingAngles = target.localEulerAngles;
    }

    void Update()
    {
        if (startChangeRotation)
        {
            y = Mathf.Lerp(y, 0f, Time.deltaTime * 0.5f);
            target.localRotation = Quaternion.Euler(target.localEulerAngles.x, y, target.localEulerAngles.z);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            startChangeRotation = true;

        }

    }

}
