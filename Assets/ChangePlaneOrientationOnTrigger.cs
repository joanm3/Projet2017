using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangePlaneOrientationOnTrigger : MonoBehaviour {

	public RotateTowardSmoothly sciptToChange;
	public Vector3 newVectorToSet = Vector3.zero;

	void OnTriggerEnter(Collider coll)
	{
		if(coll.tag == "Player"){

			sciptToChange.newRotation_asVector = newVectorToSet;
			Destroy(gameObject);
		}
	}

}
