using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectGiants.GFunctions;

public class CharacterV4 : MonoBehaviour
{

    #region PARAMETERS

    public float MaxGlideSpeed;
    public float ConfortAngle;
    public float GlideAngle;
    [SerializeField]
    private AnimationCurve velocityGlideAcceleration;


    //input
    [Header("Input")]
    private Vector3 m_inputDirection;
    [SerializeField]
    private float m_inputCurrentSpeed;
    private float m_inputDeltaHeadingAngleInDeg;
    private Quaternion m_inputRotation;
    [SerializeField]
    private AnimationCurve m_inputAccelerationCurve;
    [SerializeField]
    private float m_inputTargetSpeed = 20f;
    [SerializeField]
    private float m_startRunningSpeed = 10f;
    [SerializeField]
    private float m_inputTimeToReachMaxSpeed = 2f;
    [SerializeField]
    [Range(0, 3)]
    private float m_inputStopForce = 0.5f;
    private float m_tMove = 0f;


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
    private float m_surfaceForceSpeed;
    private Quaternion m_normalRotation;
    private Vector3 m_tangDownwardsNormalized;
    private float m_surfaceAngle;
    private Vector3 m_surfaceForceVector;
    public bool Glide = true;
    public bool DecreaseSpeed = true;
    private AnimationCurve m_descreaseSpeedCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    public float StartForcesAngle = 5f;
    public float StartFallingAngle = 20f;
    [SerializeField]
    private float m_surfaceMaxSpeed;
    [SerializeField]
    private float velocityTransitionSpeed_acceleration;
    [SerializeField]
    private float velocityTransitionSpeed_decceleration;


    private float m_tGlide;
    [SerializeField]
    private float m_surfaceTimeToReachMaxForce = 2f;
    [SerializeField]
    private float m_surfaceStopForce = 1.5f;

    //final avatar movement
    private Vector3 m_characterForward;
    private Vector3 m_characterUp;
    private float m_characterSpeed;
    private Vector3 m_charMovement;
    private Quaternion m_characterRotation;
    private float m_characterAngleInDegFromSurfaceTang;



    //references
    [SerializeField]
    private Camera m_cam;
    private CharacterController m_controller;
    [SerializeField]
    private Transform m_characterRenderer;

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
    private float m_gravForce = 100f;
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


    public enum CharacterState { Idle, Walking, Running, Falling, Jumping, GoingUp, GoingDown, Gliding, StrongGliding, Stopping };

    public CharacterState characterState = CharacterV4.CharacterState.Idle;


    //marco
    [SerializeField]
    float m_inputCurrentForce = 0.0f;
    [SerializeField]
    float maxForce = 1.0f;
    [SerializeField]
    float massPlayer = 1;
    [SerializeField]
    [Range(0, 1)]
    float friction = 1;
    [SerializeField]
    float angleGravityForce = 1f;
    [SerializeField]
    float velMax = 0f;
    // velocity max when forceInput == forceFriction => forceInput == frictionConstant * velmax  => velmax == forceInput / frictionConstant
    // velmax == sqrt ( mass * maxForce / frictionConstant )

    #endregion

    #region UNITY FUNCTIONS

    private void Start()
    {
        m_controller = GetComponent<CharacterController>();
        m_lastSurfaceNormal = m_surfaceNormal;
        m_surfaceAngle = -1f;
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
        m_inputDirection = UpdateInputVector();
        if (m_inputDirection.magnitude >= 0.1f)
            m_inputDeltaHeadingAngleInDeg = UpdateAngleInDeg(m_inputDirection, Vector3.forward);

        m_inputRotation = UpdateInputRotation(m_inputDeltaHeadingAngleInDeg);

        // old
        //m_inputCurrentSpeed = UpdateInputSpeed(_dt);

        // marco
        maxForce = GetMaxForce(friction, velMax, massPlayer);
        float _trueAngleForce = Mathf.Abs(GetAngleForce(angleGravityForce, m_surfaceAngle));
        float _force = (m_characterAngleInDegFromSurfaceTang >= 90) ? maxForce - _trueAngleForce : maxForce + _trueAngleForce;
        Debug.Log(_force);


        m_inputCurrentForce = UpdateInputForce(_force, _dt);
        m_inputCurrentSpeed = UpdateInputSpeed(m_inputCurrentSpeed, _dt);
        //velMax = VelMax(massPlayer, maxForce, friction);
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
            m_tangDownwardsNormalized = GetSurfaceTangentDownwards(m_surfaceNormal, m_surfaceHit.point);
            m_lastSurfaceNormal = m_surfaceNormal;
        }
        m_surfaceForceSpeed = GetVelocitySurfaceSpeed(MaxGlideSpeed, m_surfaceNormal, m_surfaceAngle, _dt);
        m_surfaceForceVector = m_tangDownwardsNormalized * m_surfaceForceSpeed;

