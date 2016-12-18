using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThirdPersonCameraMovement : MonoBehaviour
{

    public Transform playerTransform;
    public CharacterController playerController;
    public bool useJoystick = false;

    [Header("Camera values")]
    public float maxDistanceFromCamera = 40f;
    public float minDistanceFromCamera = 10f;
    public float cameraSpeed = 2f;
    public float sensitivityX = 4.0f;
    public float sensitivityY = 1.0f;
    public float lerpVelocity = 5f;
    [Range(1, 3)]
    public float fadeDistanceFactor = 3f;
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

    private bool m_fadeRaycastEntered = false;
    private bool m_obstacleRaycastEntered = false;
    private bool m_terrainRaycastEntered = false;
    private Renderer m_colliderRend = null;
    private TerrainCollider m_terrainCollider = null;

    private List<Renderer> m_colliderRenderers = new List<Renderer>();
    private List<TerrainCollider> m_terrainColliders = new List<TerrainCollider>();

    private float m_noKeysTouchedTime = 0.0f;

    [SerializeField]
    private float m_lerpedHeight;
    [SerializeField]
    private float m_trueDistance;

#if UNITY_EDITOR

    Vector3 gizmoPoint;
    Vector3 gizmoRayDirection;
    float gizmoDistance;
    Vector3 gizmoRayOrigin;
    Vector3 gizmoDownPoint;
    Vector3 gizmoDownRayDirection;
    float gizmoDownDistance;
    Vector3 gizmoDownRayOrigin;

#endif

    private const string obstacleLayerString = "Obstacle";
    private const string fadeLayerString = "Fade";
    private const string terrainLayerString = "Terrain";



    // Use this for initialization
    void Start()
    {
        m_transform = transform;
        m_cam = Camera.main;
        m_trueDistance = maxDistanceFromCamera;

        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if (playerController == null)
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterController>();
    }



    void Update()
    {

        //GET INPUT
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

        //ray parameters
        Vector3 _rayDirection = cameraPivotTransform.position - playerTransform.position;
        Vector3 _rayOrigin = playerTransform.position;
        RaycastHit _hit;
        float _distance = maxDistanceFromCamera + (Vector3.Distance(m_transform.position, cameraPivotTransform.position));

        ////Draw Gizmo
        gizmoRayDirection = _rayDirection;
        gizmoDistance = 1f;
        gizmoRayOrigin = _rayOrigin;



        //BEHAVIOUR WITH OBSTACLES
        if (Physics.Raycast(_rayOrigin, _rayDirection, out _hit, maxDistanceFromCamera, SumLayers(obstacleMask, fadeMask, terrainMask)))
        {
            Debug.Log("entered the raycast");

            //Obstacle behaviour
            if (_hit.collider.gameObject.layer == LayerMask.NameToLayer(obstacleLayerString))
            {
                m_fadeRaycastEntered = false;
                m_terrainRaycastEntered = false;
                gizmoPoint = _hit.point;
                m_colliderRend = GetRendererFromCollision(_hit);
                //m_colliderRenderer = _hit.collider.GetComponent<MeshRenderer> (); 
                if (m_colliderRend.enabled)
                {
                    float _lerpedAlpha = (m_colliderRend.material.color.a >= 0.001f) ?
                        Mathf.Lerp(m_colliderRend.material.color.a, 0.0f, Time.deltaTime * lerpVelocity * 3.5f) : 0f;
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha);
                    //Debug.Log (_lerpedAlpha); 
                    m_trueDistance = Mathf.Lerp(m_trueDistance, _hit.distance, Time.deltaTime * lerpVelocity * 2f);
                    m_obstacleRaycastEntered = true;
                }

            }

            // fade Object behaviour
            else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer(fadeLayerString)))
            {
                m_obstacleRaycastEntered = false;
                m_terrainRaycastEntered = false;

                m_colliderRend = GetRendererFromCollision(_hit);
                if (m_colliderRend.enabled)
                {

                    if (m_colliderRend.material.HasProperty("_Color"))
                    {
                        float _lerpedAlpha = (m_colliderRend.material.color.a >= 0.001f) ? Mathf.Lerp(m_colliderRend.material.color.a, 0.5f, Time.deltaTime * lerpVelocity * 3f) : 0f;
                        m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha);
                    }

                    if (m_colliderRend.material.HasProperty("_Cloak"))
                    {
                        float _desiredDistance = maxDistanceFromCamera / fadeDistanceFactor;
                        m_colliderRend.material.SetFloat("_ChangePoint", _desiredDistance);
                        //float _lerpedDistanceFromCamera = (Mathf.Abs(m_colliderRend.material.GetFloat("_ChangePoint") - _desiredDistance) >= 0.1f) ?
                        //    Mathf.Lerp(m_colliderRend.material.GetFloat("_ChangePoint"), _desiredDistance, Time.deltaTime * lerpVelocity) : _desiredDistance;
                        //m_colliderRend.material.SetFloat("_ChangePoint", _lerpedDistanceFromCamera);

                        float _lerpedCloak = (m_colliderRend.material.GetFloat("_Cloak") > 0.01f) ?
                            Mathf.Lerp(m_colliderRend.material.GetFloat("_Cloak"), 0f, Time.deltaTime * lerpVelocity) : 0f;
                        //Debug.Log("cloak: " + _lerpedCloak);
                        m_colliderRend.material.SetFloat("_Cloak", _lerpedCloak);
                    }

                    m_fadeRaycastEntered = true;
                }
            }

            //terrain behaviour
            else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer(terrainLayerString)))
            {
                m_fadeRaycastEntered = false;
                m_obstacleRaycastEntered = false;


                if (m_terrainCollider != null)
                {
                    //add code here when exiting a terrainCollider
                }

                m_terrainCollider = _hit.collider.GetComponent<TerrainCollider>();
                if (m_terrainCollider.enabled)
                {
                    m_trueDistance = Mathf.Lerp(m_trueDistance, _hit.distance, Time.deltaTime * lerpVelocity * 2f);
                    //Debug.Log (_hit.distance);
                    //gizmoPoint = _hit.point;
                    m_terrainRaycastEntered = true;
                }
            }
            else
            {
                m_fadeRaycastEntered = false;
                m_terrainRaycastEntered = false;
                m_obstacleRaycastEntered = false;
            }
        }
        else
        {
            m_fadeRaycastEntered = false;
            m_terrainRaycastEntered = false;
            m_obstacleRaycastEntered = false;
        }



        //BEHAVIOURS EXITING COLLISIONS

        //BEHAVIOUR when exiting fade object
        if (!m_fadeRaycastEntered && m_colliderRend != null)
        {

            if (m_colliderRend.material.HasProperty("_Color"))
            {
                float _lerpedAlpha = Mathf.Lerp(m_colliderRend.material.color.a, 1f, Time.fixedDeltaTime * lerpVelocity);
                m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlpha);
                if (_lerpedAlpha >= 0.9f)
                {
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, 1f);
                    if (!m_colliderRend.material.HasProperty("_Cloak"))
                    {
                        m_colliderRend = null;
                        m_fadeRaycastEntered = false;
                    }
                }
            }

            if (m_colliderRend.material.HasProperty("_Cloak"))
            {
                float _lerpedCloak = Mathf.Lerp(m_colliderRend.material.GetFloat("_Cloak"), 1f, Time.fixedDeltaTime * lerpVelocity/2);
                m_colliderRend.material.SetFloat("_Cloak", _lerpedCloak);
                //Debug.Log("cloak: " + _lerpedCloak);

                if (_lerpedCloak >= 0.999f)
                {
                    m_colliderRend.material.SetFloat("_Cloak", 1f);
                    m_colliderRend = null;
                    m_fadeRaycastEntered = false;
                }
            }
            else
            {
                m_colliderRend = null;
                m_fadeRaycastEntered = false;
            }
        }


        //BEHAVIOUR when exiting a obstacle object
        else if (!m_obstacleRaycastEntered && m_colliderRend != null)
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
        if (!m_terrainRaycastEntered && m_terrainCollider != null)
        {
            m_terrainCollider = null;
            m_terrainRaycastEntered = false;
        }


        //OTHER BEHAVIOURS
        //BEHAVIOUR Distance from ground
        m_lerpedHeight = MappedLerp(currentY, yAngleMin - minDistanceFromCamera, yAngleMax, maxDistanceFromCamera, 0f);
        float _distancePivot = Vector3.Distance(m_transform.position, cameraPivotTransform.position);


        if (!m_terrainRaycastEntered && !m_obstacleRaycastEntered)
        {
            m_trueDistance = (Mathf.Abs(m_trueDistance - maxDistanceFromCamera) > 0.1f) ?
                Mathf.Lerp(m_trueDistance, maxDistanceFromCamera - m_lerpedHeight, Time.fixedDeltaTime * lerpVelocity) :
                maxDistanceFromCamera - m_lerpedHeight;
        }

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
        cameraPivotTransform.localRotation = Quaternion.Euler(pivotXRotation, pivotYRotation, pivotZRotation);
    }


    private void OnDrawGizmos()
    {

        if (!Application.isPlaying)
            return;
        //Vector3 gizmoRayDirection = playerTransform.position - m_transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(gizmoRayOrigin, gizmoRayDirection * gizmoDistance);
        Gizmos.DrawWireSphere(gizmoPoint, 1f);


        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(gizmoDownRayOrigin, gizmoDownRayDirection * gizmoDownDistance);
        Gizmos.DrawWireSphere(gizmoDownPoint, 1f);

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

    public static float MappedLerp(float valueToTransform, float oldMin, float oldMax, float newMin, float newMax)
    {
        float oldRange = oldMax - oldMin;
        float newRange = newMax - newMin;
        return (((valueToTransform - oldMin) * newRange) / oldRange) + newMin;
    }

}
