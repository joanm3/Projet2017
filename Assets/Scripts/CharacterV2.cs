using UnityEngine;
using System.Collections;

public class CharacterV2 : MonoBehaviour {

	[SerializeField]
	[Range(1.0f, 50.0f)]
	private float speedInputMax = 25.0f;

	[SerializeField]
	[Range(1.0f, 50.0f)]
	private float speedInputMax_whileTurning = 15.0f;

	[SerializeField]
	private AnimationCurve inputAcceleration;

	[SerializeField]
	private AnimationCurve inputDeceleration;

	private CharacterController myController;
	private Camera camRef;
	private CharacterInputSpeed InputSpeed;
	private CharacterVelocitySpeed VelocitySpeed;



	private Vector3 shouldDirection = Vector3.zero;

	void Start()
	{
		myController = GetComponent<CharacterController>();
		camRef = Camera.main;
		InputSpeed = new CharacterInputSpeed(inputAcceleration, inputDeceleration);
		VelocitySpeed = new CharacterVelocitySpeed();
	}

	void Update()
	{


		//Calcul de direction et de vitesse
//		shouldDirection = InputSpeed.GetTheInputSpeed() + VelocitySpeed.GetTheVelocitySpeed();
		Vector3 _inputSpeed = InputSpeed.GetTheInputSpeed();
		Vector3 _velSpeed = VelocitySpeed.GetTheVelocitySpeed();
		shouldDirection = _inputSpeed + _velSpeed;

		myController.Move(shouldDirection * Time.deltaTime);

	}

}
