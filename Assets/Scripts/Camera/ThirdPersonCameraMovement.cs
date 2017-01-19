using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectGiants.GFunctions;

public class ThirdPersonCameraMovement : MonoBehaviour
{
    public enum CameraMode { Follow, Orbit, Target, Static, Rail, Cinematic };

    public CameraMode cameraMode = CameraMode.Follow;

    public float orbitWantedDistance = 30f;

    public Transform playerTransform;
    public CharacterMotion characterMotion;
    public Camera cam;
    public bool useJoystick = false;

    [Header("Camera values")]
    public Vector2 minDistancePosition;
    public Vector2 maxDistancePosition;
    public AnimationCurve heightPositionByDistance = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
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
    public bool rotateCameraWithNormal = true;
    public float rotationIntensity = 1f;
    public float rotationLerp = 3f;
    [SerializeField]
    private Vector2 cameraLocalRotation;
    private Vector3 pivotRotation;
    //[Header("dont activate for the moment, some issues to solve")]
    public bool AxisEqualsSurfaceAngle = true;

    [Header("Static Camera Values")]
    public Transform staticTransformPosition;
    public Transform staticTransformLookAt;
    public enum LookAtType { player, forward, customTransform };
    public LookAtType staticLookAtType = LookAtType.forward;
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
    [SerializeField]
    private float m_sphereCastRadiusForFadingObjects = 15f;
    private Quaternion m_rotation;
    private Vector3 m_dir;
    private bool m_playerColorChanged = false;

    private Vector3 m_velocityCamSmooth;
    [SerializeField]
    private float m_camSmoothDampTime = 0.1f;

    private Quaternion m_normalRotation;
    private Vector3 m_cameraForwardOnSurface;
    private Quaternion m_cameraRotationOnSurface;
    private float m_surfaceCameraAngle;
    private Quaternion m_rotationWithNormals;

    [SerializeField]
    private float m_distanceUp;
    [SerializeField]
    private float m_distanceAway;
    [SerializeField]
    private float m_startingAngleFromPlayer = 0f;
    [SerializeField]
    Vector3 targetPosition = Vector3.zero;
    [SerializeField]
    Vector3 lookAtPosition = new Vector3();



    Vector3 gizmoPoint;
    Vector3 gizmoRayDirection;
    float gizmoDistance;
    float gizmoRadius;
    Vector3 gizmoRayOrigin;
    Vector3 gizmoDownPoint;
    Vector3 gizmoDownRayDirection;
    float gizmoDownDistance;
    Vector3 gizmoDownRayOrigin;


    private float m_tLerpDistance = 0f;
    private float m_currentXDistance = 0f;
    [Range(0, 1)]
    public float startingDistance = 0.5f;
    [SerializeField]
    private float m_currentXDis;
    [SerializeField]
    private float m_currentYDis;
    [SerializeField]
    private float lookDirFactorRotation = 1;
    [SerializeField]
    private float lookDirDampTime = 0.5f;
    [SerializeField]
    private float movementLookDirectionThreshold = 5f;

    private Vector3 characterUp;
    private Vector3 characterForward;
    private Vector3 lookDir;

    [SerializeField]
    private float m_targetXDistance = 10f;
    [SerializeField]
    private float m_targetYDistance = 3f;
    [SerializeField]
    private Vector3 posRelativeToPlayer;
    [SerializeField]
    private Vector3 positionBeforeGliding;
    [SerializeField]
    private float followStartThreshold = 0.1f;
    [SerializeField]
    private bool blockCameraMode = false;
    [SerializeField]
    private Vector3 m_rotDirection;
    [SerializeField]
    private Vector3 curLookDir;

    private float hitLerpPos;
    private Vector3 collisionPoint;

    float hitXDistance = 1000f;
    float hitYDistance = 1000f;

    private Vector3 m_velocityCollisionCamSmooth;
    [SerializeField]
    private float m_camSmoothCollisionDampTime = 0.2f;
    [SerializeField]
    private bool m_smoothCollision = true;



    private const float minY = 0f;
    private const float maxY = 180f;
    public const string obstacleLayerString = "Obstacle";
    public const string fadeLayerString = "Fade";
    public const string terrainLayerString = "Terrain";
    private const float TARGETING_THRESHOLD = 0.3f;


