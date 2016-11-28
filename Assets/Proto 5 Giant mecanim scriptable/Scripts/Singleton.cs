using UnityEngine;

/// <summary>
/// Be aware this will not prevent a non singleton constructor
///   such as `T myT = new T();`
/// To prevent that, add `protected T () {}` to your singleton class.
/// 
/// As a note, this is made as MonoBehaviour because we need Coroutines.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	public static T Instance; 

	private void Awake()
	{
		if (Instance == null) 
		{
			Instance = T; 
		} 
		else if (Instance != this) 
		{
			Destroy (this); 
		}

		OnAwake (); 

	}

	internal void OnAwake()
	{
	}


	private void OnDestroy()
	{
		Instance = null; 
	}

}