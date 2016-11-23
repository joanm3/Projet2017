using UnityEngine;
using System.Collections;

public class gravity : MonoBehaviour {


	public float gravityStrength = 20.0f;
	public float jumpStrength = 20.0f;

	private Vector3 currentJump = Vector3.zero;
	private Vector3 currentVelocity = Vector3.zero;
	private float t = 0.0f;

	private Collider myCol;
	private float distToGround;

	private Vector3 boundsY;

	void Start (){
		myCol = GetComponent<Collider>();
		distToGround = myCol.bounds.extents.y;
		boundsY = new Vector3(0, myCol.bounds.extents.y,0);
	}

	void Update () {
	
		if(Input.GetButtonDown("Jump") && IsGrounded()){
			print("jump !");
			currentJump = transform.position + Vector3.up * jumpStrength;
			t = 0.0f;
			currentVelocity = Vector3.zero;
		}else if(IsGrounded()){
			RaycastHit hit;
			if (Physics.Raycast(transform.position, -Vector3.up, out hit, distToGround + 0.1f))
				transform.position = hit.point + boundsY;
				
		}

		t += Time.deltaTime;
		currentJump = Vector3.Lerp(currentJump, (transform.position - Vector3.up * gravityStrength), t * Time.deltaTime);

		if(IsGrounded() && !Input.GetButtonDown("Jump") && t > 0.1f){
			print("grounded");
			currentJump = transform.position;
			currentVelocity = Vector3.zero;
		}

		transform.position = Vector3.SmoothDamp(transform.position, currentJump, ref currentVelocity, 1.0f);


	}

	bool IsGrounded() {
		return Physics.Raycast(transform.position, -Vector3.up, distToGround + 0.1f);
	}
}
