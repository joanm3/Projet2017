using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class CharacterV3 : MonoBehaviour
{


    [Header("Angles")]
    [Space(20.0f)]

    [Tooltip("Jusqu'à quel angle sommes nous en Confort (Gizmos cyan)")]
    [Range(1.0f, 45.0f)]
    public float Confort_angle = 25.0f;
    [Tooltip("Jusqu'à quel angle sommes nous en Glide (au delà = Fall) (Gizmos violet puis rouge)")]
    [Range(45.0f, 90.0f)]
    public float Glide_angle = 25.0f;
    public float Gliding_force_time = 1f;

    [Space(20.0f)]
    [Header("Surface")]

    [Tooltip("La vitesse que donne la surface de Glide_angle maximum")]
    [Range(5.0f, 50.0f)]
    public float maxGlideSpeed = 15.0f;
    [Tooltip("Time est utilisée en tant qu'indicateur de surface. 0 = Confort_angle, 1 = Glide_angle. Value est utilisé pour définir quel proportion de maxGlideSpeed est utilisé (0 = 0%, 1 = 100%)")]
    public AnimationCurve velocityGlideAcceleration = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    [Tooltip("Vitesse de transition par seconde entre la velocité actuelle et celle fournie par la surface (1 = 1 unité de vitesse par seconde). lORSQUE LA VELOCITE MONTE")]
    [Range(1.0f, 40.0f)]
    public float velocityTransitionSpeed_acceleration = 2.0f;
    [Tooltip("Vitesse de transition par seconde entre la velocité actuelle et celle fournie par la surface (1 = 1 unité de vitesse par seconde). LORSQUE LA VELOCITE BAISSE")]
    [Range(1.0f, 40.0f)]
    public float velocityTransitionSpeed_decceleration = 0.5f;

    [Space(20.0f)]
    [Header("Input")]

    [Tooltip("La vitesse que donne le stick sur une surface plane")]
    [Range(5.0f, 40.0f)]
    public float maxInputSpeed = 15.0f;
    [Tooltip("Vitesse de rotation maximum (en angles par seconde)")]
    [Range(5.0f, 1440.0f)]
    public float max_RotationSpeed = 200.0f;
    [Tooltip("Vitesse de rotation minimum (en angles par seconde)")]
    [Range(5.0f, 360.0f)]
    public float min_RotationSpeed = 50.0f;
    [Tooltip("Vitesse de rotation en fonction de la vitesse de deplacement input. De gauche à droite la vitesse de deplacement input, de haut en bas la vitesse de rotation. Interpolation entre min_RotationSpeed et max_RotationSpeed en fonction de la curve")]
    public AnimationCurve rotationBySpeed = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
    [Tooltip("Interpolation entre 0 et maxInputSpeed")]
    public AnimationCurve InputAcceleration = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    [Tooltip("Interpolation entre maxInputSpeed et 0")]
    public AnimationCurve InputDecceleration = AnimationCurve.Linear(0.0f, 1.0f, 0.5f, 0.0f);

    [Range(0f, 1f)]
    public float t_time = 0.0f;
    [Range(0f, 1f)]
    public float v_value = 0.0f;


    private enum CurvesOfSpeed { Accelerate, Deccelerate, NotMoving };

    private CharacterController m_myController;
    [SerializeField]
    private Surface m_actualSurface;
    private Surface m_lastSurface;


    private float m_standard_confort_angle = 25.0f;
    private float m_standard_glide_angle = 25.0f;
    private float m_standard_glide_force = 1f;
    private float m_standard_maxGlideSpeed = 15.0f;
    private AnimationCurve m_standard_velocityGlideAcceleration = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    private float m_standard_velocityTransitionSpeed_acceleration = 2.0f;
    private float m_standard_velocityTransitionSpeed_decceleration = 0.5f;

    private RaycastHit m_rayHit;
    private Vector3 m_surfaceNormal;
    private Vector3 m_current_VelocitySpeedDir = Vector3.zero;    //Velocité actuelle
    private Vector3 m_surface_VelocitySpeedDir = Vector3.zero;    //Velocité renseignée par la surface
    private Vector3 m_inputSpeedDir = Vector3.zero;           //Direction et vitesse
    private Vector3 m_shouldSpeedDir = Vector3.zero;  //Direction et vitesse finale

    private CurvesOfSpeed currentCurveOfSpeed = CurvesOfSpeed.NotMoving;
    private CurvesOfSpeed lastFrameCurveOfSpeed = CurvesOfSpeed.NotMoving;

    private float m_currentRotationSpeed = 0.0f;
    //private float m_oldGlideAngle;
    private float m_startTime;

    private float m_timer;

    void Start()
    {
        m_myController = GetComponent<CharacterController>();
        // m_myController.slopeLimit = Glide_angle;
        //m_oldGlideAngle = Glide_angle;
        m_startTime = Time.time;
        m_actualSurface = null;
        m_lastSurface = null;

        m_standard_confort_angle = Confort_angle;
        m_standard_glide_angle = Glide_angle;
        m_standard_glide_force = Gliding_force_time;
        m_standard_maxGlideSpeed = maxGlideSpeed;
        m_standard_velocityGlideAcceleration = velocityGlideAcceleration;
        m_standard_velocityTransitionSpeed_acceleration = velocityTransitionSpeed_acceleration;
        m_standard_velocityTransitionSpeed_decceleration = velocityTransitionSpeed_decceleration;

    }


    void FixedUpdate()
    {
        //if (m_oldGlideAngle != Glide_angle)
        //{
        //    // m_myController.slopeLimit = Glide_angle;
        //    m_oldGlideAngle = Glide_angle;
        //}

        //Vector3 _rayDirection = -Vector3.up;
        Vector3 _rayDirection = (-m_rayHit.normal != Vector3.zero) ? -m_rayHit.normal : -Vector3.up;
        //Debug.Log(_rayDirection);

        if (Physics.Raycast(transform.position + (-Vector3.up * (m_myController.bounds.extents.y - 0.1f)), _rayDirection, out m_rayHit, Mathf.Infinity))
        {
            Debug.DrawRay(transform.position + (-Vector3.up * (m_myController.bounds.extents.y - 0.1f)), _rayDirection, Color.red);
            m_surfaceNormal = m_rayHit.normal;
            m_actualSurface = m_rayHit.collider.GetComponent<Surface>();
        }
        else
        {
            m_actualSurface = null;
        }


        if (m_lastSurface != m_actualSurface)
        {
            SetSurfaceProperties();
            m_lastSurface = m_actualSurface;
        }

        //Input dir + velocity
        m_inputSpeedDir = GetInputSpeedDir();

        //Surface dir + velocity
        m_current_VelocitySpeedDir = GetVelocitySpeedDir();

        m_shouldSpeedDir = m_inputSpeedDir + m_current_VelocitySpeedDir;

        m_myController.Move(m_shouldSpeedDir * Time.fixedDeltaTime);

        ////Pseudo grav
        //if (Physics.Raycast(transform.position, _rayDirection, out m_rayHit, 1000))
        //{
        //    Vector3 _tempVector = m_myController.transform.position;
        //    _tempVector.y = m_rayHit.point.y + m_myController.bounds.extents.y;
        //    m_myController.transform.position = _tempVector;
        //}

    }


    void OnDrawGizmos()
    {

        if (!Application.isPlaying)
            return;

        //Player - up
        Ray _playerUpRay = new Ray(transform.position + (Vector3.up * m_myController.bounds.extents.y), -Vector3.up);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(_playerUpRay);

        //Surface Normal
        Color _violet = new Color(0.5f, 0.0f, 0.5f);
        Gizmos.color = (Vector3.Angle(Vector3.up, m_surfaceNormal) < Confort_angle) ?
            Color.cyan : (Vector3.Angle(Vector3.up, m_surfaceNormal) < Glide_angle) ?
            _violet : Color.red;
        Gizmos.DrawLine(m_rayHit.point, m_rayHit.point + m_surfaceNormal * 2.5f);
    }


    private void SetSurfaceProperties()
    {
        if (m_actualSurface != null)
        {
            Confort_angle = m_actualSurface.Properties.Confort_angle;
            Glide_angle = m_actualSurface.Properties.Glide_angle;
            Gliding_force_time = m_actualSurface.Properties.Gliding_force_time;
            maxGlideSpeed = m_actualSurface.Properties.maxGlideSpeed;
            velocityGlideAcceleration = m_actualSurface.Properties.velocityGlideAcceleration;
            velocityTransitionSpeed_acceleration = m_actualSurface.Properties.velocityTransitionSpeed_acceleration;
            velocityTransitionSpeed_decceleration = m_actualSurface.Properties.velocityTransitionSpeed_decceleration;
        }
        else
        {
            Confort_angle = m_standard_confort_angle;
            Glide_angle = m_standard_glide_angle;
            Gliding_force_time = m_standard_glide_force;
            maxGlideSpeed = m_standard_maxGlideSpeed;
            velocityGlideAcceleration = m_standard_velocityGlideAcceleration;
            velocityTransitionSpeed_acceleration = m_standard_velocityTransitionSpeed_acceleration;
            velocityTransitionSpeed_decceleration = m_standard_velocityTransitionSpeed_decceleration;
        }
    }

    /// <summary>
    /// Vitesse et direction du joueur par surface
    /// </summary>
    private Vector3 GetVelocitySpeedDir()
    {

        //Vector3 _vectorToReturn = Vector3.zero;

        //Direction
        Vector3 _tempTang = Vector3.Cross(m_rayHit.normal, Vector3.up);
        Vector3 _tang = Vector3.Cross(m_rayHit.normal, _tempTang);

        Debug.DrawLine(m_rayHit.point, m_rayHit.point + _tang * 5f, Color.black);

        float fromCtoG = ((Vector3.Angle(Vector3.up, m_surfaceNormal) - Confort_angle)) / (Glide_angle - Confort_angle);
        fromCtoG = Mathf.Clamp(fromCtoG, 0f, 1f);
        m_surface_VelocitySpeedDir = _tang.normalized * (maxGlideSpeed * velocityGlideAcceleration.Evaluate(fromCtoG));

        //Debug.Log(velocityGlideAcceleration.Evaluate(fromCtoG));

        //lerp de current à surface

        if (m_surface_VelocitySpeedDir.magnitude > m_current_VelocitySpeedDir.magnitude)
        {
            return Vector3.MoveTowards(m_current_VelocitySpeedDir, m_surface_VelocitySpeedDir, velocityTransitionSpeed_acceleration * Time.deltaTime);
        }
        else
        {
            return Vector3.MoveTowards(m_current_VelocitySpeedDir, m_surface_VelocitySpeedDir, velocityTransitionSpeed_decceleration * Time.deltaTime);
        }

    }

    /// <summary>
    /// Vitesse et direction du joueur par Input
    /// </summary>
    private Vector3 GetInputSpeedDir()
    {

        Vector3 _inputSpeedDir = Vector3.zero;
        Vector3 _glideVector = Vector3.zero;

        //Direction
        Vector3 _inputVector = (Vector3.forward * Input.GetAxis("Vertical")) + Vector3.right * Input.GetAxis("Horizontal"); //Input Axis en tant que vec3

        //What happens when we are in a surface with a bigger max angle allowed
        if (Vector3.Angle(Vector3.up, m_surfaceNormal) > Glide_angle)
        {
            float fracComplete = (Time.time - m_startTime) / Gliding_force_time;
            Debug.Log(fracComplete);

            //see if angle is positif or negatif
            bool _positifAngle = true;
            Vector3 _crossProduct = Vector3.Cross(Vector3.up, m_surfaceNormal);
            if (_crossProduct.z < 0) _positifAngle = !_positifAngle;
            Vector3 _slerpVector = new Vector3(Mathf.Abs(_inputVector.x), Mathf.Abs(_inputVector.y), Mathf.Abs(_inputVector.z));
            if (!_positifAngle) _slerpVector *= -1;

            //set slerp
            if (m_timer < Gliding_force_time)
            {
                _glideVector = Vector3.Slerp(Vector3.zero, _slerpVector, fracComplete);
                m_timer += Time.deltaTime;
            }
            else if (m_timer >= Gliding_force_time)
            {
                _glideVector = Vector3.zero;
                m_timer = 0f;
            }
        }
        else
        {
            // try to improve this to not call it every update
            m_timer = 0f;
        }

        _inputVector -= _glideVector;
        //Debug.Log(m_timer);

        //		print("input magnitude " + _inputVector.magnitude);
        Vector3 _vectorTolook = _inputVector;       //Direction que le controler doit regarder
        if (_inputVector.magnitude < 0.3)
            _vectorTolook = transform.forward;

        //Rotation speed
        //		currentRotationSpeed = ((max_RotationSpeed - min_RotationSpeed) * (1/_v_value)) + min_RotationSpeed;
        m_currentRotationSpeed = ((max_RotationSpeed - min_RotationSpeed) * rotationBySpeed.Evaluate(v_value)) + min_RotationSpeed;
        //		currentRotationSpeed = (-1390 * _v_value) + 1440;

        //Rotation
        Quaternion _toRot = Quaternion.LookRotation(_vectorTolook, transform.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _toRot, m_currentRotationSpeed * Time.deltaTime);

        //au cas ou on look nul part
        _inputSpeedDir = transform.forward;

        //Translation Speed
        //		_vectorToReturn *= maxInputSpeed * _inputVector.magnitude;
        float _currentSpeedByCurve = GetCurrentSpeedByCurve(_vectorTolook.normalized * _inputVector.magnitude);
        _inputSpeedDir = _inputSpeedDir.normalized * _currentSpeedByCurve;
        //Debug.Log(_currentSpeedByCurve); 

        return _inputSpeedDir;
    }

    /// <summary>
    /// Renvoie une vitesse en float. Choisi une courbe en fonction de la direction et de la magnitude de la pression du stick
    /// </summary>
    private float GetCurrentSpeedByCurve(Vector3 directionAndMagnitude)
    {
        float _floatToReturn = 0.0f;
        //		float _t_time;		TODO trouver comment passer par l'init qu'une fois

        if (directionAndMagnitude.magnitude > 0.2f)
        {
            //Le joueur utilise le stick : on accelere

            if (currentCurveOfSpeed != CurvesOfSpeed.Accelerate)
            {
                t_time = SetTimeToEquivalent(InputAcceleration, v_value, 40);
                currentCurveOfSpeed = CurvesOfSpeed.Accelerate;
                //				print("Begin accelerate" + _t_time);
            }

            t_time += Time.deltaTime;
            //Clamp to stick inclinaison
            float _v_unclamped = InputAcceleration.Evaluate(t_time);
            if (_v_unclamped > directionAndMagnitude.magnitude)
                t_time -= Time.deltaTime;
            v_value = InputAcceleration.Evaluate(t_time);

        }
        else
        {
            //Le joueur a laché le stick : on deccelere

            if (currentCurveOfSpeed != CurvesOfSpeed.Deccelerate)
            {
                t_time = SetTimeToEquivalent(InputDecceleration, v_value, 20);
                currentCurveOfSpeed = CurvesOfSpeed.Deccelerate;
                //print("Begin deccelerate" + _t_time);
            }

            t_time += Time.deltaTime;
            v_value = InputDecceleration.Evaluate(t_time);

        }

        //		_t_time = Mathf.Clamp(_t_time, 0.0f, 1.0f);		//TODO trouver un moyen d'unclamper (donc vérifier quel pourcentage magnitude est par rapport au time de la dernière key de la curve)
        t_time = Mathf.Clamp(t_time, 0.0f, 10.0f);    //ou alors on s'en fou (Grace à ma super fonction SetTimeToEquivalent :D)

        _floatToReturn = maxInputSpeed * v_value;

        return _floatToReturn;
    }

    /// </summary>
    ///Récupère le premier equivalent t le plus proche de la courbe (une bonne accuracy pour une courbe de 0 à 1 est à peu près de 20)
    /// </summary>
    private float SetTimeToEquivalent(AnimationCurve curveToCheck, float value, int accuracy)
    {
        //		print("value unclamped " + value);
        value = Mathf.Clamp(value, 0f, curveToCheck.keys[curveToCheck.keys.Length - 1].time);
        //		print(accuracy);
        float accuracyNormalized = (Vector2.up * accuracy).normalized.magnitude;
        //		print(accuracyNormalized);

        //		float _step = 1f/accuracy;
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

        //		print("v = " + value + " | v_hypotetic = " + curveToCheck.Evaluate(nearest));
        //		print("t_hypotetic = " + nearest);

        return nearest;
    }

}
