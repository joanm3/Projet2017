using UnityEngine;
using System.Collections;

public class CameraTriggerBehaviour : MonoBehaviour
{
	public ThirdPersonCameraMovement thirdPersonCameraMovement;
    public ThirdPersonCameraMovement.CameraMode newMode = ThirdPersonCameraMovement.CameraMode.Free; 

    [Header("OnTriggerEnter")]
    [Range (0f, 5f)]
	public float lerpTime = 1f;

    [Header("General Values")]
    public bool changeGeneralValues = true; 
	public float maxDistance = 50f;
    public float minDistance = 5f;

    [Header("Modifications")]
    public bool changeModifications = true;
	public float xModificationAngle = 0f;
    public float yModificationAngle = 0f;

    [Header("Angle clamp")]
    public bool changeAngleClamp = true; 
    public bool limitXAngle = false;
    public float xAngleMin = -50.0f;
    public float xAngleMax = 50.0f;
    public float yAngleMin = -20.0f;
    public float yAngleMax = 50.0f;

    [Header("Rotation By Normal")]
    public bool changeRotationByNormal = true;
    public bool rotationByNormal = true;
    public float rotationIntensity = 1f;

    [Header("OnTriggerExit")]
    public bool returnToLastCamera = false;
    public ThirdPersonCameraMovement lastCameraMovementValues; 


    private bool changeDistance = false;

	private float startTime;
	private float journeyLength;

	void Start ()
	{
		startTime = Time.time; 
		if (thirdPersonCameraMovement == null)
			thirdPersonCameraMovement = Camera.main.GetComponentInParent<ThirdPersonCameraMovement> (); 
		
		journeyLength = Mathf.Abs (thirdPersonCameraMovement.maxDistance - maxDistance); 

	}

	void FixedUpdate ()
	{
		float distCovered = (changeDistance == true) ? (Time.time - startTime) * (1f/lerpTime) : 0f; 
		float fracJourney = distCovered / journeyLength; 
		//Debug.Log (fracJourney); 

		if (changeDistance == true) {

			thirdPersonCameraMovement.maxDistance = Mathf.Lerp (thirdPersonCameraMovement.maxDistance, maxDistance, fracJourney); 
			thirdPersonCameraMovement.xModificationAngle = Mathf.Lerp (thirdPersonCameraMovement.xModificationAngle, xModificationAngle, fracJourney);
            thirdPersonCameraMovement.xModificationAngle = Mathf.Lerp(thirdPersonCameraMovement.yModificationAngle, yModificationAngle, fracJourney);


            if (Mathf.Abs (thirdPersonCameraMovement.maxDistance - maxDistance) <= 0.1f) {
				thirdPersonCameraMovement.maxDistance = maxDistance;
				thirdPersonCameraMovement.xModificationAngle = xModificationAngle;
                thirdPersonCameraMovement.yModificationAngle = yModificationAngle;
                changeDistance = false; 
			}
		}


	}


	void OnTriggerEnter (Collider other)
	{
		//Debug.Log ("trigger entered"); 
		if (other.tag == "Player") {
			startTime = Time.time; 
			journeyLength = Mathf.Abs (thirdPersonCameraMovement.maxDistance - maxDistance); 
			changeDistance = true; 

		}
	}



		



}
