using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThirdPersonCameraMovement : MonoBehaviour
{

    public Transform playerTransform;
    public CharacterV3 player;
    public bool useJoystick = false;

    [Header("Camera values")]
    public float maxDistance = 40f;
    public float minDistance = 10f;
    //public float minDistanceFromCamera = 5f; 
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
    public Vector3 pivotPosition;
    public bool rotationByNormal = true;
    public float rotationIntensity = 1f;
    public float rotationLerp = 3f; 
    public Vector3 pivotRotation;


    private Transform m_transform;
    private Camera m_cam;

    private bool m_fadeRaycastEntered = false;
    private bool m_obstacleRaycastEntered = false;
    private bool m_terrainRaycastEntered = false;
    private Renderer m_colliderRend = null;
    private TerrainCollider m_terrainCollider = null;

    //private List<Renderer> m_colliderRenderers = new List<Renderer>();
    //private List<TerrainCollider> m_terrainColliders = new List<TerrainCollider>();

    private float m_noKeysTouchedTime = 0.0f;

    [Header("Read Values")]
    [SerializeField]
    private float m_lerpedHeight;
    [SerializeField]
    private float m_trueDistance;
    private Quaternion m_rotation;
    private Vector3 m_dir;


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
        m_trueDistance = maxDistance;

        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterV3>();
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
        float _distance = maxDistance + (Vector3.Distance(m_transform.position, cameraPivotTransform.position));

        ////Draw Gizmo
        gizmoRayDirection = _rayDirection;
        gizmoDistance = 1f;
        gizmoRayOrigin = _rayOrigin;



        //BEHAVIOUR WITH OBSTACLES
        if (Physics.Raycast(_rayOrigin, _rayDirection, out _hit, maxDistance, SumLayers(obstacleMask, fadeMask, terrainMask)))
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
                        float _desiredDistance = maxDistance / fadeDistanceFactor;
                        m_colliderRend.material.SetFloat("_ChangePoint", _desiredDistance);
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
                float _lerpedCloak = Mathf.Lerp(m_colliderRend.material.GetFloat("_Cloak"), 1f, Time.fixedDeltaTime * lerpVelocity / 2);
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
        m_lerpedHeight = MappedLerp(currentY, yAngleMin - minDistance, yAngleMax, maxDistance, 0f);

        if (rotationByNormal) CameraRotationByNormal(ref pivotRotation, rotationIntensity, rotationLerp);

        if (!m_terrainRaycastEntered && !m_obstacleRaycastEntered)
        {
            m_trueDistance = (Mathf.Abs(m_trueDistance - maxDistance) > 0.1f) ?
                Mathf.Lerp(m_trueDistance, maxDistance - m_lerpedHeight, Time.fixedDeltaTime * lerpVelocity) :
                maxDistance - m_lerpedHeight;
        }

    }


    void LateUpdate()
    {
        m_dir = new Vector3(0, 0, -m_trueDistance);

        m_rotation = Quaternion.Euler(currentY + yModificationAngle, currentX + xModificationAngle, 0f);
        //Debug.Log("rotation: " + m_rotation); 
        m_transform.position = playerTransform.position + m_rotation * m_dir;

        m_transform.LookAt(playerTransform.position);

        cameraPivotTransform.localPosition = pivotPosition;
        cameraPivotTransform.localRotation = Quaternion.Euler(pivotRotation);
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

    private void CameraRotationByNormal(ref Vector3 rotation, float xIntensity, float yIntensity, float zIntensity, float lerpFactor)
    {
        if (player == null)
            return;

        if (player.surfaceNormal != null)
        {
            //Vector3 desiredRotation = new Vector3(player.surfaceNormal.x * xIntensity * m_rotation.x, player.surfaceNormal.y * yIntensity * m_rotation.y, player.surfaceNormal.z * zIntensity * m_rotation.z);
            Vector3 desiredRotation = new Vector3((player.surfaceNormal.x - m_rotation.x) * xIntensity, (player.surfaceNormal.y - m_rotation.y) * yIntensity, (player.surfaceNormal.z - m_rotation.z) * zIntensity);

            //Debug.Log("Normal: " + player.surfaceNormal.ToString());
            //Debug.Log("Desired: " + desiredRotation.ToString());

            rotation = Vector3.Lerp(rotation, desiredRotation, Time.deltaTime * lerpFactor);
        }
    }

    private void CameraRotationByNormal(ref Vector3 rotation, float intensity, float lerpFactor)
    {
        if (player == null)
            return;

        if (player.surfaceNormal != null)
        {
            

            Vector3 norm = player.surfaceNormal;
            if (norm == Vector3.zero)
                return; 

            Vector3 cameraPosRelativeToPlayer = (cameraPivotTransform.position - player.transform.position).normalized;
            Vector3 vectorOnFacePlane = Vector3.Cross(cameraPosRelativeToPlayer, Vector3.up);

            Vector3 relNorm = Vector3.ProjectOnPlane(norm, Vector3.Cross(Vector3.up, vectorOnFacePlane));

            float dot = Vector3.Dot(vectorOnFacePlane, relNorm);

            float desiredRotation = Vector3.Angle(relNorm, Vector3.up);

            if (dot > 0) desiredRotation = -desiredRotation;


            rotation.z = Mathf.Lerp(rotation.z, desiredRotation * intensity, Time.deltaTime * lerpFactor);



            //rotation = Vector3.Lerp(rotation, desiredRotation, Time.deltaTime * lerpFactor);
        }
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
