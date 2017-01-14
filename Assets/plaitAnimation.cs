using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class plaitAnimation : MonoBehaviour {

    #region Data Requirements from other scripts
    [HideInInspector]
    public Vector3 moveDirection;   //This is the direction applied to the character at the end of the movement
    #endregion
    //Pourquoi ne pas calculer à l'intérieur de ce script ? on récup la position de la dernière frame et c'est bon. Il suffit de copy paste le script et il marche

    [Tooltip("Le Bone parent de tous les autres Bones")]
    [SerializeField]
    private Transform RootTransform;
    [Tooltip("Vitesse de rotation de chaque bone vers son résultat (en angle/seconde)")]
    [SerializeField]
    private float BoneRotSpeed = 45f;

    private List<Transform> m_bonesList;
    private List<Transform> m_initialBonesTransform;

    private Vector3 m_lastPos = Vector3.zero;
    private Vector3 m_moveDirection = Vector3.zero;     
	private Vector3 m_initialDecalagePosition = Vector3.zero;	//The placement of the RootTransform relative to PlayerRender (the object on wich this script is attached)

	void Start () {

        m_bonesList = new List<Transform>(RootTransform.GetComponentsInChildren<Transform>());
        m_initialBonesTransform = new List<Transform>(m_bonesList);

		//Unparent. We need to have a custom child/parent relation
		RootTransform.parent = null;
		m_initialDecalagePosition = RootTransform.position - transform.position;
	}
	
	void LateUpdate () {

		//Child parent relation position
//		RootTransform.position = transform.position + m_initialDecalagePosition;

        //Check the movement since the last frame
        m_moveDirection = m_lastPos - RootTransform.position;

        //Act on every bone
        for (int i = 0; i < m_bonesList.Count; i++)
        {
			//Rot de parenting
			//TODO

			//Rot vers direction de deplacement
            m_bonesList[i].rotation = GetBoneOrientation(m_initialBonesTransform[i], m_bonesList[i], m_moveDirection, BoneRotSpeed);
            
        }

        //Keep last pos in memory
        m_lastPos = RootTransform.position;

	}

    /// <summary>
    /// Return bone orientation towards a direction. The amount of rotation is clamped.
    /// </summary>
    Quaternion GetBoneOrientation(Transform _initialBoneTransform, Transform _boneTransform, Vector3 _moveDir, float _rotSpeed)
    {
		Quaternion toReturn;
		//Check if moving
		Vector3 _finalMoveVector = (_moveDir.normalized.magnitude > 0f) ? _moveDir : -Vector3.up;

		//We rotate the up of the bone toward the desired vector, because this is the way the rig was created in it's 3d software
		Quaternion fromTo = Quaternion.FromToRotation(_boneTransform.up, _finalMoveVector) * _boneTransform.rotation;

		//Smooth the result
		toReturn = Quaternion.RotateTowards(_boneTransform.rotation, fromTo, _rotSpeed * Time.deltaTime);

		return toReturn;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(RootTransform.position, m_moveDirection);

    }
}
