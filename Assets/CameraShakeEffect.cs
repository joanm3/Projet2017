using UnityEngine;
using System.Collections;

public class CameraShakeEffect : MonoBehaviour {

	public float shakeSumForce = 1f; 
	public float shake = 0f; 
	public float shakeAmount = 0.7f; 
	public float decreaseFactor = 1.0f; 

	
	// Update is called once per frame
	void Update () {

		//Debug.Log (shake); 

		if(Input.GetKeyDown(KeyCode.Q))
			{
				shake += shakeSumForce; 
			}

		if (shake > 0) {
			transform.localPosition = Random.insideUnitSphere * shakeAmount * shake; 
			shake -= Time.deltaTime * decreaseFactor; 	
		} else {
			shake = 0.0f; 
		}
	
	}
}


