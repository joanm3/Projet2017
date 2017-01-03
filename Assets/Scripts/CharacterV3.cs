using UnityEngine;
using System.Collections;

[RequireComponent (typeof(CharacterController))]
public class CharacterV3 : MonoBehaviour
{

	[Header ("Angles")]
	[Space (20.0f)]

	[Tooltip ("Jusqu'à quel angle sommes nous en Confort (Gizmos cyan)")]
	[Range (1.0f, 45.0f)]
	public float Confort_angle = 25.0f;
	[Tooltip ("Jusqu'à quel angle sommes nous en Glide (au delà = Fall) (Gizmos violet puis rouge)")]
	[Range (45.0f, 90.0f)]
	public float Glide_angle = 25.0f;

	[Space (20.0f)]
	[Header ("Surface")]

	[Tooltip ("La vitesse que donne la surface de Glide_angle maximum")]
	[Range (5.0f, 50.0f)]
	public float maxGlideSpeed = 15.0f;
	[Tooltip ("Time est utilisée en tant qu'indicateur de surface. 0 = Confort_angle, 1 = Glide_angle. Value est utilisé pour définir quel proportion de maxGlideSpeed est utilisé (0 = 0%, 1 = 100%)")]
	public AnimationCurve velocityGlideAcceleration = AnimationCurve.EaseInOut (0.0f, 0.0f, 1.0f, 1.0f);
	[Tooltip ("Vitesse de transition par seconde entre la velocité actuelle et celle fournie par la surface (1 = 1 unité de vitesse par seconde). lORSQUE LA VELOCITE MONTE")]
	[Range (1.0f, 40.0f)]
	public float velocityTransitionSpeed_acceleration = 2.0f;
	[Tooltip ("Vitesse de transition par seconde entre la velocité actuelle et celle fournie par la surface (1 = 1 unité de vitesse par seconde). LORSQUE LA VELOCITE BAISSE")]
	[Range (1.0f, 40.0f)]
	public float velocityTransitionSpeed_decceleration = 0.5f;

	[Space (20.0f)]
	[Header ("Input")]

	[Tooltip ("La vitesse que donne le stick sur une surface plane")]
	[Range (5.0f, 40.0f)]
	public float maxInputSpeed = 15.0f;
	[Tooltip ("Vitesse de rotation maximum (en angles par seconde)")]
	[Range (5.0f, 1440.0f)]
	public float max_RotationSpeed = 200.0f;
	[Tooltip ("Vitesse de rotation minimum (en angles par seconde)")]
	[Range (5.0f, 360.0f)]
	public float min_RotationSpeed = 50.0f;
	[Tooltip ("Vitesse de rotation en fonction de la vitesse de deplacement input. De gauche à droite la vitesse de deplacement input, de haut en bas la vitesse de rotation. Interpolation entre min_RotationSpeed et max_RotationSpeed en fonction de la curve")]
	public AnimationCurve rotationBySpeed = AnimationCurve.Linear (0.0f, 0.0f, 1.0f, 1.0f);
	[Tooltip ("Interpolation entre 0 et maxInputSpeed")]
	public AnimationCurve InputAcceleration = AnimationCurve.EaseInOut (0.0f, 0.0f, 1.0f, 1.0f);
	[Tooltip ("Interpolation entre maxInputSpeed et 0")]
	public AnimationCurve InputDecceleration = AnimationCurve.Linear (0.0f, 1.0f, 0.5f, 0.0f);

	[HideInInspector]
	public Vector3 FirstTang = Vector3.zero;
	[HideInInspector]
	public Vector3 TangDownwards = Vector3.zero;
	[HideInInspector]
	public float fromCtoG = 0f;
	public AnimationCurve gravForceOverTime;
    public float maxGravForce = 100f; 
	[Range (0f, 1f)]
	public float _t_time = 0.0f;
	[Range (0f, 1f)]	
	public float _v_value = 0.0f;
    [HideInInspector]
    public Vector3 inputVector;
    [HideInInspector]
    public bool canUseInput = true;
    public CharacterParenting myCharaparenting;


    private float tGrav = 0f; 

	private Camera cam;
	private CharacterController myController;
	private RaycastHit rayHit;
	private Vector3 surfaceNormal;
	private Vector3 current_VelocitySpeedDir = Vector3.zero;
	//Velocité actuelle
	private Vector3 surface_VelocitySpeedDir = Vector3.zero;
	//Velocité renseignée par la surface
	private Vector3 InputSpeedDir = Vector3.zero;
	//Direction et vitesse
	private Vector3 shouldSpeedDir = Vector3.zero;
	//Direction et vitesse finale
	
	private float currentRotationSpeed = 0.0f;
	private Color violet = new Color (0.5f, 0.0f, 0.5f);

	private CurvesOfSpeed currentCurveOfSpeed = CurvesOfSpeed.NotMoving;
	private CurvesOfSpeed lastFrameCurveOfSpeed = CurvesOfSpeed.NotMoving;

    private enum CharacterState { Balanced, Instable, Air, FallTest };
    private CharacterState characterState; 

