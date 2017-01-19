using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectGiants.GFunctions;

public class CharacterMotion : MonoBehaviour
{

    #region PARAMETERS

    //references
    [SerializeField]
    private Camera m_cam;
    private CharacterController m_controller;
    [SerializeField]
    private Transform m_characterRenderer;
    [SerializeField]
    characterAnimation CA;
    [SerializeField]
    private bool m_snap = true;
    public bool Snap { get { return m_snap; } }

    //input
    public enum CharacterMovementType { Absolute, Relative, NoInput, NoMovement };
    public CharacterMovementType characterMovementType = CharacterMovementType.Absolute;

    [Header("Input")]
    private Vector3 m_inputVector;
    public Vector3 InputVector { get { return m_inputVector; } }
    private float m_inputMagnitude;
    private float m_inputDeltaHeadingAngleInDeg;

    private Quaternion m_inputRotation;
    public Quaternion Rotation { get { return m_inputRotation; } set { m_inputRotation = value; } }
    [SerializeField]
    private float m_startRunningSpeed = 10f;
    //CHECK THIS!!!
    private float m_tMove = 0f;
    [SerializeField]
    [Tooltip("Vitesse de rotation maximum (en angles par seconde)")]
    [Range(5.0f, 1440.0f)]
    private float m_maxRotSpeed = 200.0f;
    [SerializeField]
    [Tooltip("Vitesse de rotation minimum (en angles par seconde)")]
    [Range(5.0f, 360.0f)]
    private float m_minRotSpeed = 50.0f;
    [SerializeField]
    [Tooltip("Vitesse de rotation en fonction de la vitesse de deplacement input. De gauche à droite la vitesse de deplacement input, de haut en bas la vitesse de rotation. Interpolation entre min_RotationSpeed et max_RotationSpeed en fonction de la curve")]
    private AnimationCurve m_rotationBySpeed = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

    //surface
    [Header("Surfaces")]
    private Vector3 m_surfaceNormal;
    public Vector3 SurfaceNormal
    {
        get { return m_surfaceNormal; }
        private set { m_surfaceNormal = value; }
    }
    private Vector3 m_lastSurfaceNormal;
    private Vector3 m_upSurfaceNormal;
    private Quaternion m_normalRotation;
    private Vector3 m_surfaceTangDownwardsNormalized;
    public Vector3 SurfaceTang
    {
        get { return m_surfaceTangDownwardsNormalized; }
        private set { m_surfaceTangDownwardsNormalized = value; }
    }
    [SerializeField]
    private float m_surfaceAngle;

    //raycast
    private RaycastHit m_surfaceHit;
    public RaycastHit SurfaceHit
    {
        get { return m_surfaceHit; }
    }
    private RaycastHit m_upHit;
    private Vector3 m_surfaceHitCharacterPosition;
    private Vector3 m_upHitPoint;

    //gravity
    [SerializeField]
    private bool m_isGrounded = false;
    public bool IsGrounded
    {
        get { return m_isGrounded; }
    }

    private Vector3 m_fallVec;
    private Vector3 m_fallVector;
    [SerializeField]
    private float m_airGravForce = 100f;
    [SerializeField]
    private AnimationCurve m_gravForceOverTime;
    [SerializeField]
    private float m_jumpForce;
    private float m_tGrav = 0f;
    [SerializeField]
    private Vector3 m_gravVector = -Vector3.up;
    [SerializeField]
    private float m_inputGravityMultiplier;
    private Vector3 m_gravForceVector = Vector3.zero;
    [SerializeField]
    private float m_tJumpCooldown = 0.2f;

    //character state
    public enum CharacterState { Idle, Walking, Running, Falling, Jumping, GoingUp, GoingDown, Gliding, StrongGliding, Stopping };
    public CharacterState characterState = CharacterMotion.CharacterState.Idle;

    //avatar movement
    public Vector3 Forward { get { return m_characterForward; } }
    public Vector3 Up { get { return m_characterUp; } }
    public Vector3 Right { get { return m_characterRight; } }
    private Vector3 m_characterForward;
    [SerializeField]
    private Vector3 m_characterDirection;

