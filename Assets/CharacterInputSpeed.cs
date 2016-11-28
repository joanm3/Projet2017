using UnityEngine;
using System.Collections;

class CharacterInputSpeed {


	public CharacterInputSpeed(AnimationCurve accel, AnimationCurve decelNoInput)
	{
		inputAcceleration = accel;
		inputDeceleration = decelNoInput;
	}

	private AnimationCurve inputAcceleration;
	private AnimationCurve inputDeceleration;

	public float currentSpeed = 0.0f;

	private Vector3 inputAxis = Vector3.zero;

	/// <summary>
	/// Donne un Vector3 de direction avec sa magnitude (non-normalisé). Input Joueur
	/// </summary>
	public Vector3 GetTheInputSpeed ()
	{
		inputAxis = (Vector3.forward * Input.GetAxis("Vertical")) + (Vector3.right * Input.GetAxis("Horizontal"));

//		Vector3

		//Si le stick est utilisé
		if(inputAxis.magnitude > 0.2f)
		{
		
			inputAxis *= GetSpeed();

		}

		return inputAxis;
	}

	float GetSpeed()
	{

		//

		return 0.0f;
	}


	// NB. Will only work for curves with one definite time for each value
	/// <summary>
	/// Renvoie la valeur "t" qui se trouve à la première occurence de "value" sur la curve "curveToCheck"
	/// </summary>
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

}
