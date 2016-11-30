using UnityEngine;
using System.Collections;

public class RotByInput : MonoBehaviour {

	RaycastHit rayHit;


	void Update () {
		
//		Vector3 _vectorTolook = transform.position + ((Vector3.forward * Input.GetAxis("Vertical")) + (Vector3.right * Input.GetAxis("Horizontal")));
//		Debug.DrawLine(_vectorTolook, _vectorTolook + Vector3.up, Color.green);
//		Debug.DrawLine(_vectorTolook, _vectorTolook + Vector3.right, Color.red);
//		Debug.DrawLine(_vectorTolook, _vectorTolook + Vector3.forward, Color.blue);
//
//
//		if(_vectorTolook.magnitude > 0.3f)
//		{
//			_vectorTolook = _vectorTolook - transform.position;
//		}
//		else
//		{
//			_vectorTolook = transform.forward;
//		}
//
//
//
//		Quaternion _toRot = Quaternion.LookRotation(_vectorTolook);
//		Quaternion _rRot = Quaternion.RotateTowards(transform.rotation, _toRot, 140f * Time.deltaTime);
//		transform.rotation = _rRot;



	}
}
