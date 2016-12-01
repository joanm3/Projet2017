using UnityEngine;
using System.Collections;

public class CustomPhysicCharacter : MonoBehaviour {

	[SerializeField]
	[Range(1.1f, 10.0f)]
	private float gravityForce = 5.0f;

	[SerializeField]
	[Range(35.0f, 75.0f)]
	private float AngleMaxWalkableSurface = 50.0f;

	[SerializeField]
	[Range(1.0f, 100.0f)]
	private float maxVelocity = 10.0f;

	[SerializeField]
	[Range(1.0f, 100.0f)]
	private float maxDistDelta = 10.0f;

	[SerializeField]
	[Range(0.1f, 1.0f)]
	private float distToGround = 0.25f;

	[SerializeField]
	[Range(0.1f, 5.0f)]
	private float playerInputSpeed = 2.0f;

	[SerializeField]
	[Range(0.1f, 25.0f)]
	private float playerMoveSpeed = 5.0f;

	private CharacterController myCollider;
	private RaycastHit rayHit;
	private Vector3 shouldPosition = Vector3.zero;
	private Transform pivotRender;
	private Vector3 shouldInclinaison = Vector3.up;
	private Vector3 myCurrentVelocity = Vector3.zero;
	private bool isGrounded = false;
	private Vector3 inputDirection = Vector3.zero;

	void Start () {

		myCollider = GetComponent<CharacterController>();
		pivotRender = transform.FindChild("PivotRender").transform;

	}




	void Update () {

		//Wanted position
		shouldPosition = transform.position - (Vector3.up * gravityForce);
		float _shouldGravity = shouldPosition.y;

		Debug.DrawLine(transform.position, shouldPosition, Color.cyan);

		//GRAVITY VERTIAL

		//Physic snap calculation
		if(Physics.Raycast(transform.position, -transform.up, out rayHit, Mathf.Infinity)){

			shouldPosition.y = Mathf.Clamp(shouldPosition.y, rayHit.point.y, Mathf.Infinity);	//Clamp la position y au point de contact sous les pieds du collider

			isGrounded = Vector3.Distance(transform.position, rayHit.point) < myCollider.bounds.extents.y + distToGround;

		}else{
			isGrounded = false;
		}

		//On dirais que cette ligne l'empeche de descendre sous 0
		shouldPosition.y = Mathf.Clamp(shouldPosition.y, _shouldGravity * Time.deltaTime, Mathf.Infinity);	//Clamp la position y aux effets de la gravité
		//		print(_shouldGravity);




		//INPUT INCLINAISON
		//		shouldInclinaison = transform.position + transform.up;
		//		shouldInclinaison += transform.right * Input.GetAxis("Horizontal");
		//		shouldInclinaison += transform.forward * Input.GetAxis("Vertical");
		//
		//		Vector3 toDir = shouldInclinaison - pivotRender.position;
		//
		//		pivotRender.rotation = Quaternion.LookRotation(pivotRender.forward, toDir);


		//Detecte la direction qui va vers le bas le long de la surface
		//Get the direction towards the bottom of the surface
		Vector3 _tempTang = Vector3.Cross(rayHit.normal, Vector3.up);
		Vector3 _tang = Vector3.Cross(rayHit.normal, _tempTang);

		Debug.DrawLine(rayHit.point, rayHit.point + _tang, Color.yellow);


		//Get the inclinaison in angle
		//		float _surfaceInclinaison = Vector3.Angle(Vector3.up, rayHit.normal);


		//velocity by angle

		if(isGrounded){
			//IL FAUT ENCORE CLAMP AU SOL (le collider traverse le sol (debug ligne noire)), + revoir le code {
			float _speedByAngle = Mathf.Clamp(-Vector3.Dot(Vector3.up, rayHit.normal) + 1.0f, 0.0f, 1.0f);		//inverser le rapport 0 à 1


			myCurrentVelocity = Vector3.MoveTowards(myCurrentVelocity, _tang.normalized * maxDistDelta * _speedByAngle, maxVelocity * Time.deltaTime);


			Debug.DrawLine(transform.position, transform.position + myCurrentVelocity, Color.black);

			//ShouldPosition + velocity
			shouldPosition += myCurrentVelocity * Time.deltaTime;
		}
		//}

		//Should + INPUT
		if(isGrounded){
			inputDirection = Vector3.MoveTowards(inputDirection, (Vector3.forward * Input.GetAxis("Vertical")) + (Vector3.right * Input.GetAxis("Horizontal")), playerInputSpeed * Time.deltaTime);

			Debug.DrawLine(transform.position, transform.position + inputDirection, Color.green);

			shouldPosition += inputDirection * playerMoveSpeed * Time.deltaTime;

		}

		//Apply position
		shouldPosition.y += myCollider.bounds.extents.y;	//Replacer le shouldPosition.y aux pieds du collider (et non plus au centre)
//		transform.position = shouldPosition;
//		myCollider.Move(transform.position - (transform.position + shouldPosition).normalized * Time.deltaTime);

		Debug.DrawLine(transform.position, transform.position - (transform.position + shouldPosition), Color.grey);



		//DEBUG

		//ShouldPosition
		Debug.DrawLine(shouldPosition, shouldPosition + Vector3.forward, Color.blue);
		Debug.DrawLine(shouldPosition, shouldPosition + Vector3.right, Color.red);
		Debug.DrawLine(shouldPosition, shouldPosition + Vector3.up, Color.green);

		//ShouldInclinaison
		//		Debug.DrawLine(pivotRender.position, shouldInclinaison, Color.magenta);

		//SurfaceNormal
		//		if(Physics.Raycast(transform.position, -transform.up, out rayHit, Mathf.Infinity)){
		Debug.DrawRay(rayHit.point, rayHit.normal * 5.0f, Color.red);
		//		}

	}
}
