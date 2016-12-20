using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (SphereCollider))]
[RequireComponent (typeof (Rigidbody))]
public class GetGravitySurface : MonoBehaviour {

	//In ordrer to prevent the character to be blocked in in the air or sliding throught Fall surfaces,
	//This scripts checks all collision points in the surroundings of the feets of the player.
	//It returns the average normal surface direction.

	SphereCollider mySphereCol;
	Rigidbody myRigid;

	[HideInInspector]
	Vector3 averageSurfaceNormal = Vector3.zero;	//Average surface normal

	void Start()
	{
		mySphereCol = GetComponent<SphereCollider>();
		myRigid = GetComponent<Rigidbody>();
	}


	void OnCollisionStay(Collision col) {

		//Temp average surface normal
		Vector3 _average = Vector3.zero;

		foreach (ContactPoint contact in col.contacts) {
			Debug.DrawRay(transform.position, contact.normal, Color.white);

			//We get all the contact points in one vector
			_average += contact.normal;
		}

		//Get the average
		_average /= col.contacts.Length;

		Debug.DrawRay(transform.position, _average, Color.magenta);
	}
		
}
