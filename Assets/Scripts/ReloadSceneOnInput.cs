﻿using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadSceneOnInput : MonoBehaviour
{

    public KeyCode keyToLoad = KeyCode.R;

    public bool loadActifScene = true;
    public int sceneToLoad = 0;

    void Update()
    {



        if (loadActifScene)
        {
            if (Input.GetKeyDown(keyToLoad))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
            }
        }
        else if (Input.GetKeyDown(keyToLoad))
        {
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
        }

    }
}
