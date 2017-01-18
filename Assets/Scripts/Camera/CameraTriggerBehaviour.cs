using UnityEngine;
using System.Collections;

public class CameraTriggerBehaviour : MonoBehaviour
{

    #region PUBLIC PARAMETERS
    public ThirdPersonCameraMovement thirdPersonCameraMovement;
    public ThirdPersonCameraMovement.CameraMode newCameraMode = ThirdPersonCameraMovement.CameraMode.Follow;

    [Header("OnTriggerEnter")]
    [Range(0f, 5f)]
    public float lerpTime = 1f;

    [Header("General Values")]
    public bool changeGeneralValues = true;
    public float maxDistance = 50f;
    public float minDistance = 5f;

    [Header("Modifications")]
    public bool changeModifications = true;
    public float xModificationAngle = 0f;
    public float yModificationAngle = 0f;

    [Header("Angle clamp")]
    public bool changeAngleClamp = true;
    public bool limitXAngle = false;
    public float xAngleMin = -50.0f;
    public float xAngleMax = 50.0f;
    public float yAngleMin = -20.0f;
    public float yAngleMax = 50.0f;

    [Header("Rotation By Normal")]
    public bool changeRotationByNormal = true;
    public bool rotateCameraWithNormal = true;
    public float rotationIntensity = 1f;
    public bool axisEqualsSurfaceAngle = false;

    [Header("Static Camera")]
    public Transform staticTransformPosition;
    public Transform staticTransformLookAt;
    public ThirdPersonCameraMovement.LookAtType staticLookAt;

    [Header("OnTriggerExit")]
    public bool triggerCameraOnExit = false;
    public bool destroyTriggerOnExit = false;

    #endregion


    #region PRIVATE PARAMETERS
    private bool changeCamera = false;
    private float startTime;
    private float journeyLength;



    [SerializeField]
    private ThirdPersonCameraMovement.CameraMode m_lastCameraMode = ThirdPersonCameraMovement.CameraMode.Follow;
    [SerializeField]
    private float m_exitMaxDistance = 50f;
    [SerializeField]
    private float m_exitMinDistance = 5f;
    [SerializeField]
    private float m_exitXModificationAngle = 0f;
    [SerializeField]
    private float m_exitYModificationAngle = 0f;
    [SerializeField]
    private bool m_exitLimitXAngle = false;
    [SerializeField]
    private float m_exitXAngleMin = -50.0f;
    [SerializeField]
    private float m_exitXAngleMax = 50.0f;
    [SerializeField]
    private float m_exitYAngleMin = -20.0f;
    [SerializeField]
    private float m_exitYAngleMax = 50.0f;
    [SerializeField]
    private bool m_exitRotateCameraWithNormal = true;
    [SerializeField]
    private float m_exitRotationIntensity = 1f;
    [SerializeField]
    private Transform m_exitStaticTransformPosition;
    [SerializeField]
    private bool m_exitAxisEqualSurfaceAngle = false;
    [SerializeField]
    private Transform m_exitStaticTransformLookAt;
    [SerializeField]
    private ThirdPersonCameraMovement.LookAtType m_exitStaticLookAt;




    #endregion

    void Start()
    {
        startTime = Time.time;
        if (thirdPersonCameraMovement == null)
            thirdPersonCameraMovement = Camera.main.GetComponentInParent<ThirdPersonCameraMovement>();

        journeyLength = Mathf.Abs(thirdPersonCameraMovement.maxDistance - maxDistance);
        //AssignLastValues(thirdPersonCameraMovement);
    }

