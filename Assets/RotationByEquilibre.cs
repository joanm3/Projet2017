using UnityEngine;
using System.Collections;

public class RotationByEquilibre : MonoBehaviour {

	[Tooltip("Vitesse de chute (rotation) maximum par seconde. La curve définit quel pourcentage de cette valeur est appliquée à la rotation du joueur")]
	[Range(10f, 360f)]
	public float maxSpeedFall = 10f;

	[Tooltip("A partir de cet angle, le personnage tombe")]
	[Range(25f, 80f)]
	public float maxAngleBeforeFall = 75f;

	[Range(10f, 90f)]
	public float Recovery_Angle = 15f;

	[Tooltip("Temps pour se relever (en secondes)")]
	[Range(0.1f, 5f)]
	public float timeToRecover = 1f;

	[Tooltip("time 0 = max confort, time 1 = max glide")]
	public AnimationCurve rotSpeedByAngle;

	RaycastHit rayHit;
	CharacterController myController;
	CharacterV3 myCharacterV3Script;
	Transform playerRender;
	Material feedbackMat;

	Quaternion currentInclinaison = Quaternion.identity;
	float tRecovery;
	Quaternion lastFalledRotation;

	StabilityState myStabilityState = StabilityState.Stable;
	public enum StabilityState
	{
		Stable,
		Unstable,
		Falling,
		GettingUp
	}

	void Start()
	{
		myController = GetComponent<CharacterController>();
		myCharacterV3Script = GetComponent<CharacterV3>();
		playerRender = GameObject.Find("PlayerRender").transform;
		feedbackMat = playerRender.GetComponentInChildren<MeshRenderer>().material;

		playerRender.parent = GameObject.Find("JoueurPrefab").transform;
	}

	void Update ()
	{

		//If grounded
		if(Physics.Raycast(transform.position, -Vector3.up, out rayHit, myController.bounds.extents.y + 0.1f))
		{

			if(myStabilityState == StabilityState.Stable || myStabilityState == StabilityState.Unstable)
			{

				myCharacterV3Script.canUseInput = true;

				//If surface Glide or Fall
				if(Vector3.Angle(Vector3.up, rayHit.normal) > myCharacterV3Script.Confort_angle)
				{
					Debug.DrawLine(rayHit.point, rayHit.point + (myCharacterV3Script.FirstTang * 10f), Color.green);

					myStabilityState = StabilityState.Unstable;
					float myCGTOG = myCharacterV3Script.fromCtoG;
					currentInclinaison *=  Quaternion.AngleAxis((maxSpeedFall * rotSpeedByAngle.Evaluate(myCGTOG)) * Time.deltaTime, -myCharacterV3Script.FirstTang);
				}
				//If surface confort
				else
				{
					myStabilityState = StabilityState.Stable;
					currentInclinaison = Quaternion.RotateTowards(currentInclinaison, Quaternion.LookRotation(Vector3.forward, Vector3.up), 90f * Time.deltaTime);
				}

				//IF we have to fall
				if(Vector3.Angle(Vector3.up, playerRender.up) > maxAngleBeforeFall)
				{
					myStabilityState = StabilityState.Falling;
				}

			}
			else
			{
				
				if(myStabilityState == StabilityState.Falling)
				{
					myCharacterV3Script.canUseInput = false;

//					print(Vector3.Angle(Vector3.up, rayHit.normal));
					if(Vector3.Angle(Vector3.up, rayHit.normal) < Recovery_Angle)
					{
						myStabilityState = StabilityState.GettingUp;
						tRecovery = 0f;
						lastFalledRotation = playerRender.rotation;
					}

				}
				else if(myStabilityState == StabilityState.GettingUp)
				{
					myCharacterV3Script.canUseInput = true;
//					currentInclinaison = Quaternion.RotateTowards(currentInclinaison, Quaternion.LookRotation(Vector3.forward, Vector3.up), 90f * Time.deltaTime);
					tRecovery += Time.deltaTime / timeToRecover;
					currentInclinaison = Quaternion.Slerp(lastFalledRotation, Quaternion.LookRotation(Vector3.forward, Vector3.up), tRecovery);

					if(tRecovery > 1f)
						myStabilityState = StabilityState.Stable;

				}


			}


			
		}
		//If is in air
		else
		{
			
		}

		playerRender.rotation = currentInclinaison;

		UpdateFeedBack();

	}

	void LateUpdate()
	{
		playerRender.position = transform.position - (Vector3.up * myController.bounds.extents.y);
	}

	void UpdateFeedBack()
	{
		switch (myStabilityState)
		{
		case StabilityState.Stable:
			feedbackMat.SetColor("_Color", Color.green);
			break;
		case StabilityState.Unstable:
//			feedbackMat.SetColor("_Color", Color.grey);
			float percent = Vector3.Angle(Vector3.up, playerRender.up) / maxAngleBeforeFall;
			feedbackMat.SetColor("_Color", Color.Lerp(Color.grey, Color.red, (percent > 0.25f) ? percent : 0f));
//			print((percent > 25f) ? percent : 0f);
			break;
		case StabilityState.Falling:
//			feedbackMat.SetColor("_Color", Color.red);
			feedbackMat.SetColor("_Color", Color.Lerp(Color.black, Color.red, Mathf.Sin(Time.time * 50f)));
			break;
		case StabilityState.GettingUp:
			feedbackMat.SetColor("_Color", Color.blue);
			break;
		default:
			feedbackMat.SetColor("_Color", Color.Lerp(Color.black, Color.magenta, Mathf.Abs(Mathf.Sin(Time.time * 100f))));
			break;
		}
	}

	void OnValidate()
	{
		if(Recovery_Angle < GetComponent<CharacterV3>().Confort_angle)
		{
			Debug.LogError("Attention ! un Recovery_Angle inférieur au Confort_Angle signifie que le joueur peut rester bloquer entre les deux après une perte d'équilibre");
		}
	}
		

}
