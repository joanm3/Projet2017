using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class smoothOrientTowardsDirection : MonoBehaviour {

	[Header("Oriente les objets vers une direction (sens de gravité)")]

	public Transform container;

//	public float anglePerSecond = 25f;

	private List<Transform> objectsInContainer;

	void Start () {
		objectsInContainer = new List<Transform>(container.GetComponentsInChildren<Transform>());

	}
	
	void LateUpdate () {

		for (int i = 1; i < objectsInContainer.Count; i++) {

			//Check if moving
			Vector3 _finalMoveVector = -Vector3.up;

			//We rotate the up of the bone toward the desired vector, because this is the way the rig was created in it's 3d software
			Quaternion fromTo = Quaternion.FromToRotation(objectsInContainer[i].up, _finalMoveVector) * objectsInContainer[i].rotation;

//			objectsInContainer[i].rotation = Quaternion.RotateTowards(objectsInContainer[i].rotation, fromTo, anglePerSecond * Time.deltaTime);

			objectsInContainer[i].rotation = fromTo;

		}

	}
}
