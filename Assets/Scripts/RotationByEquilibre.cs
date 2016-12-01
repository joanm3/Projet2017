using UnityEngine;
using System.Collections;

public class RotationByEquilibre : MonoBehaviour {

	public float maxSpeedFall = 10f;

	public float maxAngleBeforeFall = 75f;

	[Tooltip("time 0 = max confort, time 1 = max glide")]
	public AnimationCurve rotSpeedByAngle;

	RaycastHit rayHit;
	CharacterController myController;
	CharacterV3 myCharacterV3Script;

	void Start()
	{
		myController = GetComponent<CharacterController>();
		myCharacterV3Script = GetComponent<CharacterV3>();
	}

	void Update ()
	{

		if(Physics.Raycast(transform.position + (-Vector3.up * (myController.bounds.extents.y - 0.1f)), -Vector3.up, out rayHit, Mathf.Infinity)){
		

			if(Vector3.Angle(Vector3.up, rayHit.normal) > myCharacterV3Script.Confort_angle)
			{
				Debug.DrawLine(rayHit.point, rayHit.point + (myCharacterV3Script.FirstTang * 10f), Color.green);

				float myCGTOG = myCharacterV3Script.fromCtoG;

//				transform.rotation *= Quaternion.AngleAxis((maxSpeedFall * rotSpeedByAngle.Evaluate(myCGTOG)) * Time.deltaTime, -myCharacterV3Script.FirstTang);

			}
			else
			{
//				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(transform.forward, Vector3.up), 90f * Time.deltaTime);
			}

		}

	}
		

}
