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

	public bool limitXAngle = false;
	public float xTest = 1f;
	public float waitingTimeToMoveCamera = 2f; 

	private Transform m_transform;
	private Camera m_cam;
	private float m_trueDistance;

	private bool m_fadeRaycastEntered = false;
	private bool m_obstacleRaycastEntered = false;
	private MeshRenderer m_colliderRenderer = null;

	private const float Y_ANGLE_MIN = -20.0f;
	private const float Y_ANGLE_MAX = 50.0f;


	private const float X_ANGLE_MIN = -50.0f;
	private const float X_ANGLE_MAX = 50.0f;

	private string obstacleLayerString = "Obstacle";
	private string fadeLayerString = "Fade";

	private float m_noKeysTouchedTime = 0.0f; 

	// Use this for initialization
	void Start ()
	{
		m_transform = transform; 
		m_cam = Camera.main; 
		m_trueDistance = distance;
	}
		
	// Update is called once per frame
	void Update ()
	{
		
		currentX += Input.GetAxis ("Mouse X");
		currentY += Input.GetAxis ("Mouse Y");

		currentY = Mathf.Clamp (currentY, Y_ANGLE_MIN, Y_ANGLE_MAX); 
		if (limitXAngle)
			currentX = Mathf.Clamp (currentX, X_ANGLE_MIN, X_ANGLE_MAX); 			
	}



	void FixedUpdate ()
	{
		Vector3 _rayDirection = playerTransform.position - m_transform.position; 
		RaycastHit _hit; 
		
		Debug.DrawRay (playerTransform.position, -_rayDirection, Color.red); 

		//When ray detected
		if (Physics.Raycast (playerTransform.position, -_rayDirection, out _hit, distance, SumLayers (obstacleMask, fadeMask))) {

			//Obstacle behaviour
			if (_hit.collider.gameObject.layer == LayerMask.NameToLayer (obstacleLayerString)) {
				
				m_colliderRenderer = _hit.collider.GetComponent<MeshRenderer> (); 
				float _lerpedAlpha = (m_colliderRenderer.material.color.a >= 0.001f) ? Mathf.Lerp (m_colliderRenderer.material.color.a, 0.0f, Time.fixedDeltaTime * lerpVelocity * 3.5f) : 0f;  
				m_colliderRenderer.material.color = new Color (m_colliderRenderer.material.color.r, m_colliderRenderer.material.color.g, m_colliderRenderer.material.color.b, _lerpedAlpha); 
				m_trueDistance = Mathf.Lerp (m_trueDistance, _hit.distance, Time.fixedDeltaTime * lerpVelocity * 2f);
				m_obstacleRaycastEntered = true; 

			// fade Object behaviour
			} else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer (fadeLayerString))) {
				
				m_colliderRenderer = _hit.collider.GetComponent<MeshRenderer> (); 
				float _lerpedAlpha = (m_colliderRenderer.material.color.a >= 0.001f) ? Mathf.Lerp (m_colliderRenderer.material.color.a, 0.5f, Time.fixedDeltaTime * lerpVelocity * 3f) : 0f;  
				m_colliderRenderer.material.color = new Color (m_colliderRenderer.material.color.r, m_colliderRenderer.material.color.g, m_colliderRenderer.material.color.b, _lerpedAlpha); 
				m_fadeRaycastEntered = true; 
			}
				
		//when no ray behaviour
		} else {
			m_trueDistance = Mathf.Lerp (m_trueDistance, distance, Time.fixedDeltaTime * lerpVelocity); 

			//when exiting a fade object
			if (m_fadeRaycastEntered && m_colliderRenderer != null) {
				
				float _lerpedAlpha = Mathf.Lerp (m_colliderRenderer.material.color.a, 1f, Time.fixedDeltaTime * lerpVelocity); 
				m_colliderRenderer.material.color = new Color (m_colliderRenderer.material.color.r, m_colliderRenderer.material.color.g, m_colliderRenderer.material.color.b, _lerpedAlpha); 
				if (_lerpedAlpha >= 0.9f) {
					m_colliderRenderer.material.color = new Color (m_colliderRenderer.material.color.r, m_colliderRenderer.material.color.g, m_colliderRenderer.material.color.b, 1f); 
					m_colliderRenderer = null; 
					m_fadeRaycastEntered = false; 
				}

			//when exiting a obstacle object
			} else if (m_obstacleRaycastEntered && m_colliderRenderer != null) {
				
				float _lerpedAlpha =  Mathf.Lerp (m_colliderRenderer.material.color.a, 1.0f, Time.fixedDeltaTime * lerpVelocity);  
				m_colliderRenderer.material.color = new Color (m_colliderRenderer.material.color.r, m_colliderRenderer.material.color.g, m_colliderRenderer.material.color.b, _lerpedAlpha); 
				if (_lerpedAlpha >= 0.9f) {
					m_colliderRenderer.material.color = new Color (m_colliderRenderer.material.color.r, m_colliderRenderer.material.color.g, m_colliderRenderer.material.color.b, 1f);
					m_colliderRenderer = null; 
					m_obstacleRaycastEntered = false; 
				}

			}

		}
	}

	public float testX = 0f; 

	void LateUpdate ()
	{

		Vector3 _dir = new Vector3 (0, 0, -m_trueDistance); 
		Quaternion _rotation = Quaternion.Euler (currentY, currentX + testX, 0f); 
		m_transform.position = playerTransform.position + _rotation * _dir; 
		m_transform.LookAt (playerTransform.position); 

	}


	public static int SumLayers (LayerMask first, LayerMask second)
	{

		return first.value + second.value; 
	}

}
