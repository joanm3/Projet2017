using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapToSurface : MonoBehaviour
{

    public Vector3 surfaceNormal = Vector3.up;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;

        Debug.DrawRay(transform.position, -surfaceNormal * 10f, Color.red);

        if (Physics.Raycast(transform.position, -surfaceNormal, out hit))
        {
            Debug.Log("snapping");
            //surfaceNormal = hit.normal;
            transform.position = GetSnapPositionByHitPoint(hit.point, GetComponent<BoxCollider>());
        }
    }


    private Vector3 GetSnapPositionByHitPoint(Vector3 point, BoxCollider collider)
    {
        return point - (-transform.up * (collider.bounds.extents.y));
    }

}
