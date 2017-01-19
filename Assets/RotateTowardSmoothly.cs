using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTowardSmoothly : MonoBehaviour {


	public Vector3 newRotation_asVector;
	public float rotateSpeed = 10f;

	void Start()
	{
		newRotation_asVector = transform.rotation.eulerAngles;
	}

	void Update ()
	{

		transform.rotation = Quaternion.RotateTowards(Quaternion.Euler(transform.rotation.eulerAngles), Quaternion.Euler(newRotation_asVector), rotateSpeed * Time.deltaTime);

	}
}
