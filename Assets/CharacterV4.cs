using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private float m_inputTimeInSecondsToReachMaxSpeed;
    [SerializeField]
    private float m_inputMaxSpeed; 

    //surface
    [Header("Surfaces")]
    private Vector3 m_surfaceNormal;
    public Vector3 SurfaceNormal
    {
        get { return m_surfaceNormal; }
        private set { m_surfaceNormal = value; }

    }
    private Vector3 m_surfaceDirection;
    private Vector3 m_surfaceForce;
    Quaternion m_normalRotation;

    //final avatar movement
    private Vector3 m_characterDirection;
    private float m_characterSpeed;
    private Quaternion m_characterRotation;

    //references
    [SerializeField]
    private Camera m_cam;
    private CharacterController m_controller;

    //raycast
    RaycastHit m_surfaceHit;

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
    private float m_tJumpCooldown = 0.05f;


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




    private Vector3 m_surfaceHitPoint;
    private Quaternion rotCur;

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
            m_inputDeltaHeadingAngleInDeg = UpdateDeltaAngleInDeg(m_inputDirection, Vector3.forward);

        m_inputRotation = UpdateInputRotation(m_inputDeltaHeadingAngleInDeg);

        m_inputCurrentSpeed = 0f;
        #endregion

        #region GET CURRENT SURFACE VALUES
        if (m_isGrounded)
            m_surfaceNormal = UpdateSurfaceNormalByRaycast(out m_surfaceHit);

        m_normalRotation = GetRotationByNormal2(m_surfaceNormal);
        #endregion

        #region GET CHARACTER VALUES: INPUT + SURFACE
        m_characterRotation = m_inputRotation * m_normalRotation;

        #endregion

        transform.rotation = m_characterRotation;

        #region GRAVITY CALCULATION
        m_tGrav += _dt;
        m_tGrav = Mathf.Max(m_tGrav, 10f);
        //edit this to solve some problems. 
        //m_isGrounded = (m_tGrav > m_tJumpCooldown) ? GetRaycastAtPosition(out m_hitInfo, 0.1f) : false;
        //m_isGrounded = GetRaycastAtPosition(out m_surfaceHit, 0.1f);

        //temp to check
        m_isGrounded = GetRaycastAtPosition(out m_surfaceHit, 0.1f);
        #endregion


        if (m_isGrounded)
        {
            #region ON GROUND BEHAVIOURS
            if (Input.GetButtonDown("Jump"))
            {
                Jump(m_surfaceNormal);
            }
            m_jumpVector = Vector3.zero;
            m_fallVector = Vector3.zero;
            m_surfaceHitPoint = GetSnapPositionByHitPoint(m_surfaceHit.point);
            //transform.rotation = Quaternion.FromToRotation(transform.up, m_surfaceNormal) * transform.rotation;
            transform.position = m_surfaceHitPoint;
            #endregion

        }
        else
        {
            #region ON AIR BEHAVIOURS
            //transform.rotation = Quaternion.identity;
            OnAir(_dt);
            #endregion
        }


        #region CHARACTER MOTION
        //INTEGRATE m_inputGravityMultiplier
        Vector3 _characterMotion = ((m_characterDirection * m_characterSpeed) * m_inputGravityMultiplier) + m_fallVector;
        m_controller.Move(_characterMotion * _dt);
        #endregion

    }

    private void Jump(Vector3 surfaceNormal)
    {
        m_isGrounded = false;
        m_jumpVector = (Vector3.up + (surfaceNormal * 0.5f)).normalized * m_jumpForce;
        Debug.Log("Jump Vector: " + m_jumpVector);
    }

    private void OnAir(float deltaTime)
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

    private float UpdateDeltaAngleInDeg(Vector3 inputDirection, Vector3 forwardVector)
    {
        float _angle =
            Mathf.Atan2(Vector3.Dot(Vector3.up, Vector3.Cross(forwardVector, inputDirection)),
            Vector3.Dot(forwardVector, inputDirection)) * Mathf.Rad2Deg;

        //Debug.Log(_angle);

        return _angle;
    }

    private Quaternion GetRotationByNormal2(Vector3 normal)
    {

        Quaternion _normalRot = transform.rotation;
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

    private Vector3 UpdateSurfaceNormalByRaycast(out RaycastHit hitInfo)
    {

        if (GetRaycastAtPosition(out hitInfo))
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

    private bool GetRaycastAtPosition(out RaycastHit hitInfo, float distance)
    {
        Vector3 newPosition = transform.position;
        Ray ray = new Ray(transform.position + (-transform.up * (m_controller.bounds.extents.y - 0.1f)), -transform.up);
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
    private Quaternion GetRotationByNormal2(Vector3 normal, Quaternion rotation)
    {
        return Quaternion.FromToRotation(Vector3.up, normal) * rotation;
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
