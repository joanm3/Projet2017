using UnityEngine;
using System.Collections;

public class PitchRot : MonoBehaviour {

	public float speedY = 100.0f;

	void Update () {

		transform.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * speedY * Time.deltaTime);

	}
}
