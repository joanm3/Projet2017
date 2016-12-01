using UnityEngine;
using System.Collections;

public class AnimatorScript : MonoBehaviour {

	Animator myAnimatorController;

	void Start () {
		myAnimatorController = GetComponent<Animator>();
	}
	

	void Update () {
	
		if(Input.GetButtonDown("A"))
		{
			myAnimatorController.SetBool("Rise", true);
		}
		if(Input.GetButtonDown("B"))
		{
			myAnimatorController.SetBool("Rise", false);
		}

	}
}
