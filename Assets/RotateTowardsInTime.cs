using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTowardsInTime : MonoBehaviour
{

    public Vector3 targetRotation;
    public float speed = 1f;

    void Update()
    {

        float step = speed * Time.deltaTime;
        Vector3 newDir = Vector3.RotateTowards(Vector3.up, targetRotation, step, 0.0F);
        Debug.DrawRay(transform.position, newDir, Color.red);
        transform.rotation = Quaternion.LookRotation(newDir);
    }
}