    private enum CurvesOfSpeed
    {
        Accelerate,
        Deccelerate,
        NotMoving
    };

	void Start ()
	{

		myController = GetComponent<CharacterController> ();
		myController.slopeLimit = Glide_angle;
		cam = Camera.main;
		gravSurfaceData = transform.FindChild("SphereSurfCol").GetComponent<GetGravitySurface>();

        if (myCharaparenting == null)
        {
            myCharaparenting = FindObjectOfType<CharacterParenting>(); 
        }

	}

	public StabilityState myState = StabilityState.Stable;
	public enum StabilityState
	{
		Stable,
		Unstable,
		Falling
	}

	GetGravitySurface gravSurfaceData;


	void Update ()
	{
		float _rayDistance = 1f; 
		Vector3 _rayDirection = -Vector3.up; 

		//Cast a Ray to get basic surface data
		if (Physics.Raycast (transform.position + (-Vector3.up * (myController.bounds.extents.y - 0.1f)), _rayDirection, out rayHit, _rayDistance)) {
			//surfaceNormal = rayHit.normal;

			//CETTE LIGNE SERT A CE QUE LE JOUEUR BOUGE EN MEME TEMPS QUE SA PLATE FORME
			if(myCharaparenting != null)
				myCharaparenting.SetPlayerParent (transform, rayHit);
		}

		//Recup la normale de la surface
		surfaceNormal = gravSurfaceData.averageSurfaceNormal;

		//Maj State en fonction de surface normal
		myState = UpdateCurrentSurfaceState(surfaceNormal);



        //Input dir + vel
		InputSpeedDir = GetInputSpeedDir();

        //Surface dir + velocity
		SetVelocitySpeedDir ();


		//GRAV
		if(Physics.Raycast(transform.position, -Vector3.up, out rayHit, Mathf.Infinity)){


			if(Vector3.Distance(transform.position - (Vector3.up * myController.bounds.extents.y), rayHit.point) > 0.5f || myState == StabilityState.Falling)
			{
				gravForce = Vector3.up * (-maxGravForce * gravForceOverTime.Evaluate(tGrav));
				tGrav += Time.deltaTime;
			}
			else
			{
				tGrav = 0f;
				gravForce = -Vector3.up * 10f;
			}


		}

		shouldSpeedDir = InputSpeedDir + current_VelocitySpeedDir + gravForce;
//		print("Total : " + shouldSpeedDir + " / input : " + InputSpeedDir + " / velocity : " + current_VelocitySpeedDir + " / gravForce : " + gravForce);
		myController.Move (shouldSpeedDir * Time.deltaTime);

    }


	StabilityState UpdateCurrentSurfaceState(Vector3 theNormal)
	{
		if(Vector3.Angle(theNormal, Vector3.up) > Glide_angle || Vector3.Distance(transform.position - (Vector3.up * myController.bounds.extents.y), rayHit.point) > 0.5f)
		{
			return StabilityState.Falling;
		}
		else if(Vector3.Angle(theNormal, Vector3.up) < Glide_angle)
		{
			if(Vector3.Angle(theNormal, Vector3.up) < Confort_angle)
			{
				return StabilityState.Stable;
			}
			else
			{
				return StabilityState.Unstable;
			}
		}

		return StabilityState.Stable;
	}

	Vector3 gravForce = Vector3.zero;

	/// <summary>
	/// Vitesse et direction du joueur par surface
	/// </summary>
	void SetVelocitySpeedDir ()
	{

		Vector3 _vectorToReturn = Vector3.zero;

		//Direction

		FirstTang = Vector3.Cross (rayHit.normal, Vector3.up);
		TangDownwards = Vector3.Cross (rayHit.normal, FirstTang);

		Debug.DrawLine (rayHit.point, rayHit.point + TangDownwards * 5f, Color.black);

		fromCtoG = ((Vector3.Angle (Vector3.up, surfaceNormal) - Confort_angle)) / (Glide_angle - Confort_angle);
		fromCtoG = Mathf.Clamp (fromCtoG, 0f, 1f);
		surface_VelocitySpeedDir = TangDownwards.normalized * (maxGlideSpeed * velocityGlideAcceleration.Evaluate (fromCtoG));

		//lerp de current à surface

			//si accelere
			if (surface_VelocitySpeedDir.magnitude > current_VelocitySpeedDir.magnitude)
			{
				current_VelocitySpeedDir = Vector3.MoveTowards (current_VelocitySpeedDir, surface_VelocitySpeedDir, velocityTransitionSpeed_acceleration * Time.deltaTime);
			}
			//Si decelere
			else
			{
				current_VelocitySpeedDir = Vector3.MoveTowards (current_VelocitySpeedDir, surface_VelocitySpeedDir, velocityTransitionSpeed_decceleration * Time.deltaTime);
			}
		//TODO cette partie là doit avoir un algorythme de transition plus effeicace (pour éviter le probleme du personnage qui subit une force pas logique car la transition est trop lente)		
	}



