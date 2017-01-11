using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectGiants.GFunctions;

public class ThirdPersonCameraMovement : MonoBehaviour
{
    public enum CameraMode { Follow, CharacterRotation, Rail, Static, Cinematic };

    public CameraMode cameraMode = CameraMode.Follow;


    public Transform playerTransform;
    public CharacterMotion player;
    public bool useJoystick = false;

    [Header("Camera values")]
    public float maxDistance = 40f;
    public float minDistance = 10f;
    public float joystickSpeed = 2f;
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

    [Header("Player Fadeout")]
    public float minDistToStartFadeCharacter = 5f;
    public float distanceToFullyDisappear = 1.8f;

    [Header("Angle clamp")]
    public bool limitXAngle = false;
    public float xAngleMin = -50.0f;
    public float xAngleMax = 50.0f;
    public float yAngleMin = -20.0f;
    public float yAngleMax = 50.0f;

    [Header("Pitch when Camera Down")]
    public float yAngleMinToYawn = -20f;
    public float pitchAngle = 45f;
    public float minDistanceAtYawn = 1f;
    public bool applyMovementWithYawn = true;
    [Tooltip("0 = angleMin")]
    public float startMovementAtAngle = 0f;
    public float xYawnMovement = 0.5f;
    public float yYawnMovement = 0.5f;

    [Header("Modificiations")]
    public float xModificationAngle = 0f;
    public float yModificationAngle = 0f;
    public float waitingTimeToMoveCamera = 2f;



    [Header("Pivot local transform")]
    public Transform cameraPivotTransform;
    public Vector3 pivotPosition;
    [Header("Rotation by normal")]
    public bool rotationAxisDependsOnCameraSurface = true;
    public bool rotateCameraWithNormal = true;
    public float rotationIntensity = 1f;
    public float rotationLerp = 3f;
    public Vector3 pivotRotation;

    [Header("Static Camera Values")]
    public Transform staticTransformPosition;

    private Transform m_transform;
    private Camera m_cam;

    private bool m_fadeRaycastEntered = false;
    private bool m_obstacleRaycastEntered = false;
    private bool m_terrainRaycastEntered = false;
    private TerrainCollider m_terrainCollider = null;

    //private List<Renderer> m_colliderRenderers = new List<Renderer>();
    //private List<TerrainCollider> m_terrainColliders = new List<TerrainCollider>();

    private float m_noKeysTouchedTime = 0.0f;

    [Header("Read Values")]
    [SerializeField]
    private float m_lerpedHeight;
    [SerializeField]
    private float m_trueDistance;
    [SerializeField]
    private Renderer m_colliderRend = null;
    public List<Renderer> fadeRenderers = new List<Renderer>();
    public List<Renderer> obstacleRenderers = new List<Renderer>();
    [SerializeField]
    private Renderer[] playerRenderers;
    [SerializeField]
    float m_distanceToYawn;
    private Quaternion m_rotation;
    private Vector3 m_dir;
    private bool m_playerColorChanged = false;

    private Quaternion m_normalRotation;
    private Vector3 m_cameraForwardOnSurface;
    private Quaternion m_cameraRotationOnSurface;
    private float m_surfaceCameraAngle;
    private Quaternion m_rotationWithNormals;

#if UNITY_EDITOR

    Vector3 gizmoPoint;
    Vector3 gizmoRayDirection;
    float gizmoDistance;
    float gizmoRadius;
    Vector3 gizmoRayOrigin;
    Vector3 gizmoDownPoint;
    Vector3 gizmoDownRayDirection;
    float gizmoDownDistance;
    Vector3 gizmoDownRayOrigin;

#endif

    public const string obstacleLayerString = "Obstacle";
    public const string fadeLayerString = "Fade";
    public const string terrainLayerString = "Terrain";



    // Use this for initialization
    void Start()
    {
        m_transform = transform;
        m_cam = Camera.main;
        m_trueDistance = maxDistance;
        m_rotationWithNormals = m_rotation;

        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if (player == null)
            player = playerTransform != null ? playerTransform.GetComponent<CharacterMotion>() : GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterMotion>();

        if (player != null)
        {
            playerRenderers = player.GetComponentsInChildren<Renderer>();
        }

        if (!player || !playerTransform || playerRenderers.Length <= 0)
        {
            Debug.LogError("error assigning references for camera");
        }


        if (applyMovementWithYawn && startMovementAtAngle == 0) { startMovementAtAngle = yAngleMin; }
    }

