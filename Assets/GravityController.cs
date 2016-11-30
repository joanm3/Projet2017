using UnityEngine;
using System.Collections;
using System; 

[Serializable]
public class GravityController {

	public float qqqq;



	public Vector3 GetVerticalPosition ()
	{




		return Vector3.zero;
	}


}

////GRAV
//if(Physics.Raycast(transform.position, -Vector3.up, out rayHit, Mathf.Infinity)){
//
//
//	Vector3 _tempVector = myController.transform.position;
//	_tempVector.y = rayHit.point.y + myController.bounds.extents.y;
//	myController.transform.position = _tempVector;
//
//}

