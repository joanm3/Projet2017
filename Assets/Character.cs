using UnityEngine;
using System.Collections;

public class Character : MonoBehaviour {

	[SerializeField]
	[Range(1.0f, 50.0f)]
	private float speedInputMax = 25.0f;

	[SerializeField]
	[Range(1.0f, 50.0f)]
	private float speedInputMax_whileTurning = 15.0f;

	[SerializeField]
	private AnimationCurve inputAcceleration;
	private float inputAcceleration_HighterValue = 0.0f;

	[SerializeField]
	private AnimationCurve inputDeceleration;
	private float inputDeceleration_HighterValue = 0.0f;

	[SerializeField]
	private AnimationCurve inputTuring;
	private float inputTurning_HighterValue = 0.0f;

	[SerializeField]
	private AnimationCurve input180Deceleration;
	private float input180Deceleration_HighterValue = 0.0f;

	[SerializeField]
	private AnimationCurve input180Acceleration;
	private float input180Acceleration_HighterValue = 0.0f;

	public float minSpeedRotation = 1.0f;
	public float maxSpeedRotation = 50.0f;

	public float Confort_angle = 25.0f;
	public float Glide_angle = 60.0f;

	[SerializeField]
	[Range(1.1f, 10.0f)]
	private float gravityForce = 5.0f;

	[SerializeField]
	[Range(1.0f, 100.0f)]
	private float maxVelocity = 10.0f;

	private float distToGround = 0.25f;

	private RaycastHit rayHit;
	private Vector3 shouldPosition = Vector3.zero;
	private Vector3 myCurrentVelocity = Vector3.zero;
	private bool isGrounded = false;
	private float maxDistDelta = 10.0f;


	private float currentRotationSpeed = 0.0f;


	private float current_HighterValue = 0.0f;

	private CharacterController myController;
	private Vector3 shouldDirection = Vector3.zero;

	private Vector3 moveDirection = Vector3.zero;

	private float timeOfChangeSpeed = 0.0f;

	void Start ()
	{
		//Get the last key time of each
		inputAcceleration_HighterValue = GetHighterCurveKeyTime(inputAcceleration);
		inputDeceleration_HighterValue = GetHighterCurveKeyTime(inputDeceleration);
		inputTurning_HighterValue = GetHighterCurveKeyTime(inputTuring);
		input180Deceleration_HighterValue = GetHighterCurveKeyTime(input180Deceleration);
		input180Acceleration_HighterValue = GetHighterCurveKeyTime(input180Acceleration);

		myController = GetComponent<CharacterController>();

	}

	float GetHighterCurveKeyTime (AnimationCurve curve)
	{
		float highterTime = 0.0f;
		for (int i = 0; i < curve.keys.Length; i++) {
			if(curve.keys[i].time > highterTime)
				highterTime = curve.keys[i].time;
		}
//		print(highterTime);
		return highterTime;
	}

	float _x = 0.0f;
	float _y = 0.0f;

	public float currentSpeed = 0.0f;

	private bool isTurning = false;

	private float lastSpeedIHaveBeforeToTurn = 0.0f;

	void Update ()
	{
	//INPUT
		_x = Input.GetAxis("Horizontal");
		_y = Input.GetAxis("Vertical");

		currentCurve = ChooseCurve();
		t_input = GetCurrentCurvePosition();
		v_input = SetSpeedInput();

		//Recupère la vitesse
//		currentSpeed = isTurning ? speedInputMax_whileTurning * v_input : speedInputMax * v_input ;
//		currentSpeed = isTurning ? Mathf.Lerp(currentSpeed, speedInputMax_whileTurning * v_input, t_input) : speedInputMax * v_input ;	//améliorer le lerp. Changer la A en "la vitesse que j'avais avant de tourner"
		currentSpeed = isTurning ? Mathf.Lerp(lastSpeedIHaveBeforeToTurn, speedInputMax_whileTurning * v_input, t_input) : speedInputMax * v_input ;


		shouldDirection = transform.forward * currentSpeed;

		//ROTATION
//		transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(transform.up, transform.position - (transform.position + inputVector)), speedRotation * Time.deltaTime);
		Vector3 targetDir = (transform.position + inputVector) - transform.position;
		float step = currentRotationSpeed * Time.deltaTime;
		Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0F);
		Debug.DrawRay(transform.position, newDir, Color.red);
		transform.rotation = Quaternion.LookRotation(newDir);

		currentRotationSpeed = Mathf.Lerp(minSpeedRotation, maxSpeedRotation, (speedInputMax - currentSpeed) / speedInputMax);	//La meme formule mais avec le speed velocity à la place
	
	//VELOCITY