    void Update()
    {

        UpdateInput();


        //CAMERA BEHAVIOURS

        //BEHAVIOUR Block Axis
        BlockXAndYAxis();

        //BEHAVIOUR Distance from ground
        FreeCameraUpdate();

        //BEHAVIOUR Surface Normal
        if (rotationAxisDependsOnCameraSurface) SurfaceNormalBehaviour();

        //BEHAVIOUR Rotation by normal
        if (cameraMode == CameraMode.CharacterRotation || cameraMode == CameraMode.Follow)
            if (rotateCameraWithNormal) CameraRotationByNormal(ref pivotRotation, rotationIntensity, rotationLerp);

        //BEHAVIOUR Obstacles raycasting
        ObstacleBehaviours();

        //BEHAVIOUR Player too close to the camera
        PlayerFadeOutWhenTooClose();

    }


    void LateUpdate()
    {

        switch (cameraMode)
        {
            case CameraMode.Follow:
                {
                    //APPLY MOVEMENT
                    m_dir = new Vector3(0, 0, -m_trueDistance);
                    m_rotation = Quaternion.Euler(Mathf.Max(currentY, yAngleMin) + yModificationAngle, currentX + xModificationAngle, 0f);
                    Vector3 _rotDirection = RotateCameraWithSurfaceAxis(ref m_rotationWithNormals);

                    m_transform.position = playerTransform.position + _rotDirection;
                    m_transform.LookAt(playerTransform.position);

                    cameraPivotTransform.localPosition = pivotPosition;
                    cameraPivotTransform.localRotation = Quaternion.Euler(pivotRotation);
                    break;
                }
            case CameraMode.CharacterRotation:
                {
                    //APPLY MOVEMENT
                    m_dir = new Vector3(0, 0, -m_trueDistance);
                    m_rotation = Quaternion.Euler(Mathf.Max(currentY, yAngleMin) + yModificationAngle, currentX + xModificationAngle, 0f);
                    Vector3 _rotDirection = RotateCameraWithSurfaceAxis(ref m_rotationWithNormals);

                    m_transform.position = playerTransform.position + _rotDirection;
                    m_transform.LookAt(playerTransform.position);

                    cameraPivotTransform.localPosition = pivotPosition;
                    cameraPivotTransform.localRotation = Quaternion.Euler(pivotRotation);
                    break;
                }

            case CameraMode.Static:
                {
                    //do a lerp also here. 

                    if (staticTransformPosition != null) m_transform.position = staticTransformPosition.position;
                    m_transform.LookAt(playerTransform.position);
                    cameraPivotTransform.localPosition = pivotPosition;
                    cameraPivotTransform.localRotation = Quaternion.Euler(pivotRotation);
                    break;
                }
        }

    }

    private Vector3 RotateCameraWithSurfaceAxis(ref Quaternion rotationWithNormals)
    {
        if (rotationAxisDependsOnCameraSurface)
        {
            rotationWithNormals = Quaternion.Slerp(rotationWithNormals, m_normalRotation * m_rotation, Time.deltaTime * 5f);
            //Debug.Log("rotation: " + m_rotation); 
            //Vector3 _rotDirection = m_rotation * m_dir;
        }
        Vector3 _rotDirection = (rotationAxisDependsOnCameraSurface) ? rotationWithNormals * m_dir : m_rotation * m_dir;
        return _rotDirection;
    }

    private void UpdateInput()
    {
        if (useJoystick)
        {
            float inputXStick = (Input.GetAxis("360_R_Stick_X") > 0.35f || Input.GetAxis("360_R_Stick_X") < -0.35f) ? Input.GetAxis("360_R_Stick_X") : 0f;
            float inputYStick = (Input.GetAxis("360_R_Stick_Y") > 0.35f || Input.GetAxis("360_R_Stick_Y") < -0.35f) ? Input.GetAxis("360_R_Stick_Y") : 0f;

            //Debug.LogFormat("Bool: {0}, stick: {1}", (Input.GetAxis("360_R_Stick_X") > 0.2f || Input.GetAxis("360_R_Stick_X") < -0.2f), inputXStick); 

            currentX += inputXStick * joystickSpeed;
            currentY += inputYStick * joystickSpeed;

        }
        else
        {
            currentX += Input.GetAxis("Mouse X");
            currentY += Input.GetAxis("Mouse Y");
        }
    }

