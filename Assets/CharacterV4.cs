using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectGiants.GFunctions;

public class CharacterV4 : MonoBehaviour
{

    public float glideAngle;

    //input
    [Header("Input")]
    private Vector3 m_inputDirection;
    private float m_inputCurrentSpeed;
    private float m_inputDeltaHeadingAngleInDeg;
    private Quaternion m_inputRotation;
    [SerializeField]
    private AnimationCurve m_inputAccelerationCurve;
    [SerializeField]
    private float m_inputMaxSpeed = 5f;
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
    private Vector3 m_upSurfaceNormal;
    private Vector3 m_surfaceForce;
    private Quaternion m_normalRotation;
    private Vector3 m_tangDownwardsNormalized;



    //final avatar movement
    private Vector3 m_characterForward;
    private Vector3 m_characterUp;
    private float m_characterSpeed;
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


    //speed
    [Range(0f, 1f)]
    public float _t_time = 0.0f;
    [Range(0f, 1f)]
    public float _v_value = 0.0f;
    private CurvesOfSpeed currentCurveOfSpeed = CurvesOfSpeed.NotMoving;
    private CurvesOfSpeed lastFrameCurveOfSpeed = CurvesOfSpeed.NotMoving;
    [Tooltip("Interpolation entre 0 et maxInputSpeed")]
    public AnimationCurve InputAcceleration = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    [Tooltip("Interpolation entre maxInputSpeed et 0")]
    public AnimationCurve InputDecceleration = AnimationCurve.Linear(0.0f, 1.0f, 0.5f, 0.0f);
    [Tooltip("La vitesse que donne le stick sur une surface plane")]
    [Range(5.0f, 40.0f)]
    public float maxInputSpeed = 15.0f;

    [SerializeField]
    private float m_rotationSpeed = 1f;

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


    public enum CharacterState { Idle, Walking, Running, Falling, Jumping, GoingUp, GoingDown, Gliding };

    public CharacterState characterState = CharacterV4.CharacterState.Idle;



    private enum CurvesOfSpeed
    {
        Accelerate,
        Deccelerate,
        NotMoving
    };


    private void Start()
    {
        m_controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        #region DELTA TIME
        //delta time
        float _dt = Time.deltaTime;
        if (Time.deltaTime > 0.15f)
            _dt = 0.15f;
        #endregion

        #region GET INPUT VALUES
        m_inputDirection = UpdateInputVector();

        if (m_inputDirection.magnitude >= 0.3f)
            m_inputDeltaHeadingAngleInDeg = UpdateAngleInDeg(m_inputDirection, Vector3.forward);

        m_inputRotation = UpdateInputRotation(m_inputDeltaHeadingAngleInDeg);

        //speed
        m_inputCurrentSpeed = UpdateInputSpeed(_dt);

        //edit this to solve some problems. for the moment using the world up!!! change it but resolve porblems. 
        m_isGrounded = (m_tGrav > m_tJumpCooldown) ? GetRaycastAtPosition(out m_surfaceHit, Vector3.up, 1f) : false;
        //m_isGrounded = (m_tGrav > m_tJumpCooldown) ? GetRaycastAtPosition(out m_surfaceHit, m_characterUp, 1f) : false;

        #endregion

        #region GET CURRENT SURFACE VALUES
        //we should calculate only when changing surface: lastsurface != currentSurface
        if (m_isGrounded)
        {
            m_surfaceNormal = UpdateSurfaceNormalByRaycast(out m_surfaceHit, transform.up, 10f);
        }

        m_upSurfaceNormal = UpdateSurfaceNormalByRaycast(out m_upHit, Vector3.up, 1f);
        m_normalRotation = GetRotationByNormal2(m_inputRotation, m_surfaceNormal);
        m_tangDownwardsNormalized = GetSurfaceTangentDownwards(m_surfaceNormal, m_surfaceHit.point);
        #endregion

        #region GET CHARACTER VALUES: INPUT + SURFACE
        m_characterRotation = m_normalRotation * m_inputRotation;
        m_characterForward = m_characterRotation * Vector3.forward;
        m_characterUp = m_characterRotation * Vector3.up;
        m_characterSpeed = m_inputCurrentSpeed;
        m_characterAngleInDegFromSurfaceTang = Vector3.Angle(m_characterForward, m_tangDownwardsNormalized);
        //Debug.Log(m_characterAngleInDegFromSurfaceTang); 
        #endregion


        #region GRAVITY CALCULATION
        m_tGrav += _dt;
        m_tGrav = Mathf.Min(m_tGrav, 2f);
        #endregion


        if (m_isGrounded)
        {
            #region ON GROUND BEHAVIOURS
            if (Input.GetButtonDown("Jump"))
            {
                m_tGrav = 0f;
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
            transform.position = m_upHitPoint; 
            #endregion

        }
        else
        {
            #region ON AIR BEHAVIOURS
            //transform.rotation = Quaternion.identity;
            OnAirUpdate(_dt);
            #endregion
        }

        transform.rotation = m_inputRotation;

        #region CHARACTER MOTION
        //INTEGRATE m_inputGravityMultiplier
        //test not done yet speed

        Vector3 _characterMotion = ((m_characterForward * m_characterSpeed) * m_inputGravityMultiplier) + m_fallVector;
        m_controller.Move(_characterMotion * _dt);
        #endregion

    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawWireSphere(m_surfaceHitPoint, 0.5f);
        float _linesLenght = 2f;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (m_characterForward * _linesLenght));
        Gizmos.DrawSphere(m_surfaceHit.point, 0.5f); 
        Gizmos.color = Color.green;
        Gizmos.DrawLine(m_surfaceHit.point, m_surfaceHit.point + (m_surfaceNormal * _linesLenght));
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + m_tangDownwardsNormalized * _linesLenght);
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f); 
    }

    private float UpdateInputSpeed(float deltaTime)
    {
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
        return m_inputAccelerationCurve.Evaluate(_currentVelocityNormalized) * m_inputMaxSpeed;
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

    private void Jump(Vector3 surfaceNormal)
    {
        m_isGrounded = false;
        m_jumpVector = (Vector3.up + (surfaceNormal * 0.5f)).normalized * m_jumpForce;
        Debug.Log("Jump Vector: " + m_jumpVector);
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

    private Vector3 UpdateInputVector()
    {
        Vector3 inputVector = (m_cam.transform.forward * Input.GetAxis("Vertical")) + (m_cam.transform.right * Input.GetAxis("Horizontal"));
        inputVector.y = 0f;
        inputVector.Normalize();
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
        float _currentRotationSpeed = ((m_maxRotSpeed - m_minRotSpeed) * m_rotationBySpeed.Evaluate(_v_value)) + m_minRotSpeed;
        Quaternion _headingDelta = Quaternion.AngleAxis(deltaAngleInDegrees, transform.up);
        Quaternion _rRot = Quaternion.RotateTowards(transform.rotation, _headingDelta, _currentRotationSpeed * Time.deltaTime);

        return _rRot;
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
        float _currentRotationSpeed = ((m_maxRotSpeed - m_minRotSpeed) * m_rotationBySpeed.Evaluate(_v_value)) + m_minRotSpeed;

        //Rotation
        //Quaternion _angleRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
        Quaternion _toRot = Quaternion.LookRotation(_vectorTolook, transform.up);
        Quaternion _rRot = Quaternion.RotateTowards(transform.rotation, _toRot, _currentRotationSpeed * Time.deltaTime);

        return _rRot;
    }
}
