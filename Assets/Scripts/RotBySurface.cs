using UnityEngine;
using System.Collections;

public class RotBySurface : MonoBehaviour {

	RaycastHit rayHit;

	void Update () {
	
		if(Physics.Raycast(transform.position, -Vector3.up, out rayHit, Mathf.Infinity))
		{
			
			Debug.DrawRay(rayHit.point, rayHit.normal, Color.red);

			transform.rotation = Quaternion.LookRotation(Vector3.forward, rayHit.normal);
		}

	}

}