    private void FreeCameraUpdate()
    {
        m_lerpedHeight = MappedLerp(Mathf.Max(currentY, yAngleMin), yAngleMin - minDistance, yAngleMax, maxDistance, 0f);

        m_distanceToYawn = (currentY > yAngleMin) ? 0f : MappedLerp(currentY, yAngleMin, yAngleMinToYawn, m_lerpedHeight, m_lerpedHeight + minDistanceAtYawn);


        //yawn behaviour when at min distance
        pivotRotation.x = (currentY > yAngleMin) ? 0f : Mathf.Lerp(pivotRotation.x, -MappedLerp(currentY, yAngleMin, yAngleMinToYawn, 0f, pitchAngle), lerpVelocity);
        if (applyMovementWithYawn)
        {
            pivotPosition.x = (currentY > startMovementAtAngle) ? 0f : Mathf.Lerp(pivotPosition.x, -MappedLerp(currentY, startMovementAtAngle, yAngleMinToYawn, 0f, xYawnMovement), lerpVelocity);
            pivotPosition.y = (currentY > startMovementAtAngle) ? 0f : Mathf.Lerp(pivotPosition.y, -MappedLerp(currentY, startMovementAtAngle, yAngleMinToYawn, 0f, -yYawnMovement), lerpVelocity);
        }

        //BEHAVIOUR Apply distance when no obstacle and with distance from ground
        //check this out for possible errors!
        if (!m_terrainRaycastEntered && !m_obstacleRaycastEntered)
        {

            float _substraction = (currentY > yAngleMin) ? m_lerpedHeight : m_distanceToYawn;

            m_trueDistance = (Mathf.Abs(m_trueDistance - maxDistance) > 0.1f) ?
                Mathf.Lerp(m_trueDistance, maxDistance - _substraction, Time.fixedDeltaTime * lerpVelocity) :
                maxDistance - _substraction;
        }
    }

    private void BlockXAndYAxis()
    {
        if (Mathf.Abs(currentX) >= 360)
        {
            currentX = 0f;
        }
        if (Mathf.Abs(currentY) >= 360)
        {
            currentX = 0f;
        }


        currentY = Mathf.Clamp(currentY, yAngleMinToYawn, yAngleMax);
        if (limitXAngle)
            currentX = Mathf.Clamp(currentX, xAngleMin, xAngleMax);
    }

