using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class plaitAnimation : MonoBehaviour {


    [Tooltip("Le Bone parent de tous les autres Bones")]
    [SerializeField]
    private Transform RootTransform;
	[Tooltip("La nuque")]
	[SerializeField]
	private Transform nuque;		//TODO utiliser la nuque comme transform dont on copie la position
	[Tooltip("Vitesse de rotation de \tchaque bone vers son résultat (en angle/seconde)")]
    [SerializeField]
    private float BoneRotSpeed = 45f;
    [Tooltip("Courbe d'adaptation des bones au mouvement dans l'espace. Le Time c'est l'index du bone (0 = la nuque, 1 = le bout de la tresse). La value c'est l'adaptation de l'orientation (0 = ignore la direction du mouvement, 1 = orientation instantanée)")]
    [SerializeField]
    private AnimationCurve WeightCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f);
    [Tooltip("Courbe d'adaptation des bones à la rotation du joueur. Le Time c'est l'index du bone (0 = la nuque, 1 = le bout de la tresse). La value c'est l'adaptation de l'orientation (0 = ignore les rotations du joueur, 1 = copie totalement la rotation du joueur)")]
    [SerializeField]
    private AnimationCurve BoneToParentRotCurve = AnimationCurve.EaseInOut(0f, 0.9f, 1f, 0.1f);
    [Tooltip("Inclinnaison max de chaque bone")]
    [SerializeField]
    [Range(1f, 90f)]
    private float maxAngle = 25f;

	public Vector3 directionOfFalling = -Vector3.up;

    private List<Transform> m_bonesList;
    private List<Transform> m_initialBonesTransform;

    private Vector3 m_lastPos = Vector3.zero;
    private Vector3 m_moveDirection = Vector3.zero;
    private Quaternion m_lastFrameRot = Quaternion.identity;
    private Quaternion m_hierarchyRotation = Quaternion.identity;   //Rotation given by the parent by default
    private Quaternion m_baseRot = Quaternion.identity;
    private Vector3 m_center;

	void Start () {

        m_bonesList = new List<Transform>(RootTransform.GetComponentsInChildren<Transform>());
        m_initialBonesTransform = new List<Transform>(m_bonesList);
        m_lastFrameRot = RootTransform.rotation;
        m_hierarchyRotation = RootTransform.rotation;

        m_baseRot = RootTransform.rotation;
        m_center = RootTransform.up;

	}
	
	void LateUpdate () {

        //Reset rotation (We don't need to copy the parent's rotation, we keep our own from the last frame)
        m_hierarchyRotation = RootTransform.rotation;   //Copy the rotation we are supposed to have
        RootTransform.rotation = m_lastFrameRot;       //Reset the root rotation as it was before the update of the unity transform hierarchy

        //Check the movement since the last frame
        m_moveDirection = m_lastPos - RootTransform.position;

        //Act on every bone
        for (int i = 0; i < m_bonesList.Count; i++)
        {

            //Get the current parenting transform of the next bone
            Quaternion _hierarchyNextBoneRotation = Quaternion.identity;
            if (i + 1 <= m_bonesList.Count - 1)
                _hierarchyNextBoneRotation = m_bonesList[i + 1].rotation;

            //Rot de parenting (we get a portion of the rotation of the parent)
            //m_bonesList[i].rotation = GetOrientationByPartialParent(m_bonesList[i], transform, m_hierarchyRotation, 1f - ((1f/m_bonesList.Count) * i));
            m_bonesList[i].rotation = GetOrientationByPartialParent(m_bonesList[i], transform, m_hierarchyRotation, BoneToParentRotCurve.Evaluate((1f / m_bonesList.Count) * i));

            //Reset the parenting of the next bone (so we can apply the true one at the next iteration of the loop)
            if (i + 1 <= m_bonesList.Count - 1)
                 m_bonesList[i + 1].rotation = _hierarchyNextBoneRotation;



            //Rot vers direction de deplacement
            m_bonesList[i].rotation = GetOrientationByMovement(m_bonesList[i], m_moveDirection, BoneRotSpeed, WeightCurve.Evaluate((1f / m_bonesList.Count) * i));

			//Clamp
//			m_bonesList[i].up = ClampVector(m_bonesList[i].rotation * m_bonesList[i].up, m_center, maxAngle);

        }

        #region save last frame data
        //Keep last pos in memory
        m_lastPos = RootTransform.position;
        //Keep last rot in memory
        m_lastFrameRot = RootTransform.rotation;
        #endregion
    }


//	Vector3 ClampVector(Vector3 direction, Vector3 center, float maxAngle){
//
//
//		float angle = Vector3.Angle(center, direction);
//		if(angle > maxAngle){
//
//			direction.Normalize();
//			center.Normalize();
//			Vector3 rotation = (direction - center) / angle;
//			return (rotation * maxAngle) + center;
//
//		}
//
//		return direction;
//
//	}

    /// <summary>
    /// Return a portion on the rotation of the parent.
    /// </summary>
    Quaternion GetOrientationByPartialParent(Transform _boneTranform, Transform _rootTransform, Quaternion _supposedRotation, float _proportion)
    {
        //Rotate sur un axe pour se rapprocher de la rotation du parent
        //print(_proportion);

        //Anti error
        if (_proportion < 0f || _proportion > 1f)
            Debug.LogError("Carrefull !! You are trying to assignate a proportion out of Range [0,1] to the rotation Lerp. Have you send the correct data to GetOrientationByPartialParent at _proportion paraeter ?");

        Quaternion toReturn;

        Quaternion rotByProportion = Quaternion.Lerp(_boneTranform.rotation, _supposedRotation, _proportion);

        toReturn = rotByProportion;

        return toReturn;
    }

    /// <summary>
    /// Return bone orientation towards a move direction smoothly. Uses weight [0 to 1] to determine how much of the speed we are using each frame
    /// </summary>
    Quaternion GetOrientationByMovement(Transform _boneTransform, Vector3 _moveDir, float _rotSpeed, float _weight)
    {
		Quaternion toReturn;
		//Check if moving
		Vector3 _finalMoveVector = (_moveDir.normalized.magnitude > 0f) ? _moveDir : directionOfFalling;

		//We rotate the up of the bone toward the desired vector, because this is the way the rig was created in it's 3d software
		Quaternion fromTo = Quaternion.FromToRotation(_boneTransform.up, _finalMoveVector) * _boneTransform.rotation;

        if(_weight < 0 || _weight > 1)
            Debug.LogError("Carrefull !! You are trying to assignate a weight out of Range [0,1] to the rotation Lerp. Have you send the correct data to GetOrientationByMovement at _weight parameter ?");
        float _finalRotSpeed = _rotSpeed * _weight;
        //_finalRotSpeed = _rotSpeed;

		//Smooth the result
		toReturn = Quaternion.RotateTowards(_boneTransform.rotation, fromTo, _finalRotSpeed * Time.deltaTime);

		return toReturn;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(RootTransform.position, m_moveDirection);

    }

}
