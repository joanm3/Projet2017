using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadSceneOnInput : MonoBehaviour {


	void Update () {
	
		if(Input.GetButtonDown("Jump"))
			SceneManager.LoadScene(0, LoadSceneMode.Single);

	}
}