        #endregion



        #region GET CHARACTER VALUES: INPUT + SURFACE
        m_characterRotation = m_normalRotation * m_inputRotation;
        m_characterForward = m_characterRotation * Vector3.forward;
        m_characterUp = m_characterRotation * Vector3.up;
        m_characterSpeed = m_inputCurrentSpeed;
        m_characterAngleInDegFromSurfaceTang = Vector3.Angle(m_characterForward, m_tangDownwardsNormalized);
        m_charMovement = m_characterForward * m_characterSpeed;
        //Debug.Log("calculation: " + GetCharacterMotionVector());
        //Debug.Log("motion: " + m_characterForward * m_characterSpeed);  
        #endregion

        #region GET CHARACTER STATE
        if (m_isGrounded)
        {
            //When in standard surface
            if (m_surfaceAngle < StartForcesAngle)
            {
                characterState = (m_inputDirection.magnitude > 0.3f) ? ((m_characterSpeed >= m_startRunningSpeed) ?
                                                                    CharacterState.Running :
                                                            CharacterState.Walking) :
                    ((m_characterSpeed > 0.05f) ? CharacterState.Stopping :
                                            CharacterState.Idle);
            }
            //when with "gliding" surface
            else if (m_surfaceAngle >= StartForcesAngle && m_surfaceAngle < StartFallingAngle)
            {
                //going down
                if (m_characterAngleInDegFromSurfaceTang >= 0 && m_characterAngleInDegFromSurfaceTang < 90)
                {
                    characterState = (m_inputDirection.magnitude > 0.3) ? CharacterState.GoingDown :
                                                        (Glide) ? CharacterState.Gliding :
                        ((m_characterSpeed > 0.05f) ? CharacterState.Stopping :
                                            CharacterState.Idle);
                }
                //going up
                else if (m_characterAngleInDegFromSurfaceTang >= 90)
                {
                    characterState = (m_inputDirection.magnitude > 0.3) ? CharacterState.GoingUp :
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
            else if (m_surfaceAngle >= StartFallingAngle && characterState != CharacterState.Jumping)
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


        transform.rotation = m_inputRotation;

        #region CHARACTER MOTION
        //INTEGRATE m_inputGravityMultiplier
        Vector3 _characterMotion = ((m_characterForward * m_characterSpeed)) + m_fallVector;
        m_controller.Move(_characterMotion * _dt);
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
        //Vector3 _groundPosition = new Vector3(transform.position.x, transform.position.y - m_controller.bounds.extents.y, transform.position.z);
        //Gizmos.DrawLine(_groundPosition, _groundPosition + m_tangDownwardsNormalized * _linesLenght);
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
        m_characterRenderer.position = m_upHitPoint;
    }

    private void OnAirUpdate(float deltaTime)
    {
        m_gravForceVector = m_gravVector * (-m_gravForce * m_gravForceOverTime.Evaluate(m_tGrav));
        //check this problem later for suming deltatime to tgrav. 
        //m_tGrav += deltaTime;
        //Debug.Log(m_tGrav);

        m_jumpVector += m_gravForceVector * -m_gravForce * deltaTime;
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



    //apply to get force of surface. 
    private static float GetAngleForce(float gravityForce, float surfaceAngle)
    {
        return gravityForce * Mathf.Sin(surfaceAngle * Mathf.Deg2Rad);
    }

    private static float VelMax(float mass, float maxForce, float friction)
    {
        return Mathf.Sqrt((mass) * maxForce / friction);
    }

    private static float GetMaxForce(float frictionConst, float maxSpeed, float mass)
    {

        return frictionConst * maxSpeed * maxSpeed / mass;

    }

    private Vector3 GetCharacterMotionVector()
    {
        Vector3 _vectorToReturn = Vector3.zero;
        float fromCtoG = ((Vector3.Angle(Vector3.up, m_surfaceNormal) - ConfortAngle)) / (GlideAngle - ConfortAngle);
        fromCtoG = Mathf.Clamp(fromCtoG, 0f, 1f);
        Vector3 m_surfaceVelocityVector = m_tangDownwardsNormalized * (m_surfaceMaxSpeed * velocityGlideAcceleration.Evaluate(fromCtoG));
        Vector3 _inputVector = (m_characterForward * m_characterSpeed);
        //si accelere
        _vectorToReturn = (m_surfaceVelocityVector.magnitude > _inputVector.magnitude) ?
            Vector3.MoveTowards(_inputVector, m_surfaceVelocityVector, velocityTransitionSpeed_acceleration * Time.deltaTime) : //accelerate
            Vector3.MoveTowards(_inputVector, m_surfaceVelocityVector, velocityTransitionSpeed_decceleration * Time.deltaTime); //deccelerate
        return _vectorToReturn;
    }

    private Vector3 GetVelocitySurfaceSpeedDir(float maxGlideSpeed, Vector3 surfaceNormal, float surfaceAngle, float deltaTime)
    {
        return m_tangDownwardsNormalized * GetVelocitySurfaceSpeed(maxGlideSpeed, surfaceNormal, surfaceAngle, deltaTime);
    }

    private float GetVelocitySurfaceSpeed(float maxGlideSpeed, Vector3 surfaceNormal, float surfaceAngle, float deltaTime)
    {
        if (surfaceAngle >= StartForcesAngle)
        {
            m_tGlide += deltaTime;
        }
        else
        {
            m_tGlide -= deltaTime * m_surfaceStopForce;
        }
        m_tGlide = Mathf.Clamp(m_tGlide, 0f, m_surfaceTimeToReachMaxForce);

        return maxGlideSpeed * velocityGlideAcceleration.Evaluate(m_tGlide);
    }

    //marco
    private float UpdateInputForce(float maxForce, float deltaTime)
    {
        // magnitude between 0 and 1
        return maxForce * m_inputDirection.magnitude;
    }

    private float CalculateDeltaVel(float deltaTime)
    {
        float frictionForce = -m_inputCurrentSpeed * m_inputCurrentSpeed * friction;
        float currentForce = m_inputCurrentForce + frictionForce;
        //forza = mass * acc
        float acc = currentForce / massPlayer;
        float deltavel = acc * deltaTime;
        //Debug.Log(deltavel); 

        return deltavel;
    }
    // end marco

    private float UpdateInputSpeed(float inputCurrentSpeed, float deltaTime)
    {
        float _inputCurrentSpeed = inputCurrentSpeed;

        ////if ((velMax - m_inputCurrentSpeed) <= 0.1f && m_inputDirection.magnitude >= 0.1f)
        //if ((maxForce - m_inputCurrentSpeed) <= 0.1f && m_inputDirection.magnitude >= 0.1f)
        //{
        //    _inputCurrentSpeed = maxForce;
        //    // _inputCurrentSpeed = velMax;
        //}
        //else if (m_inputCurrentSpeed <= 0.35f && m_inputDirection.magnitude <= 0.1f)
        //{
        //    _inputCurrentSpeed = 0f;
        //}
        //else
        //{
        //    _inputCurrentSpeed += CalculateDeltaVel(deltaTime);
        //}
        _inputCurrentSpeed += CalculateDeltaVel(deltaTime);

        return _inputCurrentSpeed;
    }

    private float UpdateInputSpeedOld(float deltaTime)
    {
        //if we want a decc curve just assign it to accCurve when magnitude is less than ... (start and end should be similar) 
        AnimationCurve _accCurve = m_inputAccelerationCurve;

        if (m_inputDirection.magnitude >= 0.3f)
        {
            m_tMove += deltaTime;
        }
        else
        {
            m_tMove -= deltaTime * m_inputStopForce;
        }
        m_tMove = Mathf.Clamp(m_tMove, 0f, m_inputTimeToReachMaxSpeed);

        float _currentVelocityNormalized = GFunctions.NormalizedRangeValue(m_tMove, 0f, m_inputTimeToReachMaxSpeed);
        return _accCurve.Evaluate(_currentVelocityNormalized) * m_inputTargetSpeed;
    }

    private float UpdateCharacterAngleFromSurface(Vector3 characterForward, Vector3 surfaceTangDownwards)
    {
        return Vector3.Angle(characterForward, surfaceTangDownwards);
    }

    private Vector3 GetSurfaceTangentDownwards(Vector3 normal, Vector3 point)
    {
        Vector3 _tangFirst = Vector3.Cross(normal, Vector3.up);
        Vector3 _tangDownwards = Vector3.Cross(normal, _tangFirst);
        return _tangDownwards.normalized;
    }

    private Vector3 UpdateInputVector()
    {
        Vector3 inputVector = (m_cam.transform.forward * Input.GetAxis("Vertical")) + (m_cam.transform.right * Input.GetAxis("Horizontal"));
        inputVector.y = 0f;
        if (inputVector.magnitude >= 0.99f)
            inputVector.Normalize();
        //Debug.Log(inputVector.magnitude);
        return inputVector;
    }

    private float UpdateAngleInDeg(Vector3 direction, Vector3 zeroVector)
    {
        float _angle =
            Mathf.Atan2(Vector3.Dot(Vector3.up, Vector3.Cross(zeroVector, direction)),
            Vector3.Dot(zeroVector, direction)) * Mathf.Rad2Deg;

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

    private Quaternion UpdateInputRotation(float deltaAngleInDegrees)
    {

        float _currentRotationSpeed = ((m_maxRotSpeed - m_minRotSpeed) * m_rotationBySpeed.Evaluate(m_tMove)) + m_minRotSpeed;
        Quaternion _headingDelta = Quaternion.AngleAxis(deltaAngleInDegrees, transform.up);
        Quaternion _rRot = Quaternion.RotateTowards(transform.rotation, _headingDelta, _currentRotationSpeed * Time.deltaTime);

        return _rRot;
    }

    private static float SetTimeToEquivalent(AnimationCurve curveToCheck, float value, int accuracy)
    {
        value = Mathf.Clamp(value, 0f, curveToCheck.keys[curveToCheck.keys.Length - 1].time);
        float accuracyNormalized = (Vector2.up * accuracy).normalized.magnitude;
        float _step = curveToCheck.keys[curveToCheck.keys.Length - 1].time / accuracy;
        float _v_hypotetic = 0.0f;
        float difference = Mathf.Infinity;
        float nearest = 0.0f;

        for (float t_hypotetic = 0f; t_hypotetic < accuracyNormalized; t_hypotetic += _step)
        {
            _v_hypotetic = curveToCheck.Evaluate(t_hypotetic);
            if (Mathf.Abs(_v_hypotetic - value) < difference)
            {
                difference = Mathf.Abs(_v_hypotetic - value);
                nearest = t_hypotetic;
            }
        }
        return nearest;
    }

    private Vector3 GetSnapPositionByHitPoint(Vector3 point)
    {
        return point - (-transform.up * (m_controller.bounds.extents.y));
    }

    private Vector3 UpdateSurfaceNormalByRaycast(out RaycastHit hitInfo, Vector3 upVector)
    {

        if (GetRaycastAtPosition(out hitInfo, upVector, float.PositiveInfinity))
        {
            return hitInfo.normal;
        }
        return Vector3.zero;
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

    #region OLD
    //old, save until it works to check them, then delete. 
    private Vector3 GetSurfaceNormalByRaycast()
    {

        RaycastHit hitInfo;

        if (GetRaycastAtPosition(out hitInfo))
        {
            return hitInfo.normal;
        }
        return Vector3.zero;
    }
    private Quaternion GetRotationByNormal(Vector3 normal)
    {
        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, normal) * transform.rotation;
        Quaternion finalRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, float.PositiveInfinity);
        return finalRotation;
    }
    private Quaternion UpdateRotationWithNormalAndHeading(Vector3 normal, float headingDeltaAngle)
    {
        //https://forum.unity3d.com/threads/character-align-to-surface-normal.33987/
        Quaternion _normalRot = transform.rotation;
        Quaternion _headingDelta = Quaternion.AngleAxis(headingDeltaAngle, transform.up);
        _normalRot = Quaternion.FromToRotation(Vector3.up, normal);
        return _headingDelta * _normalRot;

    }
    private Quaternion UpdateCharacterRotation(Quaternion inputRotation, Vector3 normal)
    {
        //https://forum.unity3d.com/threads/character-align-to-surface-normal.33987/
        Quaternion _normalRot = transform.rotation;
        _normalRot = Quaternion.FromToRotation(Vector3.up, normal);
        return inputRotation * _normalRot;

    }
    private Quaternion OldUpdateInputRotation(Vector3 inputVector)
    {



        Vector3 _vectorTolook = inputVector;        //Direction que le controler doit regarder
        if (inputVector.magnitude < 0.3)
            _vectorTolook = transform.forward;

        //Rotation speed
        float _currentRotationSpeed = ((m_maxRotSpeed - m_minRotSpeed) * m_rotationBySpeed.Evaluate(m_tMove)) + m_minRotSpeed;

        //Rotation
        //Quaternion _angleRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
        Quaternion _toRot = Quaternion.LookRotation(_vectorTolook, transform.up);
        Quaternion _rRot = Quaternion.RotateTowards(transform.rotation, _toRot, _currentRotationSpeed * Time.deltaTime);

        return _rRot;
    }
    #endregion
}
