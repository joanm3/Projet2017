using UnityEngine;
using System.Collections;

public class CameraDistanceController : MonoBehaviour
{
	public ThirdPersonCameraMovement thirdPersonCameraMovement;

	public float newDistance = 10f;
	public float newX = 0f;
    public float newY = 0f; 

	[Range (0f, 5f)]
	public float lerpTime = 1f;

	private bool changeDistance = false;

	private float startTime;
	private float journeyLength;

	void Start ()
	{
		startTime = Time.time; 
		if (thirdPersonCameraMovement == null)
			thirdPersonCameraMovement = Camera.main.GetComponentInParent<ThirdPersonCameraMovement> (); 
		
		journeyLength = Mathf.Abs (thirdPersonCameraMovement.maxDistance - newDistance); 

	}

	void FixedUpdate ()
	{
		float distCovered = (changeDistance == true) ? (Time.time - startTime) * (1f/lerpTime) : 0f; 
		float fracJourney = distCovered / journeyLength; 
		//Debug.Log (fracJourney); 

		if (changeDistance == true) {

			thirdPersonCameraMovement.maxDistance = Mathf.Lerp (thirdPersonCameraMovement.maxDistance, newDistance, fracJourney); 
			thirdPersonCameraMovement.xModificationAngle = Mathf.Lerp (thirdPersonCameraMovement.xModificationAngle, newX, fracJourney);
            thirdPersonCameraMovement.xModificationAngle = Mathf.Lerp(thirdPersonCameraMovement.yModificationAngle, newY, fracJourney);



            if (Mathf.Abs (thirdPersonCameraMovement.maxDistance - newDistance) <= 0.1f) {
				thirdPersonCameraMovement.maxDistance = newDistance;
				thirdPersonCameraMovement.xModificationAngle = newX;
                thirdPersonCameraMovement.yModificationAngle = newY;
                changeDistance = false; 
			}
		}


	}


	void OnTriggerEnter (Collider other)
	{
		//Debug.Log ("trigger entered"); 
		if (other.tag == "Player") {
			startTime = Time.time; 
			journeyLength = Mathf.Abs (thirdPersonCameraMovement.maxDistance - newDistance); 
			changeDistance = true; 

		}
	}



		



}
