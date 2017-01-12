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

    //input
    public enum CharacterMovementType { Absolute, Relative, NoInput, NoMovement };
    public CharacterMovementType characterMovementType = CharacterMovementType.Absolute;

    [Header("Input")]
    private Vector3 m_inputVector;
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
    private float m_surfaceAngle;

    //raycast
    private RaycastHit m_surfaceHit;
    private RaycastHit m_upHit;
    private Vector3 m_surfaceHitCharacterPosition;
    private Vector3 m_upHitPoint;

    //gravity
    [SerializeField]
    private bool m_isGrounded = false;
    private Vector3 m_jumpVector;
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
    private Vector3 m_characterForward;
    [SerializeField]
    private Vector3 m_characterDirection;
    private Vector3 m_characterUp;
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


    #endregion

    #region UNITY FUNCTIONS

    private void Start()
    {
        m_controller = GetComponent<CharacterController>();
        m_lastSurfaceNormal = m_surfaceNormal;
        m_surfaceAngle = 0f;
        m_characterDirection = m_characterForward;
        m_inputRotation = transform.rotation;
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

        Vector3 circleRotationInputVector = (m_cam.transform.right * Input.GetAxis("Horizontal"));


        if (m_inputMagnitude >= 0.1f)
        {
            m_inputDeltaHeadingAngleInDeg = UpdateAngleInDeg(m_inputVector, Vector3.forward);
        }
        // if(characterMovementType == CharacterMovementType.Relative)
        m_inputRotation = UpdateInputRotation(m_inputRotation, m_inputDeltaHeadingAngleInDeg);
        //edit this to solve some problems. for the moment using the world up!!! change it but resolve porblems. 
        m_isGrounded = (m_tGrav > m_tJumpCooldown) ? GetRaycastAtPosition(out m_surfaceHit, Vector3.up, 1f) : false;
        #endregion

        #region GET CURRENT SURFACE VALUES
        //we should calculate only when changing surface: lastsurface != currentSurface
        if (m_isGrounded)
        {
            m_surfaceNormal = UpdateSurfaceNormalByRaycast(out m_surfaceHit, transform.up, 10f);
            m_upSurfaceNormal = UpdateSurfaceNormalByRaycast(out m_upHit, Vector3.up, 1f);
        }
        else
        {
            m_surfaceNormal = Vector3.up;
        }

        m_normalRotation = GetRotationByNormal2(m_inputRotation, m_surfaceNormal);

        //calculate when changing surface
        if (m_lastSurfaceNormal != m_surfaceNormal)
        {
            m_surfaceAngle = ((m_isGrounded) ? Vector3.Angle(m_surfaceNormal, Vector3.up) : 0f);
            m_surfaceTangDownwardsNormalized = GetSurfaceTangentDownwards(m_surfaceNormal, m_surfaceHit.point);
            m_lastSurfaceNormal = m_surfaceNormal;
        }
        #endregion


        #region GET CHARACTER VALUES: INPUT + SURFACE
        m_characterRotation = m_normalRotation * m_inputRotation;
        m_characterForward = (m_characterRotation * Vector3.forward).normalized;
        m_characterUp = m_characterRotation * Vector3.up;
        m_characterCurrentForwardAngleFromGroundZero = GetCharacterForwardAngleFromGroundZero(m_characterForward);
        //Debug.Log(m_characterCurrentForwardAngleFromGroundZero); 
        m_characterSpeed = m_characterCurrentSpeed;
        m_characterAngleInDegFromSurfaceTang = Vector3.Angle(m_characterForward, m_surfaceTangDownwardsNormalized);

        //velMax = VelMax(massPlayer, maxForce, friction);
        m_maxForce = GetMaxForce(friction, velMax, massPlayer);
        m_gravForce = GetGravityFromInflectionAngle(FallInflectionAngle, m_maxForce, massPlayer);
        m_surfaceCurrentDescentForce = GetAngleForce(m_gravForce, m_characterCurrentForwardAngleFromGroundZero, massPlayer);
        m_inputCurrentForce = UpdateInputForce(m_maxForce, m_inputMagnitude);
        //m_inputCurrentSpeed += CalculateDeltaVel(out m_currentTotalForce, _dt);
        m_characterCurrentSpeed = UpdateInputSpeed(ref m_currentTotalForce, m_characterCurrentSpeed, _dt);


        #endregion


        #region GET CHARACTER STATE
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

        switch (characterState)
        {
            case CharacterState.Idle:
            case CharacterState.Walking:
            case CharacterState.Running:
                {
                    OnGroundUpdate();
                    break;
                }
            case CharacterState.Falling:
            case CharacterState.Jumping:
                {
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



        #region CHARACTER MOTION

        switch (characterMovementType)
        {
            case CharacterMovementType.Relative:
            case CharacterMovementType.Absolute:
            case CharacterMovementType.NoInput:
                if (characterMovementType == CharacterMovementType.Relative)
                    transform.rotation = m_inputRotation;

                UpdateCharacterDirection(ref m_characterDirection, _dt * 6f);
                Vector3 _characterMotionA = ((m_characterDirection * m_characterSpeed)) + m_fallVector;
                m_controller.Move(_characterMotionA * _dt);
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
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawWireSphere(m_surfaceHitPoint, 0.5f);
        float _linesLenght = 2f;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (m_characterForward * _linesLenght));
        Gizmos.DrawSphere(m_surfaceHit.point, 0.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(m_surfaceHit.point, m_surfaceHit.point + (m_surfaceNormal * _linesLenght));
        Gizmos.color = Color.cyan;
        Vector3 _groundPosition = new Vector3(transform.position.x, transform.position.y - m_controller.bounds.extents.y, transform.position.z);
        Gizmos.DrawLine(_groundPosition, _groundPosition + m_surfaceTangDownwardsNormalized * _linesLenght);
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
    }

    #endregion

    #region BEHAVIOURS

    private void OnGroundUpdate()
    {
        if (Input.GetButtonDown("Jump"))
        {
            m_tGrav = 0f;
            characterState = CharacterState.Jumping;
            Jump(m_surfaceNormal);
        }

        //change this to when hitting ground to calculate once, not every frame. 
        m_inputGravityMultiplier = 1f;
        if (m_tGrav >= m_tJumpCooldown)
        {
            m_jumpVector = Vector3.zero;
            m_fallVector = Vector3.zero;
        }

        m_surfaceHitCharacterPosition = GetSnapPositionByHitPoint(m_surfaceHit.point);
        m_upHitPoint = GetSnapPositionByHitPoint(m_upHit.point);
        //this bugs when the angle of the surface is big and the character doesnt snap properly!! (problem with upHitPoint). 
        //transform.position = m_upHitPoint; 
        //try maybe with the renderer, but then the collider needs to be reposition and this causes problems. 
        //m_characterRenderer.position = m_upHitPoint;
    }

    private void OnAirUpdate(float deltaTime)
    {
        m_gravForceVector = m_gravVector * (-m_airGravForce * m_gravForceOverTime.Evaluate(m_tGrav));
        //check this problem later for suming deltatime to tgrav. 
        //m_tGrav += deltaTime;
        //Debug.Log(m_tGrav);

        m_jumpVector += m_gravForceVector * -m_airGravForce * deltaTime;
        m_fallVector += m_jumpVector * deltaTime;
        //Debug.Log(m_jumpVector);
    }

    private void Jump(Vector3 surfaceNormal)
    {
        m_isGrounded = false;
        m_jumpVector = (Vector3.up + (surfaceNormal * 0.5f)).normalized * m_jumpForce;
        Debug.Log("Jump Vector: " + m_jumpVector);
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

        //is angle is smaller than start forces, dont apply surfacedescentforce
        currentTotalForce = (StartForcesAngle < m_surfaceAngle) && (Glide) ? m_inputCurrentForce + frictionForce + (m_surfaceCurrentDescentForce) : m_inputCurrentForce + frictionForce;

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
    private static float GetAngleForce(float gravityForce, float surfaceAngleInDeg, float mass)
    {

        return -gravityForce * Mathf.Sin(surfaceAngleInDeg * Mathf.Deg2Rad) * mass;
    }

    private static float VelMax(float mass, float maxForce, float friction)
    {
        return Mathf.Sqrt((mass) * maxForce / friction);
    }

    private static float GetMaxForce(float frictionConst, float maxSpeed, float mass)
    {

        return frictionConst * maxSpeed * maxSpeed;

    }

    private float UpdateInputForce(float maxForce, float forceMagnitude)
    {
        // magnitude between 0 and 1
        return maxForce * forceMagnitude;
    }

    private static float GetGravityFromInflectionAngle(float angleInDeg, float fMax, float mass)
    {
        return fMax / (mass * Mathf.Sin(angleInDeg * Mathf.Deg2Rad));
    }

    private void UpdateCharacterDirection(ref Vector3 directionVector, float deltaTime)
    {
        if (m_inputCurrentForce < Mathf.Abs(m_surfaceCurrentDescentForce))
        {
            //directionVector = m_surfaceTangDownwardsNormalized;

            //directionVector = Vector3.MoveTowards(directionVector, -m_surfaceTangDownwardsNormalized, deltaTime);
        }
        else
        {
            //directionVector = Vector3.MoveTowards(directionVector, m_characterForward, deltaTime);
            //directionVector = m_characterForward;
        }
        directionVector = m_characterForward;

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
        return inputVector.magnitude;
    }

    private float UpdateAngleInDeg(Vector3 direction, Vector3 worldVector)
    {
        float _angle =
            Mathf.Atan2(Vector3.Dot(Vector3.up, Vector3.Cross(worldVector, direction)),
            Vector3.Dot(worldVector, direction)) * Mathf.Rad2Deg;

        //Debug.Log(_angle);

        return _angle;
    }

    private Quaternion GetRotationByNormal2(Quaternion rotation, Vector3 normal)
    {

        Quaternion _normalRot = rotation;
        _normalRot = Quaternion.FromToRotation(Vector3.up, normal);

        return _normalRot;
        //return Quaternion.FromToRotation(Vector3.up, normal) * transform.rotation;
    }

    private Quaternion UpdateInputRotation(Quaternion rotation, float deltaAngleInDegrees)
    {

        float _currentRotationSpeed = ((m_maxRotSpeed - m_minRotSpeed) * m_rotationBySpeed.Evaluate(m_tMove)) + m_minRotSpeed;
        Quaternion _headingDelta = Quaternion.AngleAxis(deltaAngleInDegrees, transform.up);
        Quaternion _rRot = Quaternion.RotateTowards(rotation, _headingDelta, _currentRotationSpeed * Time.deltaTime);

        return _rRot;
    }

    private Vector3 GetSnapPositionByHitPoint(Vector3 point)
    {
        return point - (-transform.up * (m_controller.bounds.extents.y));
    }

    private Vector3 UpdateSurfaceNormalByRaycast(out RaycastHit hitInfo, Vector3 upVector, float distance)
    {

        if (GetRaycastAtPosition(out hitInfo, upVector, distance))
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

    private bool GetRaycastAtPosition(out RaycastHit hitInfo, Vector3 upVector, float distance)
    {
        Ray ray = new Ray(transform.position + (-upVector * (m_controller.bounds.extents.y - 0.3f)), -upVector);
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
        if (Physics.Raycast(ray, out hitInfo, distance))
        {
            return true;
        }

        return false;
    }

    #endregion
}
