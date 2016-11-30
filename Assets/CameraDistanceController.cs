using UnityEngine;
using System.Collections;

public class CameraDistanceController : MonoBehaviour
{
	public ThirdPersonCameraMovement thirdPersonCameraMovement;

	public float newDistance = 10f;

	[Range(0,1)]
	public float lerpTime = 0.1f;

	private bool changeDistance = false;

	private float startTime; 
	private float journeyLength; 

	void Start ()
	{
		startTime = Time.time; 
		if (thirdPersonCameraMovement == null)
			thirdPersonCameraMovement = Camera.main.GetComponentInParent<ThirdPersonCameraMovement> (); 
		
		journeyLength = Mathf.Abs (thirdPersonCameraMovement.distance - newDistance); 

	}

	void Update ()
	{
		if (changeDistance == true) {

			float distCovered = (Time.time - startTime) * lerpTime; 
			float fracJourney = distCovered / journeyLength; 

			thirdPersonCameraMovement.distance = Mathf.Lerp (thirdPersonCameraMovement.distance, newDistance, fracJourney); 

			if (Mathf.Abs (thirdPersonCameraMovement.distance - newDistance) >= 0.1f) {
				thirdPersonCameraMovement.distance = newDistance;
				changeDistance = false; 
			}
		}


	}


	void OnTriggerEnter (Collider other)
	{
		Debug.Log ("trigger entered"); 
		Debug.Log (other.name); 
		if (other.tag == "Player") {
			startTime = Time.time; 
			journeyLength = Mathf.Abs (thirdPersonCameraMovement.distance - newDistance); 
			changeDistance = true; 
			Debug.Log ("player entered"); 
		}
	}



		



}