    private Vector3 m_characterUp;
    private Vector3 m_characterRight;
    [SerializeField]
    private float m_characterSpeed;
    public float Speed
    {
        get { return m_characterSpeed; }
    }
    private Quaternion m_characterRotation;
    private float m_characterAngleInDegFromSurfaceTang;
    private float m_characterCurrentForwardAngleFromGroundZero;
    [SerializeField]
    private float m_characterCurrentSpeed;

    //forces
    [Range(0.1f, 3f)]
    public float massPlayer = 1;
    [Range(0, 1)]
    public float friction = 1;
    public float velMax = 0f;
    [Tooltip("The Character will completely stop when velocity distance from zero is smaller than threshold")]
    public float stopThreshold = 0.9f;
    public bool Glide = true;
    public float StartForcesAngle = 5f;
    public float FallInflectionAngle = 45;
    [SerializeField]
    float m_gravForce = 1f;
    [SerializeField]
    float m_maxForce = 1.0f;
    [SerializeField]
    float m_inputCurrentForce = 0.0f;
    [SerializeField]
    float m_surfaceCurrentDescentForce = 0f;
    [SerializeField]
    float m_currentTotalForce = 0f;
    private Vector3 m_collisionPoint;
    private float m_verticalSpeed;
    [SerializeField]
    private float m_characterInitialJumpSpeed = 1f;
    [SerializeField]
    private float m_lerpForcesVelocity = 1f;
    private bool m_snappedToPosition = false;

    #endregion

    #region UNITY FUNCTIONS

    private void Start()
    {
        m_controller = GetComponent<CharacterController>();
        m_lastSurfaceNormal = m_surfaceNormal;
        m_surfaceAngle = 0f;
        m_characterDirection = m_characterForward;
        m_inputRotation = transform.rotation;
        m_gravFallingVector = m_gravVector;
        if (m_cam == null)
            m_cam = Camera.main;
    }

