using UnityEngine;
using System.Collections;

public class ThirdPersonCameraMovement : MonoBehaviour
{

	public Transform playerTransform;
	public CharacterController playerController;

	public float distance = 10f;
	public float currentX = 0.0f;
	public float currentY = 0.0f;
	public float sensitivityX = 4.0f;
	public float sensitivityY = 1.0f;
	public float lerpVelocity = 5f;
	public LayerMask obstacleMask;
	public LayerMask fadeMask;
	public LayerMask terrainMask;

	public bool limitXAngle = false;
	public float xTest = 1f;
	public float waitingTimeToMoveCamera = 2f;

	private Transform m_transform;
	private Camera m_cam;
	private float m_trueDistance;

	private bool m_fadeRaycastEntered = false;
	private bool m_obstacleRaycastEntered = false;
	private bool m_terrainRaycastEntered = false;
	private Renderer m_colliderRend = null;
	private TerrainCollider m_terrainCollider = null;

	private const float Y_ANGLE_MIN = -20.0f;
	private const float Y_ANGLE_MAX = 50.0f;


	private const float X_ANGLE_MIN = -50.0f;
	private const float X_ANGLE_MAX = 50.0f;

	private const string obstacleLayerString = "Obstacle";
	private const string fadeLayerString = "Fade";
	private const string terrainLayerString = "Terrain";


	private float m_noKeysTouchedTime = 0.0f;

	// Use this for initialization
	void Start ()
	{
		m_transform = transform; 
		m_cam = Camera.main; 
		m_trueDistance = distance;

		if (playerTransform == null)
			playerTransform = GameObject.FindGameObjectWithTag ("Player").transform; 

		if (playerController == null)
			playerController = GameObject.FindGameObjectWithTag ("Player").GetComponent<CharacterController> (); 
	}

	Vector3 point; 

	void Update ()
	{

		//Debug.Log (m_trueDistance); 
		currentX += Input.GetAxis ("Mouse X");
		currentY += Input.GetAxis ("Mouse Y");

		currentY = Mathf.Clamp (currentY, Y_ANGLE_MIN, Y_ANGLE_MAX); 
		if (limitXAngle)
			currentX = Mathf.Clamp (currentX, X_ANGLE_MIN, X_ANGLE_MAX); 	

		Vector3 _rayDirection = playerTransform.position - m_transform.position; 
		RaycastHit _hit; 

		//When ray detected
		if (Physics.Raycast (playerTransform.position, -_rayDirection, out _hit, distance, SumLayers (obstacleMask, fadeMask, terrainMask))) {

			//Obstacle behaviour
			if (_hit.collider.gameObject.layer == LayerMask.NameToLayer (obstacleLayerString)) {
				m_colliderRend = GetRendererFromCollision (_hit); 
				//m_colliderRenderer = _hit.collider.GetComponent<MeshRenderer> (); 
				if (m_colliderRend.enabled) {
					float _lerpedAlpha = (m_colliderRend.material.color.a >= 0.001f) ? Mathf.Lerp (m_colliderRend.material.color.a, 0.0f, Time.fixedDeltaTime * lerpVelocity * 3.5f) : 0f;  
					m_colliderRend.material.color = new Color (m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha); 
					//Debug.Log (_lerpedAlpha); 
					m_trueDistance = Mathf.Lerp (m_trueDistance, _hit.distance, Time.fixedDeltaTime * lerpVelocity * 2f);
					m_obstacleRaycastEntered = true; 
				}

				// fade Object behaviour
			} else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer (fadeLayerString))) {
				
				m_colliderRend = GetRendererFromCollision (_hit); 
				if (m_colliderRend.enabled) {				 
					float _lerpedAlpha = (m_colliderRend.material.color.a >= 0.001f) ? Mathf.Lerp (m_colliderRend.material.color.a, 0.5f, Time.fixedDeltaTime * lerpVelocity * 3f) : 0f;  
					m_colliderRend.material.color = new Color (m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha); 
					m_fadeRaycastEntered = true; 
				}
				//terrain behaviour
			} else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer (terrainLayerString))) {
			
				m_terrainCollider = _hit.collider.GetComponent<TerrainCollider> (); 
				if (m_terrainCollider.enabled) {
					m_trueDistance = Mathf.Lerp (m_trueDistance, _hit.distance, Time.fixedDeltaTime * lerpVelocity * 2f);
					Debug.Log (_hit.distance);
					point = _hit.point; 
					m_terrainRaycastEntered = true;  
				}
			}
		}
			

		if (m_fadeRaycastEntered && m_colliderRend != null) {

			float _lerpedAlpha = Mathf.Lerp (m_colliderRend.material.color.a, 1f, Time.fixedDeltaTime * lerpVelocity); 
			Debug.Log (_lerpedAlpha); 
			m_colliderRend.material.color = new Color (m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha); 
			//Debug.Log (m_colliderRend.material.color); 
			if (_lerpedAlpha >= 0.9f) {
				m_colliderRend.material.color = new Color (m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, 1f); 
				m_colliderRend = null; 
				m_fadeRaycastEntered = false; 
			}

			//when exiting a obstacle object
		} else if (m_obstacleRaycastEntered && m_colliderRend != null) {

			float _lerpedAlpha = Mathf.Lerp (m_colliderRend.material.color.a, 1.0f, Time.fixedDeltaTime * lerpVelocity);  
			m_colliderRend.material.color = new Color (m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha); 
			if (_lerpedAlpha >= 0.9f) {
				m_colliderRend.material.color = new Color (m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, 1f);
				m_colliderRend = null; 
				m_obstacleRaycastEntered = false; 
			}

		}
			
		//when exiting terrain
		if (m_terrainRaycastEntered && m_terrainCollider != null) {
				
			m_terrainCollider = null; 
			m_terrainRaycastEntered = false; 
		}



		if (!m_terrainRaycastEntered && !m_obstacleRaycastEntered)
			m_trueDistance = (Mathf.Abs (m_trueDistance - distance) > 0.1f) ? Mathf.Lerp (m_trueDistance, distance, Time.fixedDeltaTime * lerpVelocity) : distance; 