    // Use this for initialization
    void Start()
    {
        m_transform = transform;
        m_cam = Camera.main;
        //m_trueDistance = startingDistance;
        m_rotationWithNormals = m_rotation;
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if (characterMotion == null)
            characterMotion = playerTransform != null ? playerTransform.GetComponent<CharacterMotion>() : GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterMotion>();

        if (characterMotion != null)
        {
            playerRenderers = characterMotion.GetComponentsInChildren<Renderer>();
        }

        if (!characterMotion || !playerTransform || playerRenderers.Length <= 0)
        {
            Debug.LogError("error assigning references for camera");
        }


        if (applyMovementWithYawn && startMovementAtAngle == 0) { startMovementAtAngle = yAngleMin; }
        lookAtPosition = playerTransform.position;
        currentX = m_startingAngleFromPlayer;
        currentY = orbitWantedDistance;
        positionBeforeGliding = transform.position;
        //if (!blockCameraMode)
        //    cameraMode = CameraMode.Orbit;
        hitLerpPos = -1f;
        collisionPoint = Vector3.one * 100f;
    }


    void LateUpdate()
    {
        //BEHAVIOUR Obstacles raycasting
        ObstacleBehaviours(ref hitXDistance, ref hitYDistance);

        //BEHAVIOUR Block Axis
        BlockXAndYAxis();

        //CAMERA BEHAVIOURS
        UpdateInput();

        //BEHAVIOUR Surface Normal
        //add a bool to change only when surface changes. 
        if (AxisEqualsSurfaceAngle) SurfaceNormalBehaviour();

        //BEHAVIOUR Rotation by normal
        if (cameraMode == CameraMode.Orbit || cameraMode == CameraMode.Follow)
            if (rotateCameraWithNormal) CameraRotationByNormal(ref pivotRotation, rotationIntensity, rotationLerp);


        //BEHAVIOUR Player too close to the camera
        PlayerFadeOutWhenTooClose();

        float rightX = Mathf.Abs(Input.GetAxis("360_R_Stick_X")) < 0.4f ? 0f : Input.GetAxis("360_R_Stick_X");
        float rightY = Mathf.Abs(Input.GetAxis("360_R_Stick_Y")) < 0.4f ? 0f : Input.GetAxis("360_R_Stick_Y");


        #region Assign Camera Mode
        if (!blockCameraMode)
        {

            if ((cameraMode != CameraMode.Static && cameraMode != CameraMode.Rail))
            {
                if (Input.GetAxis("Target") > TARGETING_THRESHOLD)
                {
                    if (cameraMode != CameraMode.Target)
                    {
                        cameraMode = CameraMode.Target;
                    }
                }
                else
                {
                    //follow camera case
                    if ((Mathf.Abs(rightX) > followStartThreshold)) // || (Mathf.Abs(rightY) > followStartThreshold)) // && System.Math.Round(characterMotion.Speed, 2) == 0)
                    {
                        cameraMode = CameraMode.Follow;
                    }

                    //orbit case
                    if ((cameraMode == CameraMode.Target && Input.GetAxis("Target") <= TARGETING_THRESHOLD))
                    {
                        if (cameraMode != CameraMode.Orbit)
                        {
                            cameraMode = CameraMode.Orbit;
                            // characterMotion.characterMovementType = CharacterMotion.CharacterMovementType.Relative;
                        }
                    }
                }
            }
        }
        #endregion



        Vector3 _offset = new Vector3(0f, m_currentYDis, 0f);
        Vector3 characterOffset = playerTransform.position + _offset;
        lookAtPosition = playerTransform.position;
        //Debug.LogFormat("rot: {0}, forw: {1}", m_rotDirection, characterMotion.Forward);
        float zCamRotation = 0f;
        //  Debug.Log(targetPosition);

        switch (cameraMode)
        {

            #region Follow
            case CameraMode.Follow:
                {
                    //APPLY MOVEMENT
                    m_rotation = Quaternion.Euler(0f, currentX, 0f);
                    m_rotDirection = RotateCameraWithSurfaceAxis(ref m_rotationWithNormals, m_rotation, m_normalRotation, -Vector3.forward);
                    //check later the characterMotion.Up if its the best value. or better to use vector3.up 

                    //behaviour when gliding. 
                    if (characterMotion.characterState == CharacterMotion.CharacterState.Gliding
                        || (characterMotion.characterState == CharacterMotion.CharacterState.StrongGliding)
                        || (characterMotion.characterState == CharacterMotion.CharacterState.GoingDown))
                    {
                        //targetPosition = playerTransform.position + Vector3.up * m_currentYDis + playerTransform.forward * m_currentXDis;
                        //targetPosition = playerTransform.position + Vector3.up * m_currentYDis - characterMotion.SurfaceTang * m_currentXDis;
                        //targetPosition = playerTransform.position + Vector3.up * m_currentYDis - posRelativeToPlayer.normalized * m_currentXDis;
                        //targetPosition = playerTransform.position + Vector3.up * m_currentYDis + _rotDirection * m_currentXDis;
                        //float _distanceFromBeforeGliding = Vector3.Distance(transform.position, positionBeforeGliding); 
                        //temporary, find other solution
                        //should be the angle to look. 
                        //currentX = GetAngleInDegFromVectors(transform.forward, -Vector3.forward);
                        ///targetPosition = transform.position;

                        //prova a que sigui igual a la posicio del player  - vector3.forward * distancia
                        targetPosition = playerTransform.position + Vector3.up * m_currentYDis - m_rotDirection * m_currentXDis;

                    }
                    else
                    {
                        //targetPosition = playerTransform.position + Vector3.up * m_currentYDis - characterMotion.Forward * m_currentXDis;


                        targetPosition = playerTransform.position + Vector3.up * m_currentYDis - m_rotDirection * m_currentXDis;

                    }

                    cameraLocalRotation = Vector3.zero;
                    characterForward = characterMotion.Forward;
                    characterUp = characterMotion.Up;
                    break;
                }
            #endregion

            #region Orbit
            case CameraMode.Orbit:
                {

                    if (characterMotion.Speed > movementLookDirectionThreshold)
                    {
                        //all this does that the character tends to look to the side we are facing. 
                        lookDir = Vector3.Lerp(characterMotion.Right * (rightX < 0 ? 1f : -1f) * lookDirFactorRotation, characterMotion.Forward * (rightY < 0 ? -1f : 1f) * lookDirFactorRotation,
                            Mathf.Abs(Vector3.Dot(this.transform.forward, characterMotion.Forward)));
                        curLookDir = Vector3.Normalize(characterOffset - this.transform.position);
                        curLookDir.y = 0f;
                        curLookDir = Vector3.SmoothDamp(curLookDir, lookDir, ref m_velocityCamSmooth, lookDirDampTime);

                    }
                    if (characterMotion.characterState == CharacterMotion.CharacterState.Gliding
                    || (characterMotion.characterState == CharacterMotion.CharacterState.StrongGliding)
                    || (characterMotion.characterState == CharacterMotion.CharacterState.GoingDown))
                    {

                        lookDir = Vector3.Lerp(characterMotion.Right * (rightX < 0 ? 1f : -1f) * lookDirFactorRotation, characterMotion.SurfaceTang * (rightY < 0 ? -1f : 1f) * lookDirFactorRotation,
                                  Mathf.Abs(Vector3.Dot(this.transform.forward, characterMotion.SurfaceTang)));
                        curLookDir = Vector3.Normalize(characterOffset - this.transform.position);
                        curLookDir.y = 0f;
                        curLookDir = Vector3.SmoothDamp(curLookDir, lookDir, ref m_velocityCamSmooth, lookDirDampTime);
                        curLookDir.Normalize();
                        //change currentLookDir here to be the same as the exit
                        //targetPosition = playerTransform.position + Vector3.up * m_currentYDis - Vector3.Normalize(curLookDir) * m_currentXDis;
                    }
                    else
                    {
                        targetPosition = playerTransform.position + Vector3.up * m_currentYDis - curLookDir * m_currentXDis;

                        //targetPosition = playerTransform.position + Vector3.up * m_currentYDis - characterMotion.Forward * m_currentXDis;
                        // Debug.Log(targetPosition);
                    }

                    //cameraLocalRotation = Vector3.zero;
                    m_rotDirection = curLookDir;
                    currentX = GetAngleInDegFromVectors(transform.forward, -Vector3.forward);
                    characterForward = characterMotion.Forward;
                    characterUp = characterMotion.Up;
                    break;
                }
            #endregion

            #region Static
            case CameraMode.Static:
                {
                    if (staticTransformPosition != null)
                    {
                        targetPosition = staticTransformPosition.position;
                    }
                    else
                        cameraMode = CameraMode.Follow;

                    switch (staticLookAtType)
                    {
                        case LookAtType.player:
                            lookAtPosition = playerTransform.position;
                            break;
                        case LookAtType.forward:
                            cameraLocalRotation.x += rightY * joystickSpeed;
                            cameraLocalRotation.y += rightX * joystickSpeed;

                            cameraLocalRotation.y = Mathf.Clamp(cameraLocalRotation.y, -20f, 10f);
                            cameraLocalRotation.x = Mathf.Clamp(cameraLocalRotation.x, -20f, 20f);
                            lookAtPosition = staticTransformPosition.forward;
                            break;
                        case LookAtType.customTransform:
                            //cameraLocalRotation.x += rightY * joystickSpeed;
                            //cameraLocalRotation.y += rightX * joystickSpeed;

                            //cameraLocalRotation.y = Mathf.Clamp(cameraLocalRotation.y, -20f, 10f);
                            //cameraLocalRotation.x = Mathf.Clamp(cameraLocalRotation.x, -20f, 20f);
                            if (staticTransformLookAt != null)

                            {
                                currentX = GetAngleInDegFromVectors(transform.forward, -Vector3.forward);
                                currentY = orbitWantedDistance;
                                //zCamRotation = -staticTransformLookAt.eulerAngles.z;
                                lookAtPosition = staticTransformLookAt.position;
                                //Vector3 _angles = transform.localEulerAngles;
                                //_angles.z = staticTransformLookAt.eulerAngles.z;
                                //Vector3 _anglesTwo = new Vector3(0f, 0f, staticTransformLookAt.eulerAngles.z);
                                //transform.rotation = staticTransformPosition.rotation;
                                //cam.transform.localRotation = staticTransformLookAt.rotation;
                                //cam.transform.localEulerAngles = _anglesTwo;
                                //Debug.Log(cam.transform.localEulerAngles);
                            }
                            else
                            {
                                lookAtPosition = playerTransform.position;
                            }
                            break;
                    }



                    break;
                }
            #endregion

            #region Target
            case CameraMode.Target:
                {
                    // characterMotion.characterMovementType = CharacterMotion.CharacterMovementType.Absolute;
                    lookDir = playerTransform.forward;
                    curLookDir = playerTransform.forward;
                    currentY = orbitWantedDistance;
                    currentX = GetAngleInDegFromVectors(transform.forward, -Vector3.forward);
                    // targetPosition = characterOffset + characterUp * distanceUp - characterForward * distanceAway;

                    targetPosition = playerTransform.position + characterUp * m_targetYDistance - characterForward * m_targetXDistance;
                    cameraLocalRotation.y += rightY * joystickSpeed;
                    cameraLocalRotation.x += rightX * joystickSpeed;
                    cameraLocalRotation.y = Mathf.Clamp(cameraLocalRotation.y, -20f, 10f);
                    cameraLocalRotation.x = Mathf.Clamp(cameraLocalRotation.x, -20f, 20f);

                }
                break;
                #endregion
        }

        // if (cameraMode != CameraMode.Target)
        // characterMotion.characterMovementType = CharacterMotion.CharacterMovementType.Relative;

        m_transform.position = Vector3.SmoothDamp(m_transform.position, targetPosition, ref m_velocityCamSmooth, m_camSmoothDampTime);

        cameraPivotTransform.localRotation = Quaternion.Slerp(cameraPivotTransform.localRotation, Quaternion.Euler(pivotRotation), lerpVelocity * Time.deltaTime);



        m_transform.LookAt(lookAtPosition);

        //if ((cameraMode == CameraMode.Static && staticLookAtType != LookAtType.customTransform) || cameraMode != CameraMode.Static)
        //{

        //zCamRotation = cam.transform.localEulerAngles.z;
        cam.transform.localRotation = Quaternion.Slerp(cam.transform.localRotation, Quaternion.Euler(new Vector3(cameraLocalRotation.y, cameraLocalRotation.x, zCamRotation)), lerpVelocity * Time.deltaTime);

        //}

    }
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(collisionPoint, 2f);

#endif
    }

    private Vector3 RotateCameraWithSurfaceAxis(ref Quaternion rotationWithNormals, Quaternion rotation, Quaternion normalRotation, Vector3 direction)
    {
        if (AxisEqualsSurfaceAngle)
        {
            //solve the lerp problem from orbit to target cam before activation. 
            //rotationWithNormals = Quaternion.Slerp(rotationWithNormals, normalRotation * rotation, Time.deltaTime * 5f);
            rotationWithNormals = normalRotation * rotation;

            //Debug.Log("rotation: " + m_rotation); 
            //Vector3 _rotDirection = m_rotation * m_dir;
        }
        Vector3 _rotDirection = (AxisEqualsSurfaceAngle) ? rotationWithNormals * direction : rotation * direction;
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


        m_tLerpDistance = GFunctions.NormalizedRangeValue(currentY, minY, maxY);


        m_currentXDis = minDistancePosition.x + m_tLerpDistance * maxDistancePosition.x;
        m_currentYDis = minDistancePosition.y + heightPositionByDistance.Evaluate(m_tLerpDistance) * maxDistancePosition.y;

        m_currentXDis = Mathf.Clamp(m_currentXDis, minDistancePosition.x, maxDistancePosition.x);
        m_currentYDis = Mathf.Clamp(m_currentYDis, minDistancePosition.y, maxDistancePosition.y);
        //Debug.LogFormat("x: {0}, hitX: {1}", m_currentXDis, hitXDistance);
        //Debug.LogFormat("y: {0}, hitY: {1}", m_currentYDis, hitYDistance);

    }


    private void BlockXAndYAxis()
    {
        if (Mathf.Abs(currentX) >= 360)
        {
            currentX = 0f;
        }
        if (Mathf.Abs(currentY) >= 360)
        {
            currentY = 0f;
        }


        currentY = Mathf.Clamp(currentY, minY, maxY);
        if (limitXAngle)
            currentX = Mathf.Clamp(currentX, xAngleMin, xAngleMax);
    }


    private void ObstacleBehaviours(ref float hitXDistance, ref float hitYDistance)
    {
        //add here a ref vector3 to return later with the position
        //or a float that return the m_tLerpDistance between 1 and to to know where to position camera. 

        //check that is always vector3.up!!


        //cameraPivotTransform.position.x multiply by rotation to know the true distance. 

        #region RAYCAST PARAMETERS
        Vector3 _rayDirection = transform.position - playerTransform.position;
        Vector3 _rayOrigin = playerTransform.position;
        RaycastHit _hit;
        //redo this
        float _distance = Vector3.Distance(playerTransform.position, transform.position);
        float _plusObstacleRaycastDistance = 0.1f;
        //REDO THIS THIS THIS THIS REDO
        //float _raycastDistance = maxDistance - m_lerpedHeight + _plusObstacleRaycastDistance;
        float _raycastDistance = _distance + _plusObstacleRaycastDistance;
        collisionPoint = Vector3.zero;

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
            Vector3 _playerXPositionOnPlane = Vector3.ProjectOnPlane(playerTransform.position, Vector3.up);

            if (_hit.collider.gameObject.layer == LayerMask.NameToLayer(obstacleLayerString))
            {
                #region OBSTACLE ENTER
                m_fadeRaycastEntered = false;
                m_terrainRaycastEntered = false;
                //gizmoPoint = _hit.point;
                m_colliderRend = GetRendererFromCollision(_hit);
                if (m_colliderRend != null && m_colliderRend.enabled)
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
                    collisionPoint = _hit.point;



                    cameraPivotTransform.position = m_smoothCollision ?
                        Vector3.SmoothDamp(cameraPivotTransform.position, _hit.point, ref m_velocityCollisionCamSmooth, m_camSmoothCollisionDampTime) :
                        _hit.point + (transform.forward);

                    m_obstacleRaycastEntered = true;
                }
                #endregion
            }
            else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer(fadeLayerString)))
            {
                #region FADE ENTER
                m_obstacleRaycastEntered = false;
                m_terrainRaycastEntered = false;
#if UNITY_EDITOR
                gizmoRadius = m_sphereCastRadiusForFadingObjects;
                gizmoPoint = _hit.point;
#endif
                m_colliderRend = GetRendererFromCollision(_hit);

                //ADD LAYER MASK DONNO WHY IT IS NOT WORKING
                //LATER ON CHANGE SPHERE COLLIDER FOR CAPSULE COLLIDER TO AVOID MAKING DISAPEAR OBJECTS FAR
                RaycastHit[] _sphereHits = Physics.SphereCastAll(_hit.point, m_sphereCastRadiusForFadingObjects, Vector3.one, Mathf.Infinity);

                for (int i = 0; i < _sphereHits.Length; i++)
                {

                    if (_sphereHits[i].collider.gameObject.layer == LayerMask.NameToLayer(fadeLayerString))
                    {
                        Renderer _rend = GetRendererFromCollision(_sphereHits[i]);

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
                    collisionPoint = _hit.point;

                    cameraPivotTransform.position =
                    cameraPivotTransform.position = m_smoothCollision ?
                        Vector3.SmoothDamp(cameraPivotTransform.position, _hit.point, ref m_velocityCollisionCamSmooth, m_camSmoothCollisionDampTime) :
                        _hit.point + (transform.forward);

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

            cameraPivotTransform.position = (Vector3.Distance(cameraPivotTransform.position, transform.position) > 0.05f) ?
                Vector3.SmoothDamp(cameraPivotTransform.position, transform.position, ref m_velocityCollisionCamSmooth, m_camSmoothCollisionDampTime) :
                cameraPivotTransform.position = transform.position;

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

    private float GetAngleInDegFromVectors(Vector3 direction, Vector3 worldVector)
    {
        float _angle =
            Mathf.Atan2(Vector3.Dot(Vector3.up, Vector3.Cross(worldVector, direction)),
            Vector3.Dot(worldVector, direction)) * Mathf.Rad2Deg;

        //Debug.Log(_angle);

        return _angle;
    }
    //...until here



    private void CameraRotationByNormal(ref Vector3 rotation, float intensity, float lerpFactor)
    {
        if (characterMotion == null)
            return;

        if (characterMotion.SurfaceNormal != null)
        {


            Vector3 norm = characterMotion.SurfaceNormal;
            if (norm == Vector3.zero)
                return;

            Vector3 cameraPosRelativeToPlayer = (transform.position - characterMotion.transform.position).normalized;
            Vector3 vectorOnFacePlane = Vector3.Cross(cameraPosRelativeToPlayer, Vector3.up);


            Vector3 relNorm = Vector3.ProjectOnPlane(norm, Vector3.Cross(Vector3.up, vectorOnFacePlane));

            float desiredRotation = Vector3.Angle(relNorm, Vector3.up);

            float dot = Vector3.Dot(vectorOnFacePlane, relNorm);

            if (dot > 0) desiredRotation = -desiredRotation;


            //rotation.z = Mathf.Lerp(rotation.z, desiredRotation * intensity, Time.deltaTime * lerpFactor);

            rotation.z = desiredRotation * intensity;


            //rotation = Vector3.Lerp(rotation, desiredRotation, Time.deltaTime * lerpFactor);
        }
    }


    //change this renderer for an array of renderers!!!
    public static Renderer GetRendererFromCollision(RaycastHit hit)
    {
        Renderer _colliderRend = (Renderer)hit.collider.GetComponent<MeshRenderer>();
        if (_colliderRend == null)
            _colliderRend = (Renderer)hit.collider.GetComponent<SkinnedMeshRenderer>();
        if (_colliderRend == null)
            _colliderRend = (Renderer)hit.collider.GetComponent<Renderer>();

        if (_colliderRend == null)
            _colliderRend = (Renderer)hit.collider.GetComponentInChildren<MeshRenderer>();
        if (_colliderRend == null)
            _colliderRend = (Renderer)hit.collider.GetComponentInChildren<SkinnedMeshRenderer>();
        if (_colliderRend == null)
            _colliderRend = (Renderer)hit.collider.GetComponentInChildren<Renderer>();

        //if (_colliderRend == null)
        // Debug.LogError("collider renderer not found", hit.collider);



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