		//Apply Movement
		myController.Move(shouldDirection * Time.deltaTime);

	}



	AnimationCurve currentCurve;	//Courbe actuelle
	[Range(0.0f, 50.0f)]
	public float t_input;	//valeur de 0 à 1 sur la curve de Time
	[Range(0.0f, 2.0f)]
	public float v_input;	//valeur de 0 à 1 sur la curve de Speed

	Vector3 inputVector = Vector3.zero;

	//Choisir le compteur (return AnimCurve)
	AnimationCurve ChooseCurve ()
	{
		float inputMagnitude = Vector2.Distance(Vector2.zero, (Vector2.up * _y) + (Vector2.right * _x));
		inputVector = (Vector3.forward * _y) + (Vector3.right * _x);	//Convertir ça en espace camera

		isTurning = false;

		//Si input < 0.2 alors return curve de decceleration
		if(inputMagnitude < 0.2f)
		{
			//Le joueur ne touche pas le stick

			current_HighterValue = inputDeceleration_HighterValue;
			return inputDeceleration;
		}
		else
		{
			//Le joueur utilise le stick
	
			//Le joueur avance ? tourne ? ou fait demi tour ?
			if(Vector3.Angle(inputVector, transform.forward) < 100.0f)
			{
				//Avance
				current_HighterValue = inputAcceleration_HighterValue;
				return inputAcceleration;
			}
//			else if(Vector3.Angle(inputVector, transform.forward) < 100.0f)
//			{
//				//Tourne
//				isTurning = true;
//				lastSpeedIHaveBeforeToTurn = currentSpeed;
//				current_HighterValue = inputTurning_HighterValue;
//				return inputTuring;
//			}
			else
			{
				//Retourne
				//TODO A PARTIR D'ICI, ON LOCK LE CHANGEMENT DE CURVE SUR TIMER
				current_HighterValue = input180Deceleration_HighterValue;
				return input180Deceleration;
			}

		}

	}
		

	private AnimationCurve lastFrameCurve;
	float _t_time = 0.0f;

	private const float precisionCurveAnalysis = 10.0f;

	//Get la position actuelle sur la courbe du temps en fonction de la vitesse (return t_time (float de 0 à 1))
	float GetCurrentCurvePosition ()
	{

		//Verif si on a changé de curve
		if(currentCurve != lastFrameCurve)
		{
			//On a changé de curve
//			_t_time = l'équivalent sur une autre curve

//			float diff = Mathf.Infinity;
//
//			for (float i = 0.0f; i < current_HighterValue; i += 1.0f/precisionCurveAnalysis) {
//
//				float curPointDiff = Mathf.Abs(v_input - currentCurve.Evaluate(i));
//
//				if(curPointDiff < diff)
//				{
//					diff = curPointDiff;
//				}
//				else
//				{
//					_t_time = i;
//				}
//			}

//			print(GetCurveTimeForValue(currentCurve, v_input, 10));
			_t_time = GetCurveTimeForValue(currentCurve, v_input, 10);

//			_t_time = 0.0f;

		}
		else
		{
			//On a pas changé de curve
			_t_time = Mathf.Clamp(_t_time + Time.deltaTime, 0.0f, current_HighterValue);	//TODO changer le +deltaTime par axis inclinaison * time.delatime
		}

		lastFrameCurve = currentCurve;

		return _t_time;
	}

	// NB. Will only work for curves with one definite time for each value
	public float GetCurveTimeForValue( AnimationCurve curveToCheck, float value, int accuracy ) {

		float startTime = curveToCheck.keys [0].time;
		float endTime = curveToCheck.keys [curveToCheck.length - 1].time;
		float nearestTime = startTime;
		float step = endTime - startTime;

		for (int i = 0; i < accuracy; i++) {

			float valueAtNearestTime = curveToCheck.Evaluate (nearestTime);
			float distanceToValueAtNearestTime = Mathf.Abs (value - valueAtNearestTime);

			float timeToCompare = nearestTime + step;
			float valueAtTimeToCompare = curveToCheck.Evaluate (timeToCompare);
			float distanceToValueAtTimeToCompare = Mathf.Abs (value - valueAtTimeToCompare);

			if (distanceToValueAtTimeToCompare < distanceToValueAtNearestTime) {
				nearestTime = timeToCompare;
				valueAtNearestTime = valueAtTimeToCompare;
			}
			step = Mathf.Abs(step * 0.5f) * Mathf.Sign(value-valueAtNearestTime);
		}
//		print(nearestTime);
		return nearestTime;
	}

	//Speed input = value on AnimCurve at t_time
	float SetSpeedInput ()
	{

		return currentCurve.Evaluate(t_input);
	}


}
