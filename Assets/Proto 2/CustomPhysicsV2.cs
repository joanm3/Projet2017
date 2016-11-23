using UnityEngine;
using System.Collections;

public class CustomPhysicsV2 : MonoBehaviour {

	[SerializeField]
	[Range(1.1f, 10.0f)]
	private float gravityForce = 5.0f;

	const float distToGround = 2.5f;

	private Collider myCollider;
	private RaycastHit rayHit;
	private Vector3 shouldPosition = Vector3.zero;
	private Vector3 myCurrentVelocity = Vector3.zero;
	private bool isGrounded = false;
	private Vector3 inputDirection = Vector3.zero;

	void Start () {
		myCollider = GetComponent<Collider>();
	}
	
	void Update () {
	
		shouldPosition = transform.position - (Vector3.up * gravityForce);

		//SNAP la position Y au premier collider rencontré
		//(
		if(Physics.Raycast(transform.position, -transform.up, out rayHit, Mathf.Infinity)){

			shouldPosition.y = Mathf.Clamp(shouldPosition.y, rayHit.point.y, Mathf.Infinity);	//Clamp la position y au point de contact sous les pieds du collider

			isGrounded = Vector3.Distance(transform.position, rayHit.point) < myCollider.bounds.extents.y + distToGround;

		}else{
			isGrounded = false;
		}
		//)







		//APPLY
		shouldPosition.y += myCollider.bounds.extents.y;	//Replacer le shouldPosition.y aux pieds du collider (et non plus au centre)
		transform.position = shouldPosition;


		//DEBUG
		//Debug gravity
		Debug.DrawLine(transform.position, transform.position - (Vector3.up * gravityForce), Color.cyan);

	}
}
