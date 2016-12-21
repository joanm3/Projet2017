using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterV4 : MonoBehaviour
{


    public float glideAngle;


    //input
    private Vector3 m_inputDirection;
    private float m_inputSpeed;

    //surface
    private Vector3 m_surfaceNormal;

    public Vector3 SurfaceNormal
    {
        get { return m_surfaceNormal; }
        private set { m_surfaceNormal = value; }

    }

    private Vector3 m_surfaceDirection;
    private Vector3 m_surfaceForce;

    //movement
    private Vector3 m_characterDirection;
    private float m_characterSpeed;

    //references
    [SerializeField]
    private Camera m_cam;
    private CharacterController m_controller;

    //raycast
    RaycastHit m_hitInfo;

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
    private float m_tJumpCooldown = 0.1f;


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
        //delta time
        float _dt = Time.deltaTime;
        if (Time.deltaTime > 0.15f)
            _dt = 0.15f;

        //get input
        m_inputDirection = GetInputVector();
        //Debug.Log(m_inputDirection);

        m_surfaceNormal = GetSurfaceNormalByRaycast(out m_hitInfo);
        //assign if isGrounded or not. 
        if (m_tGrav > m_tJumpCooldown)
        {
            m_tGrav = 0f;
        }

        m_isGrounded = (m_tGrav < m_tJumpCooldown) ? GetRaycastAtPosition(out m_hitInfo, 1f) : false;
        m_isGrounded = GetRaycastAtPosition(out m_hitInfo, 1.1f);



        transform.rotation = GetRotationByNormal(m_surfaceNormal);

        if (Input.GetButtonDown("Jump") && m_isGrounded)
        {
            Jump(m_surfaceNormal);
        }


        if (m_isGrounded)
        {
            m_jumpVector = Vector3.zero;
            m_fallVector = Vector3.zero;
            transform.rotation = Quaternion.FromToRotation(transform.up, m_surfaceNormal) * transform.rotation;
        }
        else
        {
            transform.rotation = Quaternion.identity;
            OnAir(_dt);
        }


        //INTEGRATE m_inputGravityMultiplier
        Vector3 _characterMotion = ((m_characterDirection * m_characterSpeed) * m_inputGravityMultiplier) + m_fallVector;

        //UpdatePlayerTransform(InputSpeedDir * (inputGravityMultiplier), current_VelocitySpeedDir + m_fallVector)

        m_controller.Move(_characterMotion * _dt);

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
        m_tGrav += Time.deltaTime;

        m_jumpVector += m_gravForceVector * -m_gravForce * deltaTime;
        m_fallVector += m_jumpVector * deltaTime;
        //Debug.Log(m_jumpVector);
    }

    private Vector3 GetInputVector()
    {
        Vector3 inputVector = (m_cam.transform.forward * Input.GetAxis("Vertical")) + (m_cam.transform.right * Input.GetAxis("Horizontal"));
        inputVector.y = 0f;
        inputVector.Normalize();
        return inputVector;
    }

    private Quaternion GetRotationByNormal(Vector3 normal)
    {
        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, normal);
        Quaternion finalRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, float.PositiveInfinity);
        return finalRotation;
    }

    private Vector3 GetSurfaceNormalByRaycast()
    {

        RaycastHit hitInfo;

        if (GetRaycastAtPosition(out hitInfo))
        {
            return hitInfo.normal;
        }
        return Vector3.zero;
    }

    private Vector3 GetSurfaceNormalByRaycast(out RaycastHit hitInfo)
    {

        if (GetRaycastAtPosition(out hitInfo))
        {
            return hitInfo.normal;
        }
        return Vector3.zero;
    }

    private float GetCurrentSpeedByCurve(Vector3 direction, float magnitude)
    {
        float _floatToReturn = 0.0f;

        Vector3 directionAndMagnitude = direction * magnitude; 

        if (directionAndMagnitude.magnitude > 0.2f)
        {
            if (currentCurveOfSpeed != CurvesOfSpeed.Accelerate)
            {
                _t_time = SetTimeToEquivalent(InputAcceleration, _v_value, 40);
                currentCurveOfSpeed = CurvesOfSpeed.Accelerate;
            }
            _t_time += Time.deltaTime;
            //Clamp to stick inclinaison
            float _v_unclamped = InputAcceleration.Evaluate(_t_time);
            if (_v_unclamped > directionAndMagnitude.magnitude)
                _t_time -= Time.deltaTime;
            _v_value = InputAcceleration.Evaluate(_t_time);
        }
        else
        {
            if (currentCurveOfSpeed != CurvesOfSpeed.Deccelerate)
            {
                _t_time = SetTimeToEquivalent(InputDecceleration, _v_value, 20);
                currentCurveOfSpeed = CurvesOfSpeed.Deccelerate;
            }
            _t_time += Time.deltaTime;
            _v_value = InputDecceleration.Evaluate(_t_time);

        }
        _t_time = Mathf.Clamp(_t_time, 0.0f, 10.0f);    //ou alors on s'en fou (Grace à ma super fonction SetTimeToEquivalent :D)
        _floatToReturn = maxInputSpeed * _v_value;
        return _floatToReturn;
    }

    private float SetTimeToEquivalent(AnimationCurve curveToCheck, float value, int accuracy)
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

    private bool GetRaycastAtPosition(out RaycastHit hitInfo)
    {
        Vector3 newPosition = transform.position;
        Ray ray = new Ray(transform.position, -transform.up);

        if (Physics.Raycast(ray, out hitInfo, float.PositiveInfinity))
        {
            return true;
        }

        return false;
    }

    private bool GetRaycastAtPosition(out RaycastHit hitInfo, float distance)
    {
        Vector3 newPosition = transform.position;
        Ray ray = new Ray(transform.position, -transform.up);

        if (Physics.Raycast(ray, out hitInfo, distance))
        {
            return true;
        }

        return false;
    }


}
