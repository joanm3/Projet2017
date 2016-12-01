using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadSceneOnInput : MonoBehaviour {


    public bool loadSceneOnSpacePressed = false;
    public bool loadActifScene = true; 
    public int sceneToLoad = 0; 

	void Update () {

        if (!loadSceneOnSpacePressed)
            return;

        if(loadActifScene)
		    if(Input.GetButtonDown("Jump"))
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
        else if (Input.GetButtonDown("Jump"))
                SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);

    }
}
