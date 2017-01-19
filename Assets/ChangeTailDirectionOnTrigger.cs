using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeTailDirectionOnTrigger : MonoBehaviour {


	public plaitAnimation plaitScript;

	public Vector3 newValue = new Vector3(1, -1, 0);

	void OnTriggerEnter(Collider coll)
	{

		if(coll.tag == "Player")
		{
			print("Trigger change vector value");
			plaitScript.directionOfFalling = newValue;
			Destroy(gameObject);
		}

	}

}
