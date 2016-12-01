using UnityEngine;
using System.Collections;

public class YawRot : MonoBehaviour {

	public float speedX = 100.0f;

	void Update () {
	
		transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * speedX * Time.deltaTime);

	}
}