    /// <summary>
    /// Vitesse et direction du joueur par Input
    /// </summary>
    Vector3 GetInputSpeedDir ()
	{

		Vector3 _vectorToReturn = Vector3.zero;
		//Direction
		inputVector = (cam.transform.forward * Input.GetAxis ("Vertical")) + (cam.transform.right * Input.GetAxis ("Horizontal"));
		inputVector.y = 0f;
		inputVector.Normalize ();
		//TODO ici, réorienter inputVector le long de la surface

		//Si nous sommes en fall, alors on ne prend plus en compte les input
//		if (Vector3.Angle (Vector3.up, surfaceNormal) > Glide_angle)
//			inputVector = Vector3.zero;
		
		Vector3 _vectorTolook = inputVector;		//Direction que le controler doit regarder
		if (inputVector.magnitude < 0.3)
			_vectorTolook = transform.forward;

		//Rotation speed
		currentRotationSpeed = ((max_RotationSpeed - min_RotationSpeed) * rotationBySpeed.Evaluate (_v_value)) + min_RotationSpeed;

		//Rotation
		Quaternion _toRot = Quaternion.LookRotation (_vectorTolook, transform.up);
		Quaternion _rRot = Quaternion.RotateTowards (transform.rotation, _toRot, currentRotationSpeed * Time.deltaTime);

		transform.rotation = _rRot;

		//au cas ou on look nul part
		_vectorToReturn = transform.forward;

		//Translation Speed
		_vectorToReturn = _vectorToReturn.normalized * GetCurrentSpeedByCurve (_vectorTolook.normalized * inputVector.magnitude);

		return _vectorToReturn;
	}





	/// <summary>
	/// Renvoie une vitesse en float. Choisi une courbe en fonction de la direction et de la magnitude de la pression du stick
	/// </summary>
	float GetCurrentSpeedByCurve (Vector3 directionAndMagnitude)
	{
		float _floatToReturn = 0.0f;

		if (directionAndMagnitude.magnitude > 0.2f) {
			//Le joueur utilise le stick : on accelere

			if (currentCurveOfSpeed != CurvesOfSpeed.Accelerate) {
				_t_time = SetTimeToEquivalent (InputAcceleration, _v_value, 40);
				currentCurveOfSpeed = CurvesOfSpeed.Accelerate;
//				print("Begin accelerate" + _t_time);
			}

			_t_time += Time.deltaTime;
			//Clamp to stick inclinaison
			float _v_unclamped = InputAcceleration.Evaluate (_t_time);
			if (_v_unclamped > directionAndMagnitude.magnitude)
				_t_time -= Time.deltaTime;
			_v_value = InputAcceleration.Evaluate (_t_time);

		} else {
			//Le joueur a laché le stick : on deccelere

			if (currentCurveOfSpeed != CurvesOfSpeed.Deccelerate) {
				_t_time = SetTimeToEquivalent (InputDecceleration, _v_value, 20);
				currentCurveOfSpeed = CurvesOfSpeed.Deccelerate;
			}

			_t_time += Time.deltaTime;
			_v_value = InputDecceleration.Evaluate (_t_time);

		}
			
		_t_time = Mathf.Clamp (_t_time, 0.0f, 10.0f);	//ou alors on s'en fou (Grace à ma super fonction SetTimeToEquivalent :D)
		_floatToReturn = maxInputSpeed * _v_value;
		return _floatToReturn;
	}

	/// </summary>
	///Récupère le premier equivalent t le plus proche de la courbe (une bonne accuracy pour une courbe de 0 à 1 est à peu près de 20)
	/// </summary>
	float SetTimeToEquivalent (AnimationCurve curveToCheck, float value, int accuracy)
	{
		value = Mathf.Clamp (value, 0f, curveToCheck.keys [curveToCheck.keys.Length - 1].time);
		float accuracyNormalized = (Vector2.up * accuracy).normalized.magnitude;
		float _step = curveToCheck.keys [curveToCheck.keys.Length - 1].time / accuracy;
		float _v_hypotetic = 0.0f;
		float difference = Mathf.Infinity;
		float nearest = 0.0f;

		for (float t_hypotetic = 0f; t_hypotetic < accuracyNormalized; t_hypotetic += _step) {
			_v_hypotetic = curveToCheck.Evaluate (t_hypotetic);
			if (Mathf.Abs (_v_hypotetic - value) < difference) {
				difference = Mathf.Abs (_v_hypotetic - value);
				nearest = t_hypotetic;
			}
		}
		return nearest;
	}


	void OnDrawGizmos ()
	{


		if (!Application.isPlaying)
			return; 
		
		Gizmos.color = Color.blue; 
		//Player - up
		Gizmos.DrawRay (transform.position + (Vector3.up * myController.bounds.extents.y), -Vector3.up); 

		//Surface Normal
		Gizmos.color = (Vector3.Angle (Vector3.up, surfaceNormal) < Confort_angle) ? 
			Color.cyan : 
			(Vector3.Angle (Vector3.up, surfaceNormal) < Glide_angle) ? 
			violet : Color.yellow;
		Gizmos.DrawLine (rayHit.point, rayHit.point + surfaceNormal * 2.5f); 
	}
}