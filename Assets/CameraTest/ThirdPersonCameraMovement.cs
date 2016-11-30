using UnityEngine;
using System.Collections;

public class ThirdPersonCameraMovement : MonoBehaviour
{

	public Transform playerTransform;

	public float distance = 10f;
	public float currentX = 0.0f;
	public float currentY = 0.0f;
	public float sensitivityX = 4.0f;
	public float sensitivityY = 1.0f;
	public float lerpVelocity = 5f;
	public LayerMask obstacleMask;
	public LayerMask fadeMask;


	private Transform m_transform;
	private Camera m_cam;
	private float m_trueDistance;

	private const float Y_ANGLE_MIN = -20.0f;
	private const float Y_ANGLE_MAX = 50.0f;

	private static string obstacleLayerString = "Obstacle"; 
	private static string fadeleLayerString = "Fade"; 


	// Use this for initialization
	void Start ()
	{
		m_transform = transform; 
		m_cam = Camera.main; 
		m_trueDistance = distance; 

//		Debug.Log ("obstacle: " + obstacleMask.value);
//		Debug.Log ("fade: " + fadeMask.value); 
//		Debug.Log ("sum: " + (fadeMask.value + obstacleMask.value)); 

	

	}
	
	// Update is called once per frame
	void Update ()
	{




		currentX += Input.GetAxis ("Mouse X");
		currentY += Input.GetAxis ("Mouse Y");

		currentY = Mathf.Clamp (currentY, Y_ANGLE_MIN, Y_ANGLE_MAX); 

	}

	private bool m_alphaChanged = false; 
	public MeshRenderer m_alphaFadeRenderer = null; 

	void FixedUpdate ()
	{
		Vector3 _rayDirection = playerTransform.position - m_transform.position; 
		RaycastHit _hit; 
		
		Debug.DrawRay (playerTransform.position, -_rayDirection, Color.red); 
		Material _hitMaterial;  
		//better if made in one single raycast, not two. to see later on. 
		if (Physics.Raycast (playerTransform.position, -_rayDirection, out _hit, distance, SumLayers (obstacleMask, fadeMask))) {
//			Debug.Log (LayerMask.NameToLayer("Obstacle")); 
//			Debug.Log (_hit.collider.gameObject.layer); 
//			Debug.Log (LayerMask.NameToLayer ("Obstacle") == _hit.collider.gameObject.layer); 


			if (_hit.collider.gameObject.layer == LayerMask.NameToLayer (obstacleLayerString)) {
				m_trueDistance = Mathf.Lerp (m_trueDistance, _hit.distance, Time.fixedDeltaTime * lerpVelocity * 3f);
				Debug.Log (-m_trueDistance); 
			} else if((_hit.collider.gameObject.layer ==LayerMask.NameToLayer(fadeleLayerString))){
				m_alphaFadeRenderer = _hit.collider.GetComponent<MeshRenderer> (); 
				_hitMaterial = m_alphaFadeRenderer.material; 
				_hitMaterial.color = new Color (_hitMaterial.color.r, _hitMaterial.color.g, _hitMaterial.color.b, 0.5f); 
				m_alphaFadeRenderer.material = _hitMaterial; 
				m_alphaChanged = true; 
			}

//			switch (_hit.collider.gameObject.layer) {
//			case LayerMask.NameToLayer(obstacleLayerString):
//				m_trueDistance = Mathf.Lerp (m_trueDistance, _hit.distance, Time.fixedDeltaTime * lerpVelocity * 3f);
//				Debug.Log (-m_trueDistance); 
//				break; 
//
//			case LayerMask.NameToLayer(fadeleLayerString):
//				m_alphaFadeRenderer = _hit.collider.GetComponent<MeshRenderer> (); 
//				_hitMaterial = m_alphaFadeRenderer.material; 
//				_hitMaterial.color = new Color (_hitMaterial.color.r, _hitMaterial.color.g, _hitMaterial.color.b, 0.5f); 
//				m_alphaFadeRenderer.material = _hitMaterial; 
//				m_alphaChanged = true; 
//				break; 
//
//			default:
//				break; 
//			}





		} else {
			m_trueDistance = Mathf.Lerp (m_trueDistance, distance, Time.fixedDeltaTime * lerpVelocity); 
			if (m_alphaChanged && m_alphaFadeRenderer != null) {
				m_alphaFadeRenderer.material.color = new Color (m_alphaFadeRenderer.material.color.r, m_alphaFadeRenderer.material.color.g, m_alphaFadeRenderer.material.color.b, 1f); 
				m_alphaFadeRenderer = null; 
				m_alphaChanged = false; 
			}

		}
	}



	void LateUpdate ()
	{
		Vector3 _dir = new Vector3 (0, 0, -m_trueDistance); 
		Quaternion _rotation = Quaternion.Euler (currentY, currentX, 0f); 
		m_transform.position = playerTransform.position + _rotation * _dir; 
		m_transform.LookAt (playerTransform.position); 

	}

	public static int SumLayers (LayerMask first, LayerMask second)
	{

		return first.value + second.value; 
	}

}