    private void Update()
    {

        #region DELTA TIME
        //delta time
        float _dt = Time.deltaTime;
        if (Time.deltaTime > 0.15f)
            _dt = 0.15f;
        #endregion

        #region GRAVITY CALCULATION
        m_tGrav += _dt;
        m_tGrav = Mathf.Min(m_tGrav, 2f);
        #endregion

        #region GET INPUT VALUES
        switch (characterMovementType)
        {
            case CharacterMovementType.Relative:
                m_inputVector = UpdateInputVectorRelativeToCamera();
                m_inputMagnitude = GetInputMagnitude();
                break;
            case CharacterMovementType.Absolute:
                m_inputVector = UpdateInputVectorRelativeToCamera();
                m_inputMagnitude = GetInputMagnitude();
                break;
            case CharacterMovementType.NoMovement:
            case CharacterMovementType.NoInput:
                if (m_inputVector != Vector3.zero)
                {
                    m_inputVector = Vector3.zero;
                    m_inputMagnitude = 0f;
                }
                break;
        }



        if (m_inputMagnitude >= 0.5f)
        {
            m_inputDeltaHeadingAngleInDeg = GetAngleInDegFromVectors(m_inputVector, Vector3.forward);
            m_inputRotation = UpdateInputRotation(m_inputRotation, m_inputDeltaHeadingAngleInDeg);
        }

        m_isGrounded = (m_tGrav > m_tJumpCooldown) ? GetRaycastAtPosition(out m_surfaceHit, -Up, 1f) : false;
        //m_isGrounded = true;
        //Debug.Log("char.Up: " + Up);
        #endregion

        #region GET CURRENT SURFACE VALUES
        //we should calculate only when changing surface: lastsurface != currentSurface
        if (m_isGrounded)
        {
            m_surfaceNormal = UpdateSurfaceNormalByRaycast(out m_surfaceHit, -Up, 10f);
            m_upSurfaceNormal = UpdateSurfaceNormalByRaycast(out m_upHit, m_gravVector.normalized, 1f);
        }
        else
        {
            m_surfaceNormal = -m_gravVector.normalized;
        }
        //Debug.Log("surfaceNormal: " + m_surfaceNormal);

        m_normalRotation = GetRotationByNormal2(m_inputRotation, m_surfaceNormal, Vector3.up);

        //calculate when changing surface
        if (m_lastSurfaceNormal != m_surfaceNormal)
        {
            //Debug.LogFormat("surfaceNormal:{0}, up:{0}", m_surfaceNormal, Up);
            m_surfaceAngle = ((m_isGrounded) ? Vector3.Angle(m_surfaceNormal, -m_gravVector.normalized) : 0f);


            //m_surfaceAngle = ((m_isGrounded) ? Vector3.Angle(m_surfaceNormal, Vector3.up) : 0f);
            m_surfaceTangDownwardsNormalized = GetSurfaceTangentDownwards(m_surfaceNormal, m_surfaceHit.point);


            //this is not exactly correct, find a better way to assign GLIDING VECTOR GLIDING VECTOR GLIDING VECTOR 
            if (m_surfaceTangDownwardsNormalized != Vector3.zero &&
                (m_surfaceAngle > StartForcesAngle))
            {
                m_glidingVector = -m_surfaceTangDownwardsNormalized;
            }
            m_lastSurfaceNormal = m_surfaceNormal;
        }
        #endregion


        #region GET CHARACTER VALUES: INPUT + SURFACE
        m_characterRotation = m_normalRotation * m_inputRotation;
        m_characterForward = m_characterRotation * Vector3.forward;
        m_characterUp = m_characterRotation * Vector3.up;
        m_characterRight = m_characterRotation * Vector3.right;
        #endregion

        #region FORCES
        m_characterAngleInDegFromSurfaceTang = Vector3.Angle(m_characterForward, m_surfaceTangDownwardsNormalized);
        m_characterCurrentForwardAngleFromGroundZero = GetCharacterForwardAngleFromGroundZero(m_characterForward);

        //this was used to calculate velmax using force, now we use the velMax to calculate the force to apply. 
        //velMax = VelMax(massPlayer, maxForce, friction);
        m_maxForce = GetMaxForce(friction, velMax, massPlayer);
        m_gravForce = GetGravityFromInflectionAngle(FallInflectionAngle, m_maxForce, massPlayer);
        m_surfaceCurrentDescentForce = (m_characterAngleInDegFromSurfaceTang < 90f) ?
            GetAngleForce(m_gravForce, m_characterCurrentForwardAngleFromGroundZero, massPlayer, m_lerpForcesVelocity) :
            GetAngleForce(m_gravForce, m_surfaceAngle, massPlayer, m_lerpForcesVelocity);

        m_inputCurrentForce = UpdateInputForce(m_maxForce, m_inputMagnitude, 0.7f, m_lerpForcesVelocity);
        m_characterCurrentSpeed = UpdateInputSpeed(ref m_currentTotalForce, m_characterCurrentSpeed, _dt);

        m_characterSpeed = Mathf.Lerp(m_characterSpeed, m_characterCurrentSpeed, _dt * m_lerpForcesVelocity);
        if (!Glide && m_characterSpeed < 0f && m_surfaceAngle < FallInflectionAngle)
        {
            m_characterSpeed = 0f;
        }
        #endregion




        #region ASSIGN CHARACTER STATE
        if (m_isGrounded)
        {
            //When in standard surface
            if (m_surfaceAngle < StartForcesAngle)
            {
                characterState = (m_inputVector.magnitude > 0.3f) ? ((m_characterSpeed >= m_startRunningSpeed) ?
                                                                    CharacterState.Running :
                                                            CharacterState.Walking) :
                    ((m_characterSpeed > 0.05f) ? CharacterState.Stopping :
                                            CharacterState.Idle);
            }
            //when with "gliding" surface
            else if (m_surfaceAngle >= StartForcesAngle && m_surfaceAngle < FallInflectionAngle)
            {
                //going down
                if (m_characterAngleInDegFromSurfaceTang >= 0 && m_characterAngleInDegFromSurfaceTang < 90)
                {
                    characterState = (m_inputVector.magnitude > 0.3) ? CharacterState.GoingDown :
                                                        (Glide) ? CharacterState.Gliding :
                        ((m_characterSpeed > 0.05f) ? CharacterState.Stopping :
                                            CharacterState.Idle);
                }
                //going up
                else if (m_characterAngleInDegFromSurfaceTang >= 90)
                {
                    characterState = (m_inputVector.magnitude > 0.3) ? CharacterState.GoingUp :
                                                    (Glide) ? CharacterState.Gliding :
                        ((m_characterSpeed > 0.05f) ? CharacterState.Stopping :
                                        CharacterState.Idle);

                }
                //if angle goes outside scope
                else if (characterState != CharacterState.Jumping)
                {
                    characterState = CharacterState.Falling;
                }
            }
            //when in "falling" surface
            else if (m_surfaceAngle >= FallInflectionAngle && characterState != CharacterState.Jumping)
            {
                characterState = CharacterState.StrongGliding;
            }
        }
        //On Air
        else
        {
            //air
            if (characterState != CharacterState.Jumping || m_fallVector.y < -0.1f)
                characterState = CharacterState.Falling;
        }
        #endregion


        #region CHARACTER STATE BEHAVIOURS
        switch (characterState)
        {
            case CharacterState.Idle:
            case CharacterState.Walking:
            case CharacterState.Running:
            case CharacterState.Stopping:
                {
                    OnGroundUpdate();
                    break;
                }
            case CharacterState.Falling:
            case CharacterState.Jumping:
                {
                    //redo on air behaviour
                    OnAirUpdate(_dt);
                    break;
                }
            case CharacterState.GoingUp:
                {
                    OnGroundUpdate();
                    break;
                }
            case CharacterState.GoingDown:
                {
                    OnGroundUpdate();
                    break;
                }
            case CharacterState.Gliding:
            case CharacterState.StrongGliding:
                {
                    OnGroundUpdate();
                    break;
                }
        }

        #endregion


        #region ASSIGN ANIMATOR
        AssignAnimation();
        #endregion


        #region CHARACTER MOTION

        switch (characterMovementType)
        {
            case CharacterMovementType.Relative:
            case CharacterMovementType.Absolute:
            case CharacterMovementType.NoInput:
                if (characterMovementType == CharacterMovementType.Relative && transform.parent != null)
                {
                    transform.rotation = Quaternion.Euler(Vector3.zero);
                    m_characterRenderer.rotation = Quaternion.Euler(0f, m_inputDeltaHeadingAngleInDeg, 0f);
                }
                else
                {
                    transform.rotation = m_inputRotation;
                }

                UpdateCharacterDirection(ref m_characterDirection, _dt * 6f);
                //Vector3 _characterMotion = (m_characterDirection * m_characterSpeed) + (m_verticalSpeed * Vector3.up);
                Vector3 _characterMotion = (m_characterDirection * m_characterSpeed) + m_fallVec;

                //Debug.Log("direction: " + (m_characterDirection * m_characterSpeed) + "fallVector:" + m_fallVector + "motion: " + _characterMotion);
                m_controller.Move(_characterMotion * _dt);
                break;
            case CharacterMovementType.NoMovement:
                break;


        }



        #endregion

    }


