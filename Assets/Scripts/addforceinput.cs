using UnityEngine;
using System.Collections;

public class addforceinput : MonoBehaviour {

	Rigidbody myRigid;

	public float speed = 10.0f;

	void Start () {
	
		myRigid = GetComponent<Rigidbody>();

	}
	
	void FixedUpdate () {
	
		myRigid.AddForce(new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical")) * speed, ForceMode.Acceleration);

	}
}
