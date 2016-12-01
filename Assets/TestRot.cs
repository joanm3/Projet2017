using UnityEngine;
using System.Collections;

public class TestRot : MonoBehaviour {

	RaycastHit rayHit;
	Quaternion rotR = Quaternion.identity;


	void Update () {
	
		//Direction input
		Vector3 _vectorTolook = transform.position + ((Vector3.forward * Input.GetAxis("Vertical")) + (Vector3.right * Input.GetAxis("Horizontal")));
		Debug.DrawLine(_vectorTolook, _vectorTolook + Vector3.up, Color.green);
		Debug.DrawLine(_vectorTolook, _vectorTolook + Vector3.right, Color.red);
		Debug.DrawLine(_vectorTolook, _vectorTolook + Vector3.forward, Color.blue);

		_vectorTolook = _vectorTolook - transform.position;

		//Rotation d'input
		//Quaternion _ToLookRot = Quaternion.LookRotation(_vectorTolook);
		Quaternion _ToLookRot = Quaternion.identity;

		Quaternion _rRot = Quaternion.RotateTowards(transform.rotation, _ToLookRot, 90f * Time.deltaTime);

		//Rotation de surface


		if(Physics.Raycast(transform.position, -Vector3.up, out rayHit, Mathf.Infinity)){
			Vector3 FirstTang = Vector3.Cross(rayHit.normal, Vector3.up);
			rotR = Quaternion.AngleAxis(45f * Time.deltaTime, FirstTang);
		}

//			float valeur = 0.5f; 
//			float sinus = Mathf.Sin(Time.time * 10f - valeur) ; 
//			Quaternion rotR = Quaternion.AngleAxis(sinus * 90f * Time.deltaTime, Vector3.right);
//	//		Quaternion rotU = Quaternion.AngleAxis(Mathf.Sin(Time.time * 10) * 45f * Time.deltaTime, Vector3.up);

		transform.rotation = rotR;
		transform.rotation = transform.rotation * _rRot;

	}

}