    void FixedUpdate()
    {
        //make a function for this because we are going to use it a lot!
        float distCovered = (changeCamera == true) ? (Time.time - startTime) * (1f / lerpTime) : 0f;
        float fracJourney = distCovered / journeyLength;
        //Debug.Log (fracJourney); 

        if (changeCamera == true)
        {
            thirdPersonCameraMovement.currentX = 0f;
            thirdPersonCameraMovement.currentY = 0f;

            LerpToNewValues(fracJourney);

            if (Mathf.Abs(thirdPersonCameraMovement.maxDistance - maxDistance) <= 0.1f)
            {
                ApplyNewValues();
                changeCamera = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {

        //Debug.Log ("trigger entered"); 
        if (other.tag == "Player")
        {
            startTime = Time.time;
            journeyLength = Mathf.Abs(thirdPersonCameraMovement.maxDistance - maxDistance);
            changeCamera = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (triggerCameraOnExit && other.tag == "Player")
        {
            //do the lerp like before
            ReturnToLastValues(ref thirdPersonCameraMovement);
        }

        if (destroyTriggerOnExit && other.tag == "Player")
            Destroy(this);

    }

    private void LerpToNewValues(float t)
    {

        thirdPersonCameraMovement.cameraMode = newCameraMode;


        if (changeGeneralValues)
        {
            thirdPersonCameraMovement.maxDistance = Mathf.Lerp(thirdPersonCameraMovement.maxDistance, maxDistance, t);
            thirdPersonCameraMovement.minDistance = Mathf.Lerp(thirdPersonCameraMovement.minDistance, minDistance, t);
        }

        if (changeModifications)
        {
            thirdPersonCameraMovement.xModificationAngle = Mathf.Lerp(thirdPersonCameraMovement.xModificationAngle, xModificationAngle, t);
            thirdPersonCameraMovement.xModificationAngle = Mathf.Lerp(thirdPersonCameraMovement.yModificationAngle, yModificationAngle, t);
        }

        if (changeAngleClamp)
        {
            thirdPersonCameraMovement.limitXAngle = limitXAngle;
            thirdPersonCameraMovement.xAngleMin = Mathf.Lerp(thirdPersonCameraMovement.xAngleMin, xAngleMin, t);
            thirdPersonCameraMovement.xAngleMax = Mathf.Lerp(thirdPersonCameraMovement.xAngleMax, xAngleMax, t);
            thirdPersonCameraMovement.yAngleMin = Mathf.Lerp(thirdPersonCameraMovement.yAngleMin, yAngleMin, t);
            thirdPersonCameraMovement.yAngleMax = Mathf.Lerp(thirdPersonCameraMovement.yAngleMax, yAngleMax, t);
        }

        if (changeRotationByNormal)
        {
            thirdPersonCameraMovement.rotateCameraWithNormal = rotateCameraWithNormal;
            thirdPersonCameraMovement.rotationIntensity = Mathf.Lerp(thirdPersonCameraMovement.rotationIntensity, rotationIntensity, t);
            thirdPersonCameraMovement.AxisEqualsSurfaceAngle = axisEqualsSurfaceAngle;
        }
        thirdPersonCameraMovement.staticTransformPosition = staticTransformPosition;
        thirdPersonCameraMovement.staticTransformLookAt = staticTransformLookAt;
        thirdPersonCameraMovement.staticLookAtType = staticLookAt;

    }

    private void ApplyNewValues()
    {
        if (changeGeneralValues)
        {
            thirdPersonCameraMovement.maxDistance = maxDistance;
            thirdPersonCameraMovement.minDistance = minDistance;
        }

        if (changeModifications)
        {
            thirdPersonCameraMovement.xModificationAngle = xModificationAngle;
            thirdPersonCameraMovement.xModificationAngle = yModificationAngle;
        }

        if (changeAngleClamp)
        {
            thirdPersonCameraMovement.limitXAngle = limitXAngle;
            thirdPersonCameraMovement.xAngleMin = xAngleMin;
            thirdPersonCameraMovement.xAngleMax = xAngleMax;
            thirdPersonCameraMovement.yAngleMin = yAngleMin;
            thirdPersonCameraMovement.yAngleMax = yAngleMax;
        }

        if (changeRotationByNormal)
        {
            thirdPersonCameraMovement.rotateCameraWithNormal = rotateCameraWithNormal;
            thirdPersonCameraMovement.rotationIntensity = rotationIntensity;
        }

        thirdPersonCameraMovement.staticTransformPosition = staticTransformPosition;
    }

    private void AssignLastValues(ThirdPersonCameraMovement lastCam)
    {
        m_lastCameraMode = lastCam.cameraMode;
        m_exitMaxDistance = lastCam.maxDistance;
        m_exitMinDistance = lastCam.minDistance;
        m_exitXModificationAngle = lastCam.xModificationAngle;
        m_exitLimitXAngle = lastCam.limitXAngle;
        m_exitXAngleMin = lastCam.xAngleMin;
        m_exitXAngleMax = lastCam.xAngleMax;
        m_exitYAngleMin = lastCam.yAngleMin;
        m_exitYAngleMax = lastCam.yAngleMax;
        m_exitRotateCameraWithNormal = lastCam.rotateCameraWithNormal;
        m_exitRotationIntensity = lastCam.rotationIntensity;
        m_exitStaticTransformPosition = lastCam.staticTransformPosition;
        m_exitAxisEqualSurfaceAngle = lastCam.AxisEqualsSurfaceAngle;
        m_exitStaticTransformLookAt = lastCam.staticTransformLookAt;
        m_exitStaticLookAt = lastCam.staticLookAtType;
    }

    private void ReturnToLastValues(ref ThirdPersonCameraMovement newCam)
    {
        newCam.cameraMode = m_lastCameraMode;
        newCam.maxDistance = m_exitMaxDistance;
        newCam.minDistance = m_exitMinDistance;
        newCam.xModificationAngle = m_exitXModificationAngle;
        newCam.limitXAngle = m_exitLimitXAngle;
        newCam.xAngleMin = m_exitXAngleMin;
        newCam.xAngleMax = m_exitXAngleMax;
        newCam.yAngleMin = m_exitYAngleMin;
        newCam.yAngleMax = m_exitYAngleMax;
        newCam.rotateCameraWithNormal = m_exitRotateCameraWithNormal;
        newCam.rotationIntensity = m_exitRotationIntensity;
        newCam.staticTransformPosition = m_exitStaticTransformPosition;
        newCam.AxisEqualsSurfaceAngle = m_exitAxisEqualSurfaceAngle;
        newCam.staticTransformLookAt = m_exitStaticTransformLookAt;
        newCam.staticLookAtType = m_exitStaticLookAt;
    }

}
