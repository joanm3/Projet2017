using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class constantRot : MonoBehaviour {


    public float speed = 45f;

    public axisdir AXE = axisdir.x;
    public enum axisdir
    { x,y,z }

	void Update () {

        switch (AXE) {

            case axisdir.x:
                transform.Rotate(Vector3.right, speed * Time.deltaTime);
                break;
            case axisdir.y:
                transform.Rotate(Vector3.up, speed * Time.deltaTime);
                break;
            case axisdir.z:
                transform.Rotate(Vector3.forward, speed * Time.deltaTime);
                break;

        }

	}
}