    private void ObstacleBehaviours()
    {
        #region RAYCAST PARAMETERS
        Vector3 _rayDirection = cameraPivotTransform.position - playerTransform.position;
        Vector3 _rayOrigin = playerTransform.position;
        RaycastHit _hit;
        float _distance = maxDistance + (Vector3.Distance(m_transform.position, cameraPivotTransform.position));
        float _plusObstacleRaycastDistance = 0.1f;
        float _raycastDistance = maxDistance - m_lerpedHeight + _plusObstacleRaycastDistance;

        //Draw Gizmo
#if UNITY_EDITOR
        gizmoRayDirection = _rayDirection;
        gizmoDistance = 1f;
        gizmoRayOrigin = _rayOrigin;
#endif
        #endregion

        //BEHAVIOUR WITH OBSTACLES
        if (Physics.Raycast(_rayOrigin, _rayDirection, out _hit, _raycastDistance, SumLayers(obstacleMask, fadeMask, terrainMask)))
        {
            if (_hit.collider.gameObject.layer == LayerMask.NameToLayer(obstacleLayerString))
            {
                #region OBSTACLE ENTER
                m_fadeRaycastEntered = false;
                m_terrainRaycastEntered = false;
                //gizmoPoint = _hit.point;
                m_colliderRend = GetRendererFromCollision(_hit);
                if (m_colliderRend.enabled)
                {
                    if (!obstacleRenderers.Contains(m_colliderRend)) obstacleRenderers.Add(m_colliderRend);
                    for (int i = 0; i < obstacleRenderers.Count; i++)
                    {
                        if (obstacleRenderers[i].enabled && obstacleRenderers[i] != m_colliderRend)
                        {
                            float _lerpedAlpha = (obstacleRenderers[i].material.color.a < 0.9f) ?
                            Mathf.Lerp(obstacleRenderers[i].material.color.a, 1f, Time.deltaTime * lerpVelocity * 3f) : 1f;
                            obstacleRenderers[i].material.color = new Color(obstacleRenderers[i].material.color.r, obstacleRenderers[i].material.color.g, obstacleRenderers[i].material.color.b, _lerpedAlpha);
                            if (_lerpedAlpha >= 0.95)
                            {
                                obstacleRenderers.Remove(obstacleRenderers[i]);
                            }
                        }
                    }
                    float _lerpedAlphaB = (m_colliderRend.material.color.a >= 0.001f) ?
                    Mathf.Lerp(m_colliderRend.material.color.a, 0.0f, Time.deltaTime * lerpVelocity * 3f) : 0f;
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlphaB);
                    //Debug.Log (_lerpedAlpha); 
                    m_trueDistance = Mathf.Lerp(m_trueDistance, _hit.distance, Time.deltaTime * lerpVelocity * 2f);
                    if (Mathf.Abs(m_trueDistance - _hit.distance) <= 0.1f)
                    {
                        m_trueDistance = _hit.distance;
                    }
                    m_obstacleRaycastEntered = true;
                }
                #endregion
            }
            else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer(fadeLayerString)))
            {
                #region FADE ENTER
                m_obstacleRaycastEntered = false;
                m_terrainRaycastEntered = false;
                float _radius = 15f;
#if UNITY_EDITOR
                gizmoRadius = _radius;
                gizmoPoint = _hit.point;
#endif
                m_colliderRend = GetRendererFromCollision(_hit);

                //ADD LAYER MASK DONNO WHY IT IS NOT WORKING
                //LATER ON CHANGE SPHERE COLLIDER FOR CAPSULE COLLIDER TO AVOID MAKING DISAPEAR OBJECTS FAR
                RaycastHit[] _sphereHits = Physics.SphereCastAll(_hit.point, _radius, Vector3.one, Mathf.Infinity);

                for (int i = 0; i < _sphereHits.Length; i++)
                {

                    if (_sphereHits[i].collider.gameObject.layer == LayerMask.NameToLayer(fadeLayerString))
                    {
                        Renderer _rend = _sphereHits[i].collider.GetComponent<Renderer>();
                        if (!fadeRenderers.Contains(_rend) && _rend != null)
                        {
                            fadeRenderers.Add(_rend);
                        }
                    }
                }


                for (int i = 0; i < fadeRenderers.Count; i++)
                {
                    if (fadeRenderers[i].enabled)
                    {

                        if (fadeRenderers[i].material.HasProperty("_Color"))
                        {
                            float _lerpedAlpha = (fadeRenderers[i].material.color.a >= 0.001f) ? Mathf.Lerp(fadeRenderers[i].material.color.a, 0.5f, Time.deltaTime * lerpVelocity * 3f) : 0f;
                            fadeRenderers[i].material.color = new Color(fadeRenderers[i].material.color.r, fadeRenderers[i].material.color.g, fadeRenderers[i].material.color.b, _lerpedAlpha);
                        }

                        if (fadeRenderers[i].material.HasProperty("_Cloak"))
                        {
                            float _desiredDistance = maxDistance / fadeDistanceFactor;
                            fadeRenderers[i].material.SetFloat("_ChangePoint", _desiredDistance);
                            float _lerpedCloak = (fadeRenderers[i].material.GetFloat("_Cloak") > 0.01f) ?
                                Mathf.Lerp(fadeRenderers[i].material.GetFloat("_Cloak"), 0f, Time.deltaTime * lerpVelocity) : 0f;
                            //Debug.Log("cloak: " + _lerpedCloak);
                            fadeRenderers[i].material.SetFloat("_Cloak", _lerpedCloak);
                        }

                    }
                }

                m_fadeRaycastEntered = true;
                #endregion
            }
            else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer(terrainLayerString)))
            {
                #region TERRAIN ENTER
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
                #endregion
            }
            else
            {
                #region OTHER COLLISIONS
                m_colliderRend = null;
                #endregion
            }
        }
        else
        {
            #region NO COLLISIONS
            m_colliderRend = null;

            #endregion
        }



        //BEHAVIOUR when exiting a obstacle object
        if (m_obstacleRaycastEntered && m_colliderRend == null)
        {
            #region OBSTACLE EXIT
            //Debug.Log("entered exit obstacle");
            for (int i = 0; i < obstacleRenderers.Count; i++)
            {
                if (obstacleRenderers[i].enabled)
                {
                    float _lerpedAlpha = (obstacleRenderers[i].material.color.a < 0.9f) ?
                    Mathf.Lerp(obstacleRenderers[i].material.color.a, 1f, Time.deltaTime * lerpVelocity * 3f) : 1f;
                    obstacleRenderers[i].material.color = new Color(obstacleRenderers[i].material.color.r, obstacleRenderers[i].material.color.g, obstacleRenderers[i].material.color.b, _lerpedAlpha);
                    if (_lerpedAlpha >= 0.95)
                    {
                        obstacleRenderers[i].material.color = new Color(obstacleRenderers[i].material.color.r, obstacleRenderers[i].material.color.g, obstacleRenderers[i].material.color.b, 1f);
                        obstacleRenderers.Remove(obstacleRenderers[i]);
                    }
                }
            }
            if (obstacleRenderers.Count <= 0)
            {
                m_obstacleRaycastEntered = false;
                //m_colliderRend = null;
            }

            #endregion
        }

        if (m_fadeRaycastEntered && m_colliderRend == null)
        {
            #region FADE EXIT
            //Debug.Log("entered exit fade");

            for (int i = 0; i < fadeRenderers.Count; i++)
            {

                Renderer _fadeRenderer = fadeRenderers[i];
                if (_fadeRenderer.material.HasProperty("_Color"))
                {
                    float _lerpedAlpha = Mathf.Lerp(_fadeRenderer.material.color.a, 1f, Time.fixedDeltaTime * lerpVelocity);
                    _fadeRenderer.material.color = new Color(_fadeRenderer.material.color.r, _fadeRenderer.material.color.g, _fadeRenderer.material.color.b, _lerpedAlpha);
                    if (_lerpedAlpha >= 0.9f)
                    {
                        _fadeRenderer.material.color = new Color(_fadeRenderer.material.color.r, _fadeRenderer.material.color.g, _fadeRenderer.material.color.b, 1f);
                        if (!_fadeRenderer.material.HasProperty("_Cloak"))
                        {
                            fadeRenderers.Remove(_fadeRenderer);
                        }
                    }
                }

                if (_fadeRenderer.material.HasProperty("_Cloak"))
                {
                    float _lerpedCloak = Mathf.Lerp(_fadeRenderer.material.GetFloat("_Cloak"), 1f, Time.fixedDeltaTime * lerpVelocity / 10);
                    _fadeRenderer.material.SetFloat("_Cloak", _lerpedCloak);
                    //Debug.Log("cloak: " + _lerpedCloak);
                    if (_lerpedCloak >= 0.75f)
                    {
                        _fadeRenderer.material.SetFloat("_Cloak", 1.8f);
                        fadeRenderers.Remove(_fadeRenderer);
                    }
                }
            }

            if (fadeRenderers.Count <= 0)
            {
                m_fadeRaycastEntered = false;
            }
            #endregion
        }

        //BEHAVIOUR when exiting terrain
        if (!m_terrainRaycastEntered && m_terrainCollider != null)
        {
            #region TERRAIN EXIT
            //m_terrainCollider = null;
            m_terrainRaycastEntered = false;
            #endregion
        }
    }

    private void PlayerFadeOutWhenTooClose()
    {
        //BEHAVIOUR Player too close to the camera
        float _distanceToPlayer = Vector3.Distance(playerTransform.position, m_cam.transform.position);
        if (_distanceToPlayer < minDistToStartFadeCharacter)
        {
            #region PLAYER FADE OUT
            float _mappedFadePlayer = GFunctions.NormalizedRangeValue(_distanceToPlayer, distanceToFullyDisappear, minDistToStartFadeCharacter);
            foreach (Renderer renderer in playerRenderers)
            {
                if (renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, _mappedFadePlayer);
                }
            }
            m_playerColorChanged = true;
        }
        else if (m_playerColorChanged)
        {
            foreach (Renderer renderer in playerRenderers)
            {
                if (renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, 1f);
                }
            }
            m_playerColorChanged = false;
            #endregion
        }
    }

    private void SurfaceNormalBehaviour()
    {
        RaycastHit _info;
        //Vector3 _surfaceNormal = UpdateSurfaceNormalByRaycast(out _info, player.CharacterUp, 100f);
        Vector3 _surfaceNormal = UpdateSurfaceNormalByRaycast(out _info, Vector3.up, maxDistance);
        //Vector3 _surfaceNormal = player.SurfaceNormal; 


        m_normalRotation = GetRotationByNormal2(transform.rotation, _surfaceNormal);
        m_cameraRotationOnSurface = m_normalRotation * transform.rotation;
    }

    //repeated from character so that is not so good...
    private float GetForwardAngleFromGroundZero(Vector3 forward)
    {
        Vector3 vectorOnFacePlane = Vector3.ProjectOnPlane(forward, Vector3.up);
        float absAngle = Vector3.Angle(forward, vectorOnFacePlane);
        float dot = Vector3.Dot(Vector3.up, forward);
        return dot < 0 ? -absAngle : absAngle;
    }

    private Quaternion GetRotationByNormal2(Quaternion rotation, Vector3 normal)
    {

        Quaternion _normalRot = rotation;
        _normalRot = Quaternion.FromToRotation(Vector3.up, normal);

        return _normalRot;
        //return Quaternion.FromToRotation(Vector3.up, normal) * transform.rotation;
    }

    private Vector3 UpdateSurfaceNormalByRaycast(out RaycastHit hitInfo, Vector3 upVector, float distance)
    {

        if (GetRaycastAtPosition(out hitInfo, upVector, distance))
        {
            return hitInfo.normal;
        }
        return Vector3.up;
    }

    private bool GetRaycastAtPosition(out RaycastHit hitInfo, Vector3 upVector, float distance)
    {
        Ray ray = new Ray(transform.position + (-upVector), -upVector);
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
        if (Physics.Raycast(ray, out hitInfo, distance))
        {
            return true;
        }

        return false;
    }
    //...until here


    private void OnDrawGizmos()
    {
#if UNITY_EDITOR

        if (!Application.isPlaying)
            return;
        //Vector3 gizmoRayDirection = playerTransform.position - m_transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(gizmoRayOrigin, gizmoRayDirection * gizmoDistance);
        Gizmos.DrawWireSphere(gizmoPoint, gizmoRadius);


        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(gizmoDownRayOrigin, gizmoDownRayDirection * gizmoDownDistance);
        Gizmos.DrawWireSphere(gizmoDownPoint, 1f);

#endif
    }

    private void CameraRotationByNormal(ref Vector3 rotation, float intensity, float lerpFactor)
    {
        if (player == null)
            return;

        if (player.SurfaceNormal != null)
        {


            Vector3 norm = player.SurfaceNormal;
            if (norm == Vector3.zero)
                return;

            Vector3 cameraPosRelativeToPlayer = (cameraPivotTransform.position - player.transform.position).normalized;
            Vector3 vectorOnFacePlane = Vector3.Cross(cameraPosRelativeToPlayer, Vector3.up);


            Vector3 relNorm = Vector3.ProjectOnPlane(norm, Vector3.Cross(Vector3.up, vectorOnFacePlane));

            float desiredRotation = Vector3.Angle(relNorm, Vector3.up);

            float dot = Vector3.Dot(vectorOnFacePlane, relNorm);

            if (dot > 0) desiredRotation = -desiredRotation;


            rotation.z = Mathf.Lerp(rotation.z, desiredRotation * intensity, Time.deltaTime * lerpFactor);



            //rotation = Vector3.Lerp(rotation, desiredRotation, Time.deltaTime * lerpFactor);
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
