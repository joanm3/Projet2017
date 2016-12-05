using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThirdPersonCameraMovement : MonoBehaviour
{

    public Transform playerTransform;
    public CharacterController playerController;
    public bool useJoystick = false;

    [Header("Camera values")]
    public float distance = 10f;
    public float cameraSpeed = 2f;
    public float sensitivityX = 4.0f;
    public float sensitivityY = 1.0f;
    public float lerpVelocity = 5f;
    public LayerMask obstacleMask;
    public LayerMask fadeMask;
    public LayerMask terrainMask;
    public float currentX = 0.0f;
    public float currentY = 0.0f;

    [Header("Angle clamp")]
    public float yAngleMin = -20.0f;
    public float yAngleMax = 50.0f;
    public bool limitXAngle = false;
    public float xAngleMin = -50.0f;
    public float xAngleMax = 50.0f;

    [Header("Modificiations")]
    public float xModificationAngle = 0f;
    public float yModificationAngle = 0f;
    public float waitingTimeToMoveCamera = 2f;



    [Header("Pivot local transform")]
    public Transform cameraPivotTransform;
    public float pivotXPosition = 0f;
    public float pivotYPosition = 0f;
    public float pivotZPosition = 0f;

    public float pivotXRotation = 0f;
    public float pivotYRotation = 0f;
    public float pivotZRotation = 0f;

    private Transform m_transform;
    private Camera m_cam;
    private float m_trueDistance;

    private bool m_fadeRaycastEntered = false;
    private bool m_obstacleRaycastEntered = false;
    private bool m_terrainRaycastEntered = false;
    private Renderer m_colliderRend = null;
    private TerrainCollider m_terrainCollider = null;

    private List<Renderer> m_colliderRenderers = new List<Renderer>();
    private List<TerrainCollider> m_terrainColliders = new List<TerrainCollider>();






    private const string obstacleLayerString = "Obstacle";
    private const string fadeLayerString = "Fade";
    private const string terrainLayerString = "Terrain";


    private float m_noKeysTouchedTime = 0.0f;

    Vector3 gizmoPoint;
    Vector3 gizmoRayDirection;
    float gizmoDistance;
    Vector3 gizmoRayOrigin; 

    // Use this for initialization
    void Start()
    {
        m_transform = transform;
        m_cam = Camera.main;
        m_trueDistance = distance;

        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if (playerController == null)
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterController>();
    }



    void Update()
    {

        //Debug.Log (m_trueDistance); 
        if (!useJoystick)
        {
            currentX += Input.GetAxis("Mouse X");
            currentY += Input.GetAxis("Mouse Y");
        }
        else
        {
            float inputXStick = (Input.GetAxis("360_R_Stick_X") > 0.15f || Input.GetAxis("360_R_Stick_X") < -0.15f) ? Input.GetAxis("360_R_Stick_X") : 0f;
            float inputYStick = (Input.GetAxis("360_R_Stick_Y") > 0.15f || Input.GetAxis("360_R_Stick_Y") < -0.15f) ? Input.GetAxis("360_R_Stick_Y") : 0f;

            currentX += inputXStick * cameraSpeed;
            currentY += inputYStick * cameraSpeed;
        }

        currentY = Mathf.Clamp(currentY, yAngleMin, yAngleMax);
        if (limitXAngle)
            currentX = Mathf.Clamp(currentX, xAngleMin, xAngleMax);

        Vector3 _rayDirection = -(playerTransform.position - m_transform.position);
        _rayDirection = playerTransform.position - cameraPivotTransform.position;
        Vector3 _rayOrigin = cameraPivotTransform.position;
        RaycastHit _hit;
        float _distance = distance + (Vector3.Distance(m_transform.position, cameraPivotTransform.position));
        _distance = distance; 
        gizmoRayDirection = _rayDirection;
        gizmoDistance = _distance;
        gizmoRayOrigin = _rayOrigin; 

        //add multiple rays, better for effect. 
        //When ray detected
        //RaycastHit[] _hits = Physics.RaycastAll(playerTransform.position, _rayDirection, _distance, SumLayers(obstacleMask, fadeMask, terrainMask)); 
        //RaycastObstacles(_rayOrigin, _rayDirection, out _hit, _distance);

        if (Physics.Raycast(_rayOrigin, _rayDirection, out _hit, distance, SumLayers(obstacleMask, fadeMask, terrainMask)))
        {
            Debug.Log("entered the raycast"); 
            //Obstacle behaviour
            if (_hit.collider.gameObject.layer == LayerMask.NameToLayer(obstacleLayerString))
            {

                if (m_colliderRend != null)
                {
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, 1f);
                }


                m_colliderRend = GetRendererFromCollision(_hit);
                //m_colliderRenderer = _hit.collider.GetComponent<MeshRenderer> (); 
                if (m_colliderRend.enabled)
                {
                    float _lerpedAlpha = (m_colliderRend.material.color.a >= 0.001f) ? Mathf.Lerp(m_colliderRend.material.color.a, 0.0f, Time.deltaTime * lerpVelocity * 3.5f) : 0f;
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha);
                    //Debug.Log (_lerpedAlpha); 
                    m_trueDistance = Mathf.Lerp(m_trueDistance, _hit.distance, Time.deltaTime * lerpVelocity * 2f);
                    m_obstacleRaycastEntered = true;
                }

                // fade Object behaviour
            }
            else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer(fadeLayerString)))
            {

                if (m_colliderRend != null)
                {
                    // m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, 1f);
                }

                m_colliderRend = GetRendererFromCollision(_hit);
                if (m_colliderRend.enabled)
                {
                    float _lerpedAlpha = (m_colliderRend.material.color.a >= 0.001f) ? Mathf.Lerp(m_colliderRend.material.color.a, 0.5f, Time.deltaTime * lerpVelocity * 3f) : 0f;
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha);
                    m_fadeRaycastEntered = true;
                }

                //terrain behaviour
            }
            else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer(terrainLayerString)))
            {

                if (m_terrainCollider != null)
                {
                    //add code here when exiting a terrainCollider
                }

                m_terrainCollider = _hit.collider.GetComponent<TerrainCollider>();
                if (m_terrainCollider.enabled)
                {
                    m_trueDistance = Mathf.Lerp(m_trueDistance, _hit.distance, Time.deltaTime * lerpVelocity * 2f);
                    //Debug.Log (_hit.distance);
                    gizmoPoint = _hit.point;
                    m_terrainRaycastEntered = true;
                }
            }
        }


        //BEHAVIOUR when exiting fade object
        if (m_fadeRaycastEntered && m_colliderRend != null)
        {

            float _lerpedAlpha = Mathf.Lerp(m_colliderRend.material.color.a, 1f, Time.fixedDeltaTime * lerpVelocity);
            //Debug.Log(_lerpedAlpha);
            m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha);
            //Debug.Log (m_colliderRend.material.color); 
            if (_lerpedAlpha >= 0.9f)
            {
                m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, 1f);
                m_colliderRend = null;
                m_fadeRaycastEntered = false;
            }

        }

        //BEHAVIOUR when exiting a obstacle object
        else if (m_obstacleRaycastEntered && m_colliderRend != null)
        {

            float _lerpedAlpha = Mathf.Lerp(m_colliderRend.material.color.a, 1.0f, Time.fixedDeltaTime * lerpVelocity);
            m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha);
            if (_lerpedAlpha >= 0.9f)
            {
                m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, 1f);
                m_colliderRend = null;
                m_obstacleRaycastEntered = false;
            }

        }

        //BEHAVIOUR when exiting terrain
        if (m_terrainRaycastEntered && m_terrainCollider != null)
        {
            m_terrainCollider = null;
            m_terrainRaycastEntered = false;
        }


        //behaviour when exiting a collision on the camera raycast
        //ExitRaycastObstacles(m_colliderRend, m_terrainCollider);


        //add here the code for the camera raycast to the ground

        float _distancePivot = Vector3.Distance(m_transform.position, cameraPivotTransform.position);

        //Debug.Log(Mathf.Abs(m_trueDistance - distance)); 

        if (!m_terrainRaycastEntered && !m_obstacleRaycastEntered)
            m_trueDistance = (Mathf.Abs(m_trueDistance - distance) > 0.1f) ?
                Mathf.Lerp(m_trueDistance, distance, Time.fixedDeltaTime * lerpVelocity) : distance;

    }





    void LateUpdate()
    {
        Vector3 _dir = new Vector3(0, 0, -m_trueDistance);
        Quaternion _rotation;

        if (useJoystick)
        {
            _rotation = Quaternion.Euler(currentY + yModificationAngle, currentX + xModificationAngle, 0f);
            m_transform.position = playerTransform.position + _rotation * _dir;
        }
        else
        {
            _rotation = Quaternion.Euler(currentY + yModificationAngle, currentX + xModificationAngle, 0f);
            m_transform.position = playerTransform.position + _rotation * _dir;
        }

        m_transform.LookAt(playerTransform.position);

        cameraPivotTransform.localPosition = new Vector3(pivotXPosition, pivotYPosition, 0f);
        cameraPivotTransform.localRotation = Quaternion.Euler(pivotXRotation,pivotYRotation, pivotZRotation); 
    }


    private void OnDrawGizmos()
    {

        if (!Application.isPlaying)
            return;
        //Vector3 gizmoRayDirection = playerTransform.position - m_transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(gizmoRayOrigin, gizmoRayDirection);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(gizmoPoint, 1f);

    }


    private void RaycastObstacles(Vector3 rayOrigin, Vector3 rayDirection, out RaycastHit hitInfo, float distance)
    {

        if (Physics.Raycast(rayOrigin, rayDirection, out hitInfo, distance, SumLayers(obstacleMask, fadeMask, terrainMask)))
        {

            //Obstacle behaviour
            if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer(obstacleLayerString))
            {

                if (m_colliderRend != null)
                {
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, 1f);
                }


                m_colliderRend = GetRendererFromCollision(hitInfo);
                //m_colliderRenderer = _hit.collider.GetComponent<MeshRenderer> (); 
                if (m_colliderRend.enabled)
                {
                    float _lerpedAlpha = (m_colliderRend.material.color.a >= 0.001f) ? Mathf.Lerp(m_colliderRend.material.color.a, 0.0f, Time.deltaTime * lerpVelocity * 3.5f) : 0f;
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha);
                    //Debug.Log (_lerpedAlpha); 
                    m_trueDistance = Mathf.Lerp(m_trueDistance, hitInfo.distance, Time.deltaTime * lerpVelocity * 2f);
                    m_obstacleRaycastEntered = true;
                }

                // fade Object behaviour
            }
            else if ((hitInfo.collider.gameObject.layer == LayerMask.NameToLayer(fadeLayerString)))
            {

                if (m_colliderRend != null)
                {
                    // m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, 1f);
                }

                m_colliderRend = GetRendererFromCollision(hitInfo);
                if (m_colliderRend.enabled)
                {
                    float _lerpedAlpha = (m_colliderRend.material.color.a >= 0.001f) ? Mathf.Lerp(m_colliderRend.material.color.a, 0.5f, Time.deltaTime * lerpVelocity * 3f) : 0f;
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha);
                    m_fadeRaycastEntered = true;
                }

                //terrain behaviour
            }
            else if ((hitInfo.collider.gameObject.layer == LayerMask.NameToLayer(terrainLayerString)))
            {

                if (m_terrainCollider != null)
                {
                    //add code here when exiting a terrainCollider
                }

                m_terrainCollider = hitInfo.collider.GetComponent<TerrainCollider>();
                if (m_terrainCollider.enabled)
                {
                    m_trueDistance = Mathf.Lerp(m_trueDistance, hitInfo.distance, Time.deltaTime * lerpVelocity * 2f);
                    //Debug.Log (_hit.distance);
                    gizmoPoint = hitInfo.point;
                    m_terrainRaycastEntered = true;
                }
            }
        }
    }

    private void ExitRaycastObstacles(Renderer colliderRend, TerrainCollider terrainCollider)
    {

        //BEHAVIOUR when exiting fade object
        if (m_fadeRaycastEntered && colliderRend != null)
        {

            float _lerpedAlpha = Mathf.Lerp(colliderRend.material.color.a, 1f, Time.fixedDeltaTime * lerpVelocity);
            //Debug.Log(_lerpedAlpha);
            colliderRend.material.color = new Color(colliderRend.material.color.r, colliderRend.material.color.g, colliderRend.material.color.b, _lerpedAlpha);
            //Debug.Log (m_colliderRend.material.color); 
            if (_lerpedAlpha >= 0.9f)
            {
                colliderRend.material.color = new Color(colliderRend.material.color.r, colliderRend.material.color.g, colliderRend.material.color.b, 1f);
                colliderRend = null;
                m_fadeRaycastEntered = false;
            }

        }

        //BEHAVIOUR when exiting a obstacle object
        else if (m_obstacleRaycastEntered && colliderRend != null)
        {

            float _lerpedAlpha = Mathf.Lerp(colliderRend.material.color.a, 1.0f, Time.fixedDeltaTime * lerpVelocity);
            colliderRend.material.color = new Color(colliderRend.material.color.r, colliderRend.material.color.g, colliderRend.material.color.b, _lerpedAlpha);
            if (_lerpedAlpha >= 0.9f)
            {
                colliderRend.material.color = new Color(colliderRend.material.color.r, colliderRend.material.color.g, colliderRend.material.color.b, 1f);
                colliderRend = null;
                m_obstacleRaycastEntered = false;
            }

        }

        //BEHAVIOUR when exiting terrain
        if (m_terrainRaycastEntered && terrainCollider != null)
        {
            terrainCollider = null;
            m_terrainRaycastEntered = false;
        }
    }


    public static Renderer GetRendererFromCollision(RaycastHit hit)
    {
        Renderer _colliderRend = (Renderer)hit.collider.GetComponent<MeshRenderer>();
        if (_colliderRend == null)
            _colliderRend = (Renderer)hit.collider.GetComponent<MeshRenderer>();
        if (_colliderRend == null)
            _colliderRend = (Renderer)hit.collider.GetComponent<SkinnedMeshRenderer>();
        return _colliderRend;
    }

    public static int SumLayers(LayerMask first, LayerMask second)
    {
        return first.value + second.value;
    }

    public static int SumLayers(LayerMask first, LayerMask second, LayerMask third)
    {
        return first.value + second.value + third.value;
    }


}
