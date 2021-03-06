﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof (CharacterController))]
public class CharacterV3 : MonoBehaviour {


	[Header("Angles")]
	[Space(20.0f)]

	[Tooltip("Jusqu'à quel angle sommes nous en Confort (Gizmos cyan)")]
	[Range(1.0f, 45.0f)]
	public float Confort_angle = 25.0f;
	[Tooltip("Jusqu'à quel angle sommes nous en Glide (au delà = Fall) (Gizmos violet puis rouge)")]
	[Range(45.0f, 90.0f)]
	public float Glide_angle = 25.0f;

	[Space(20.0f)]
	[Header("Surface")]

	[Tooltip("La vitesse que donne la surface de Glide_angle maximum")]
	[Range(5.0f, 50.0f)]
	public float maxGlideSpeed = 15.0f;
	[Tooltip("Time est utilisée en tant qu'indicateur de surface. 0 = Confort_angle, 1 = Glide_angle. Value est utilisé pour définir quel proportion de maxGlideSpeed est utilisé (0 = 0%, 1 = 100%)")]
	public AnimationCurve velocityGlideAcceleration = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
	[Tooltip("Vitesse de transition par seconde entre la velocité actuelle et celle fournie par la surface (1 = 1 unité de vitesse par seconde). lORSQUE LA VELOCITE MONTE")]
	[Range(1.0f, 40.0f)]
	public float velocityTransitionSpeed_acceleration = 2.0f;
	[Tooltip("Vitesse de transition par seconde entre la velocité actuelle et celle fournie par la surface (1 = 1 unité de vitesse par seconde). LORSQUE LA VELOCITE BAISSE")]
	[Range(1.0f, 40.0f)]
	public float velocityTransitionSpeed_decceleration = 0.5f;

	[Space(20.0f)]
	[Header("Input")]

	[Tooltip("La vitesse que donne le stick sur une surface plane")]
	[Range(5.0f, 40.0f)]
	public float maxInputSpeed = 15.0f;
	[Tooltip("Vitesse de rotation maximum (en angles par seconde)")]
	[Range(5.0f, 1440.0f)]
	public float max_RotationSpeed = 200.0f;
	[Tooltip("Vitesse de rotation minimum (en angles par seconde)")]
	[Range(5.0f, 360.0f)]
	public float min_RotationSpeed = 50.0f;
	[Tooltip("Vitesse de rotation en fonction de la vitesse de deplacement input. De gauche à droite la vitesse de deplacement input, de haut en bas la vitesse de rotation. Interpolation entre min_RotationSpeed et max_RotationSpeed en fonction de la curve")]
	public AnimationCurve rotationBySpeed = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
	[Tooltip("Interpolation entre 0 et maxInputSpeed")]
	public AnimationCurve InputAcceleration = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
	[Tooltip("Interpolation entre maxInputSpeed et 0")]
	public AnimationCurve InputDecceleration = AnimationCurve.Linear(0.0f, 1.0f, 0.5f, 0.0f);

	private CharacterController myController;
	private RaycastHit rayHit;
	private Vector3 surfaceNormal;
	private Vector3 current_VelocitySpeedDir = Vector3.zero;	//Velocité actuelle
	private Vector3 surface_VelocitySpeedDir = Vector3.zero;	//Velocité renseignée par la surface
	private Vector3 InputSpeedDir = Vector3.zero;			//Direction et vitesse
	private Vector3 shouldSpeedDir = Vector3.zero;	//Direction et vitesse finale

	private float currentRotationSpeed = 0.0f;

	CharacterParenting myCharaparenting;
	Camera cam;

	void Start ()
	{
		myController = GetComponent<CharacterController>();
		myController.slopeLimit = Glide_angle;
		myCharaparenting = GetComponent<CharacterParenting>();
		cam = GameObject.Find("Main Camera").GetComponent<Camera>();
	}


