using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour {

	private float horDepl = 0.0f;
	private float vertDepl = 0.0f;

	public float maxAngle = 73.0f;
	public float speedCam = 75.0f;

	public Transform target;

	void Update () {

		//	transform.position = player.transform.position;


		//Input = mouvement
		//	var XValor = Input.GetAxisRaw("QDAx");
		//	var YValor = Input.GetAxisRaw("ZSAx");
		float XValor = Input.GetAxis("360_R_Stick_X");
		float YValor = Input.GetAxis("360_R_Stick_Y");
		float axValor = Vector2.Distance(Vector2.zero, new Vector2(XValor, YValor));

//		print(axValor);

		if(axValor > 0.5f){
			horDepl += XValor * speedCam * Time.deltaTime;
			vertDepl += YValor * speedCam * Time.deltaTime;
		}
		//angle vertical maximum
		if(vertDepl > maxAngle) vertDepl = maxAngle;
		if(vertDepl < -maxAngle) vertDepl = -maxAngle;

		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(vertDepl, horDepl, 0.0f), 20.0f * Time.deltaTime);

	}

	void LateUpdate(){
		transform.position = target.position;
	}

}