//		Debug.Log("Raycast is: " + Physics.Raycast (playerTransform.position, -_rayDirection, out _hit, distance, SumLayers (obstacleMask, fadeMask, terrainMask))); 
//		Debug.Log ("fade: " + m_fadeRaycastEntered);  
//		Debug.Log ("obstacle: " + m_obstacleRaycastEntered); 
//		Debug.Log ("terrain: " + m_terrainRaycastEntered); 
//		//Debug.Log ("terrain collider: " + m_terrainCollider); 
//

		//Debug.Log (m_trueDistance); 
	}


	public float testX = 0f;

	void LateUpdate ()
	{

		Vector3 _dir = new Vector3 (0, 0, -m_trueDistance); 
		Quaternion _rotation = Quaternion.Euler (currentY, currentX + testX, 0f); 
		m_transform.position = playerTransform.position + _rotation * _dir; 
		m_transform.LookAt (playerTransform.position); 

	}

	private void OnDrawGizmos ()
	{

		if (!Application.isPlaying)
			return; 
		Vector3 _rayDirection = playerTransform.position - m_transform.position; 
		Gizmos.color = Color.black; 
		Debug.DrawRay (playerTransform.position, -_rayDirection); 
		Gizmos.color = Color.cyan; 
		Gizmos.DrawSphere (point, 1f); 

	}


	public static Renderer GetRendererFromCollision (RaycastHit hit)
	{

		Renderer _colliderRend = (Renderer)hit.collider.GetComponent<MeshRenderer> (); 
		if (_colliderRend == null)
			_colliderRend = (Renderer)hit.collider.GetComponent<MeshRenderer> (); 
		if (_colliderRend == null)
			_colliderRend = (Renderer)hit.collider.GetComponent<SkinnedMeshRenderer> (); 	

		return _colliderRend; 

	}

	public static int SumLayers (LayerMask first, LayerMask second)
	{

		return first.value + second.value; 
	}

	public static int SumLayers (LayerMask first, LayerMask second, LayerMask third)
	{

		return first.value + second.value + third.value; 
	}


}