	void Update ()
	{
		if(Physics.Raycast(transform.position + (-Vector3.up * (myController.bounds.extents.y - 0.1f)), -Vector3.up, out rayHit, Mathf.Infinity)){
			surfaceNormal = rayHit.normal;

			//CETTE LIGNE SERT A CE QUE LE JOUEUR BOUGE EN MEME TEMPS QUE SA PLATE FORME
			myCharaparenting.SetPlayerParent(rayHit);
		}
			

		//Input dir + velocity
		InputSpeedDir = GetInputSpeedDir();

		//Surface dir + velocity
		SetVelocitySpeedDir();

		shouldSpeedDir = InputSpeedDir + current_VelocitySpeedDir;

		myController.Move(shouldSpeedDir * Time.deltaTime);

		//Pseudo grav
		if(Physics.Raycast(transform.position, -Vector3.up, out rayHit, Mathf.Infinity)){
			Vector3 _tempVector = myController.transform.position;
			_tempVector.y = rayHit.point.y + myController.bounds.extents.y;
			myController.transform.position = _tempVector;
		}

		DebugFunction();
	}


	/// <summary>
	/// Vitesse et direction du joueur par surface
	/// </summary>
	void SetVelocitySpeedDir ()
	{

		Vector3 _vectorToReturn = Vector3.zero;

		//Direction

		Vector3 _tempTang = Vector3.Cross(rayHit.normal, Vector3.up);
		Vector3 _tang = Vector3.Cross(rayHit.normal, _tempTang);

		Debug.DrawLine(rayHit.point, rayHit.point + _tang * 5f, Color.black);

		float fromCtoG = ((Vector3.Angle(Vector3.up, surfaceNormal) - Confort_angle)) / (Glide_angle - Confort_angle);
		fromCtoG = Mathf.Clamp(fromCtoG, 0f, 1f);
		surface_VelocitySpeedDir = _tang.normalized * (maxGlideSpeed * velocityGlideAcceleration.Evaluate(fromCtoG));

//		print(velocityGlideAcceleration.Evaluate(fromCtoG));

		//lerp de current à surface

		if(surface_VelocitySpeedDir.magnitude > current_VelocitySpeedDir.magnitude)
		{
			current_VelocitySpeedDir = Vector3.MoveTowards(current_VelocitySpeedDir, surface_VelocitySpeedDir, velocityTransitionSpeed_acceleration * Time.deltaTime);
		}
		else
		{
			current_VelocitySpeedDir = Vector3.MoveTowards(current_VelocitySpeedDir, surface_VelocitySpeedDir, velocityTransitionSpeed_decceleration * Time.deltaTime);
		}
			
	}


	/// <summary>
	/// Vitesse et direction du joueur par Input
	/// </summary>
	Vector3 GetInputSpeedDir ()
	{

		Vector3 _vectorToReturn = Vector3.zero;

		//Direction
//		Vector3 _inputVector = (Vector3.forward * Input.GetAxis("Vertical")) + Vector3.right * Input.GetAxis("Horizontal");	//Input Axis en tant que vec3

		Vector3 _inputVector = (cam.transform.forward * Input.GetAxis("Vertical")) + (cam.transform.right * Input.GetAxis("Horizontal"));
		_inputVector.y = 0f;
		_inputVector.Normalize();

		if(Vector3.Angle(Vector3.up, surfaceNormal) > Glide_angle)
			_inputVector = Vector3.zero;
//		print("input magnitude " + _inputVector.magnitude);
		Vector3 _vectorTolook = _inputVector;		//Direction que le controler doit regarder
		if(_inputVector.magnitude < 0.3)
			_vectorTolook = transform.forward;

		//Rotation speed
//		currentRotationSpeed = ((max_RotationSpeed - min_RotationSpeed) * (1/_v_value)) + min_RotationSpeed;
		currentRotationSpeed = ((max_RotationSpeed - min_RotationSpeed) * rotationBySpeed.Evaluate(_v_value)) + min_RotationSpeed;
//		currentRotationSpeed = (-1390 * _v_value) + 1440;

		//Rotation
		Quaternion _toRot = Quaternion.LookRotation(_vectorTolook, transform.up);
		transform.rotation = Quaternion.RotateTowards(transform.rotation, _toRot, currentRotationSpeed * Time.deltaTime);

		//au cas ou on look nul part
		_vectorToReturn = transform.forward;

		//Translation Speed
//		_vectorToReturn *= maxInputSpeed * _inputVector.magnitude;
		_vectorToReturn = _vectorToReturn.normalized * GetCurrentSpeedByCurve(_vectorTolook.normalized * _inputVector.magnitude);

		return _vectorToReturn;
	}



