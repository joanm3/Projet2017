using UnityEngine;
using System.Collections;

public class ROtate : MonoBehaviour {


	void Update () {
	
		transform.Rotate(Vector3.up, 90.0f * Time.deltaTime);

	}
}
