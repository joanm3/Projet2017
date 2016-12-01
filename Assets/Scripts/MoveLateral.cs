using UnityEngine;
using System.Collections;

public class MoveLateral : MonoBehaviour {

	public float speed = 10.0f;


	void Update () {
	
		transform.position += transform.forward * Input.GetAxis("Vertical") * speed * Time.deltaTime;
		transform.position += transform.right * Input.GetAxis("Horizontal") * speed * Time.deltaTime;

	}
}
