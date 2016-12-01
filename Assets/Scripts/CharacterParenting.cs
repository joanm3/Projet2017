using UnityEngine;
using System.Collections;

public class CharacterParenting : MonoBehaviour {
	
	[Header("Ce script sert à synchroniser le joueur sur les mouvements du membre sur lequel il se trouve.")]

	//public fonctions for characterV3.
	/*
	 * 1 - it makes the player child of the surface he is currently colliding
	 * 2 - it ressets its rotation so it not wiggle with his parent
	 * 
	 */

	//TODO Upgrade this script by using the uv coordinates instead of parenting ?

	[Tooltip("Empeche le parent de changer la rotation du joueur (càd force le joueur à être vertical)")]
	public bool resetPlayerVerticality = true;

	public BoneColliderLink[] links;

	public void SetPlayerParent (RaycastHit rayhit)
	{
		//Compare le collider sur lequel on a les pieds avec tous les colliders de la liste
		for (int i = 0; i < links.Length; i++)
		{
			//Lorsqu'on trouve le collider, on récupère le bone auquel il est link
			if(rayhit.collider.transform == links[i].myCollider)
			{
				transform.parent = links[i].myBone;
//				print(transform.parent.name);
				break;
			}else{
				transform.parent = null;
			}
		}
	
		if(resetPlayerVerticality)
		{
			transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
		}

	}

}

//TODO Attention, 1 collider 1 bone. A prendre en compte pendant la modélisation et l'animation
//Si c'est trop restrictif pour la DA (ça a de fortes chances de l'etre), Alors prévoir une journée complette pour améliorer ce script

[System.Serializable]
public class BoneColliderLink
{
	public string name;
	public Transform myCollider;
	public Transform myBone;

}