    private void OnDrawGizmos()
    {

        if (!Application.isPlaying)
            return;

        float _linesLenght = 2f;
        Gizmos.color = Color.blue;
        //forward
        Gizmos.DrawLine(transform.position, transform.position + (m_characterForward * _linesLenght));
        Gizmos.DrawSphere(m_surfaceHit.point, 0.5f);

        //up
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + m_characterUp * _linesLenght);

        //gravity vector
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + m_gravVector * _linesLenght);

        Gizmos.color = Color.magenta;
        //surfaceNormal
        Gizmos.DrawLine(m_surfaceHit.point, m_surfaceHit.point + (m_surfaceNormal * _linesLenght));

        //tangent surface
        Gizmos.color = Color.cyan;
        Vector3 _groundPosition = new Vector3(transform.position.x, transform.position.y - m_controller.bounds.extents.y, transform.position.z);
        Gizmos.DrawLine(_groundPosition, _groundPosition + m_surfaceTangDownwardsNormalized * _linesLenght);

    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (characterState == CharacterState.Falling || characterState == CharacterState.Jumping)
        {

            //fall vector has to have the direction of the tang downwards of the point!
            m_gravFallingVector = GetSurfaceTangentDownwards(hit.normal, hit.point);
            Debug.Log("collision while falling");
        }

    }

    #endregion

    #region BEHAVIOURS

    private void OnGroundUpdate()
    {
        m_gravFallingVector = m_gravVector;
        m_verticalSpeed = 0f;

        //if (Input.GetButtonDown("Jump"))
        //{
        //    m_tGrav = 0f;
        //    characterState = CharacterState.Jumping;
        //    Jump(m_surfaceNormal);
        //}

        //change this to when hitting ground to calculate once, not every frame. 
        m_inputGravityMultiplier = 1f;
        if (m_tGrav >= m_tJumpCooldown)
        {
            m_fallVec = Vector3.zero;
            m_fallVector = Vector3.zero;
        }

        m_surfaceHitCharacterPosition = GetSnapPositionByHitPoint(m_collisionPoint);
        m_upHitPoint = GetSnapPositionByHitPoint(m_upHit.point);

        if (Vector3.Distance(transform.position, m_surfaceHitCharacterPosition) >= 0.1f)
        {
            if (characterState != CharacterState.Gliding && characterState != CharacterState.StrongGliding)
            {
                if (!m_snappedToPosition)
                {
                    Debug.Log("snapping");
                    transform.position = m_surfaceHitCharacterPosition;
                    m_snappedToPosition = true;
                }
            }
        }
    }

    private Vector3 m_gravFallingVector;

    private void OnAirUpdate(float deltaTime)
    {
        m_snappedToPosition = false;
        //m_verticalSpeed += -m_gravForce * deltaTime;
        m_fallVec += -m_gravForce * -m_gravFallingVector * deltaTime;
        //Debug.Log(m_fallVec);
        //test
        m_inputVector = Vector3.zero;

        //m_gravForceVector = m_gravVector * (-m_airGravForce * m_gravForceOverTime.Evaluate(m_tGrav));
        //check this problem later for suming deltatime to tgrav. 
        m_tGrav += deltaTime;
        //Debug.Log(m_tGrav);

        //m_jumpVector += m_gravForceVector * deltaTime;
        //m_fallVector += m_jumpVector;
        //Debug.Log(m_jumpVector);
    }

    private void Jump(Vector3 surfaceNormal)
    {
        m_isGrounded = false;
        m_fallVec = (Vector3.up + (surfaceNormal * 0.5f)).normalized * m_jumpForce; //m_characterInitialJumpSpeed;
        //m_jumpVector = (Vector3.up + (surfaceNormal * 0.5f)).normalized * m_jumpForce;
        Debug.Log("Jump Vector: " + m_fallVec);
    }

    private void AssignAnimation()
    {
        CA.isGrounded = IsGrounded;
        CA.SetAnimatorDirection(Forward, Up, m_inputVector);
        CA.SetAnimatorInclinaison(m_characterCurrentForwardAngleFromGroundZero, -10, 10, 0.5f);
        CA.SetMovingMode(Speed, velMax);
    }





    #endregion

    #region FUNCTIONS TO GET VALUES

    private float CalculateDeltaVel(ref float currentTotalForce, float deltaTime)
    {
        float signVel = Mathf.Sign(m_characterCurrentSpeed);

        //if we want to glide more make friction force smaller when speed is negative or when not pushing the buttons, etc...
        //test stuff now its too strong. 
        //float frictionForce = (signVel < 0) ? 0 : -signVel * m_inputCurrentSpeed * m_inputCurrentSpeed * friction;
        float frictionForce = -signVel * m_characterCurrentSpeed * m_characterCurrentSpeed * friction;
        //Debug.Log(frictionForce);
        //is angle is smaller than start forces, dont apply surfacedescentforce
        //CHANGE HERE TO STOP THE GLIDING LOOP when changing from one surface to another. 
        currentTotalForce = (StartForcesAngle < m_surfaceAngle) ? m_inputCurrentForce + frictionForce + (m_surfaceCurrentDescentForce) : m_inputCurrentForce + frictionForce;




        //force = mass * acc
        float acc = currentTotalForce / massPlayer;
        float deltavel = acc * deltaTime;
        //Debug.Log("angle:" + (m_characterCurrentForwardAngle));
        //Debug.Log("total:" + m_currentTotalForce);
        //Debug.Log("sign:" + signVel);
        //Debug.LogFormat("inputCuF: {0}, frictForce: {1}, descForce: {2}, signVel: {3}", m_inputCurrentForce, frictionForce, m_currentDescentForce, signVel);
        //Debug.LogFormat("SurfaceAngle: {0}, StartForces: {1}, isBigger: {2}", m_surfaceAngle, StartForcesAngle, StartForcesAngle < m_surfaceAngle); 
        return deltavel;
    }

    private float UpdateInputSpeed(ref float currentTotalForce, float inputCurrentSpeed, float deltaTime)
    {
        float _inputCurrentSpeed = inputCurrentSpeed;

        ////you should improve this because it stops sometimes when it shouldnt. 
        if ((_inputCurrentSpeed < stopThreshold) && (_inputCurrentSpeed > -stopThreshold) && m_inputVector.magnitude < 0.2f && m_surfaceAngle < StartForcesAngle)
        {
            _inputCurrentSpeed = 0f;
        }
        else
        {
            _inputCurrentSpeed += CalculateDeltaVel(ref currentTotalForce, deltaTime);
        }
        return _inputCurrentSpeed;

    }

    private float GetCharacterForwardAngleFromGroundZero(Vector3 characterForward)
    {
        Vector3 vectorOnFacePlane = Vector3.ProjectOnPlane(characterForward, Vector3.up);
        float absAngle = Vector3.Angle(characterForward, vectorOnFacePlane);
        float dot = Vector3.Dot(Vector3.up, characterForward);
        return dot < 0 ? -absAngle : absAngle;
    }



    //apply to get force of surface. 
    private float GetAngleForce(float gravityForce, float surfaceAngleInDeg, float mass, float lerpForce)
    {
        return Mathf.Lerp(m_surfaceCurrentDescentForce, -gravityForce * Mathf.Sin(surfaceAngleInDeg * Mathf.Deg2Rad) * mass, Time.deltaTime * lerpForce);

        // return -gravityForce * Mathf.Sin(surfaceAngleInDeg * Mathf.Deg2Rad) * mass;
    }

    private static float VelMax(float mass, float maxForce, float friction)
    {
        return Mathf.Sqrt((mass) * maxForce / friction);
    }

    private static float GetMaxForce(float frictionConst, float maxSpeed, float mass)
    {

        return frictionConst * maxSpeed * maxSpeed;

    }

    private float UpdateInputForce(float maxForce, float forceMagnitude, float minMagnitude, float lerpForce)
    {

        if (forceMagnitude < minMagnitude)
            return 0f;
        // magnitude between 0 and 1
        return Mathf.Lerp(m_inputCurrentForce, maxForce * forceMagnitude, Time.deltaTime * lerpForce);
    }

    private static float GetGravityFromInflectionAngle(float angleInDeg, float fMax, float mass)
    {
        return fMax / (mass * Mathf.Sin(angleInDeg * Mathf.Deg2Rad));
    }

    Vector3 m_glidingVector;

    private void UpdateCharacterDirection(ref Vector3 directionVector, float deltaTime)
    {

        //check all this function it creates bugs. 
        if (Mathf.Sign(Speed) < 0f)
        {
            directionVector = m_glidingVector;
            //Debug.Log("using gliding vector:");
            //directionVector = m_characterForward;
            //directionVector = Vector3.MoveTowards(directionVector, -m_surfaceTangDownwardsNormalized, deltaTime);
        }
        else
        {
            //directionVector = Vector3.MoveTowards(directionVector, m_characterForward, deltaTime);
            directionVector = m_characterForward;
        }
        //directionVector = m_characterForward;

    }


    private Vector3 GetSurfaceTangentDownwards(Vector3 normal, Vector3 point)
    {
        Vector3 _tangFirst = Vector3.Cross(normal, Vector3.up);
        Vector3 _tangDownwards = Vector3.Cross(normal, _tangFirst);
        return _tangDownwards.normalized;
    }

    private Vector3 UpdateInputVectorRelativeToCamera()
    {
        Vector3 inputVector = (m_cam.transform.forward * Input.GetAxis("Vertical"))
        + (m_cam.transform.right * Input.GetAxis("Horizontal"));
        inputVector.y = 0f;
        if (inputVector.magnitude >= 0.99f)
            inputVector.Normalize();
        //Debug.Log(inputVector.magnitude);
        if (inputVector.magnitude <= 0.4f)
            return Vector3.zero;

        return inputVector;
    }

    private Vector3 UpdateInputVectorAbsolute()
    {
        Vector3 inputVector = (Vector3.forward * Input.GetAxis("Vertical"))
        + (Vector3.right * Input.GetAxis("Horizontal"));
        inputVector.y = 0f;
        if (inputVector.magnitude >= 0.99f)
            inputVector.Normalize();
        //Debug.Log(inputVector.magnitude);
        return inputVector;
    }

    private Vector3 UpdateInputVectorOnlyForward()
    {
        Vector3 inputVector = (m_cam.transform.forward * Input.GetAxis("Vertical"));
        inputVector.y = 0f;
        if (inputVector.magnitude >= 0.99f)
            inputVector.Normalize();
        //Debug.Log(inputVector.magnitude);
        return inputVector;
    }

    private float GetInputMagnitude()
    {
        Vector3 inputVector = (Vector3.forward * Input.GetAxis("Vertical"))
        + (Vector3.right * Input.GetAxis("Horizontal"));
        return inputVector.normalized.magnitude;

    }

    private float GetAngleInDegFromVectors(Vector3 direction, Vector3 worldVector)
    {
        float _angle =
            Mathf.Atan2(Vector3.Dot(Vector3.up, Vector3.Cross(worldVector, direction)),
            Vector3.Dot(worldVector, direction)) * Mathf.Rad2Deg;

        //Debug.Log(_angle);

        return _angle;
    }

    private Quaternion GetRotationByNormal2(Quaternion rotation, Vector3 normal, Vector3 upVector)
    {

        Quaternion _normalRot = rotation;
        _normalRot = Quaternion.FromToRotation(upVector, normal);

        return _normalRot;
        //return Quaternion.FromToRotation(Vector3.up, normal) * transform.rotation;
    }

    private Quaternion UpdateInputRotation(Quaternion rotation, float deltaAngleInDegrees)
    {
        m_tMove = GFunctions.NormalizedRangeValue(Speed, 0f, velMax);
        float _currentRotationSpeed = ((m_maxRotSpeed - m_minRotSpeed) * m_rotationBySpeed.Evaluate(m_tMove)) + m_minRotSpeed;
        //Debug.LogFormat("speedValue:{0}, rotSpeed:{1}", m_tMove, _currentRotationSpeed);
        Quaternion _headingDelta = Quaternion.AngleAxis(deltaAngleInDegrees, transform.up);
        Quaternion _rRot = Quaternion.RotateTowards(rotation, _headingDelta, _currentRotationSpeed * Time.deltaTime);

        return _rRot;
    }

    private Vector3 GetSnapPositionByHitPoint(Vector3 point)
    {
        return point - (-transform.up * (m_controller.bounds.extents.y));
    }

    private Vector3 UpdateSurfaceNormalByRaycast(out RaycastHit hitInfo, Vector3 rayDirection, float distance)
    {

        if (GetRaycastAtPosition(out hitInfo, rayDirection, distance))
        {
            return hitInfo.normal;
        }
        return Vector3.zero;
    }

    private bool GetRaycastAtPosition(out RaycastHit hitInfo)
    {
        Vector3 newPosition = transform.position;
        Ray ray = new Ray(transform.position + (-transform.up * (m_controller.bounds.extents.y - 0.1f)), -transform.up);

        if (Physics.Raycast(ray, out hitInfo, float.PositiveInfinity))
        {
            return true;
        }

        return false;
    }



    private bool GetRaycastAtPosition(out RaycastHit hitInfo, Vector3 rayDirection, float distance)
    {
        Ray ray = new Ray(transform.position + (rayDirection * (m_controller.bounds.extents.y - 0.3f)), rayDirection);
        //Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
        if (Physics.Raycast(ray, out hitInfo, distance))
        {
            if (!hitInfo.collider.isTrigger)
            {
                m_collisionPoint = hitInfo.point;
                return true;
            }
        }
        return false;
    }

    #endregion
}