	[Range(0f,1f)]
	public float _t_time = 0.0f;
	[Range(0f,1f)]	
	public float _v_value = 0.0f;
	enum CurvesOfSpeed {Accelerate, Deccelerate, NotMoving};
	private CurvesOfSpeed currentCurveOfSpeed = CurvesOfSpeed.NotMoving;
	private CurvesOfSpeed lastFrameCurveOfSpeed = CurvesOfSpeed.NotMoving;
	/// <summary>
	/// Renvoie une vitesse en float. Choisi une courbe en fonction de la direction et de la magnitude de la pression du stick
	/// </summary>
	float GetCurrentSpeedByCurve(Vector3 directionAndMagnitude)
	{
		float _floatToReturn = 0.0f;
//		float _t_time;		TODO trouver comment passer par l'init qu'une fois

		if(directionAndMagnitude.magnitude > 0.2f)
		{
			//Le joueur utilise le stick : on accelere

			if(currentCurveOfSpeed != CurvesOfSpeed.Accelerate)
			{
				_t_time = SetTimeToEquivalent(InputAcceleration, _v_value, 40);
				currentCurveOfSpeed = CurvesOfSpeed.Accelerate;
//				print("Begin accelerate" + _t_time);
			}

			_t_time += Time.deltaTime;
			//Clamp to stick inclinaison
			float _v_unclamped = InputAcceleration.Evaluate(_t_time);
			if(_v_unclamped > directionAndMagnitude.magnitude)
				_t_time -= Time.deltaTime;
			_v_value = InputAcceleration.Evaluate(_t_time);

		}
		else
		{
			//Le joueur a laché le stick : on deccelere

			if(currentCurveOfSpeed != CurvesOfSpeed.Deccelerate)
			{
				_t_time = SetTimeToEquivalent(InputDecceleration, _v_value, 20);
				currentCurveOfSpeed = CurvesOfSpeed.Deccelerate;
//				print("Begin deccelerate" + _t_time);
			}

			_t_time += Time.deltaTime;
			_v_value = InputDecceleration.Evaluate(_t_time);

		}
			
//		_t_time = Mathf.Clamp(_t_time, 0.0f, 1.0f);		//TODO trouver un moyen d'unclamper (donc vérifier quel pourcentage magnitude est par rapport au time de la dernière key de la curve)
		_t_time = Mathf.Clamp(_t_time, 0.0f, 10.0f);	//ou alors on s'en fou (Grace à ma super fonction SetTimeToEquivalent :D)

		_floatToReturn = maxInputSpeed * _v_value;

		return _floatToReturn;
	}

	/// </summary>
	///Récupère le premier equivalent t le plus proche de la courbe (une bonne accuracy pour une courbe de 0 à 1 est à peu près de 20)
	/// </summary>
	float SetTimeToEquivalent(AnimationCurve curveToCheck, float value, int accuracy)
	{
//		print("value unclamped " + value);
		value = Mathf.Clamp(value, 0f, curveToCheck.keys[curveToCheck.keys.Length - 1].time);
//		print(accuracy);
		float accuracyNormalized = (Vector2.up * accuracy).normalized.magnitude;
//		print(accuracyNormalized);

//		float _step = 1f/accuracy;
		float _step = curveToCheck.keys[curveToCheck.keys.Length - 1].time /accuracy;
		float _v_hypotetic = 0.0f;
		float difference = Mathf.Infinity;
		float nearest = 0.0f;

		for (float t_hypotetic = 0f; t_hypotetic < accuracyNormalized; t_hypotetic += _step) {

			_v_hypotetic = curveToCheck.Evaluate(t_hypotetic);

			if(Mathf.Abs(_v_hypotetic - value) < difference)
			{
				difference = Mathf.Abs(_v_hypotetic - value);
				nearest = t_hypotetic;
			}

		}

//		print("v = " + value + " | v_hypotetic = " + curveToCheck.Evaluate(nearest));
//		print("t_hypotetic = " + nearest);

		return nearest;
	}


	private Color violet = new Color(0.5f, 0.0f, 0.5f);
	void DebugFunction()
	{
		//Player - up
		Debug.DrawRay(transform.position + (Vector3.up * myController.bounds.extents.y), -Vector3.up, Color.blue);
		//Surface Normal
		Debug.DrawLine(rayHit.point, rayHit.point + surfaceNormal * 2.5f, (Vector3.Angle(Vector3.up, surfaceNormal) < Confort_angle) ? Color.cyan : (Vector3.Angle(Vector3.up, surfaceNormal) < Glide_angle) ? violet : Color.red);
	}
}
