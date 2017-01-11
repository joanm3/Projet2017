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

    [Header("Static Camera")]
    public Transform staticTransformPosition;

    [Header("OnTriggerExit")]
    public bool returnToLastCamera = false;
    #endregion


    #region PRIVATE PARAMETERS
    private bool changeCamera = false;
    private float startTime;
    private float journeyLength;



    [SerializeField]
    private ThirdPersonCameraMovement.CameraMode m_lastCameraMode = ThirdPersonCameraMovement.CameraMode.Follow;
    [SerializeField]
    private float m_lastMaxDistance = 50f;
    [SerializeField]
    private float m_lastMinDistance = 5f;
    [SerializeField]
    private float m_lastXModificationAngle = 0f;
    [SerializeField]
    private float m_lastYModificationAngle = 0f;
    [SerializeField]
    private bool m_lastLimitXAngle = false;
    [SerializeField]
    private float m_lastXAngleMin = -50.0f;
    [SerializeField]
    private float m_lastXAngleMax = 50.0f;
    [SerializeField]
    private float m_lastYAngleMin = -20.0f;
    [SerializeField]
    private float m_lastYAngleMax = 50.0f;
    [SerializeField]
    private bool m_lastRotateCameraWithNormal = true;
    [SerializeField]
    private float m_lastRotationIntensity = 1f;
    [SerializeField]
    private Transform m_lastStaticTransformPosition;





    #endregion

    void Start()
    {
        startTime = Time.time;
        if (thirdPersonCameraMovement == null)
            thirdPersonCameraMovement = Camera.main.GetComponentInParent<ThirdPersonCameraMovement>();

        journeyLength = Mathf.Abs(thirdPersonCameraMovement.maxDistance - maxDistance);
        AssignLastValues(thirdPersonCameraMovement);
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
        if (returnToLastCamera && other.tag == "Player")
        {
            //do the lerp like before
            ReturnToLastValues(ref thirdPersonCameraMovement);
        }

    }

    private void LerpToNewValues(float t)
    {
        //        public bool changeGeneralValues = true;
        //public float maxDistance = 50f;
        //public float minDistance = 5f;

        //[Header("Modifications")]
        //public bool changeModifications = true;
        //public float xModificationAngle = 0f;
        //public float yModificationAngle = 0f;


        //[Header("Rotation By Normal")]
        //public bool changeRotationByNormal = true;
        //public bool rotationByNormal = true;
        //public float rotationIntensity = 1f;

        //[Header("Static Camera")]
        //public Transform staticPosition;

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
        }

        thirdPersonCameraMovement.staticTransformPosition = staticTransformPosition;

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
        m_lastMaxDistance = lastCam.maxDistance;
        m_lastMinDistance = lastCam.minDistance;
        m_lastXModificationAngle = lastCam.xModificationAngle;
        m_lastLimitXAngle = lastCam.limitXAngle;
        m_lastXAngleMin = lastCam.xAngleMin;
        m_lastXAngleMax = lastCam.xAngleMax;
        m_lastYAngleMin = lastCam.yAngleMin;
        m_lastYAngleMax = lastCam.yAngleMax;
        m_lastRotateCameraWithNormal = lastCam.rotateCameraWithNormal;
        m_lastRotationIntensity = lastCam.rotationIntensity;
        m_lastStaticTransformPosition = lastCam.staticTransformPosition;
    }

    private void ReturnToLastValues(ref ThirdPersonCameraMovement newCam)
    {
        newCam.cameraMode = m_lastCameraMode;
        newCam.maxDistance = m_lastMaxDistance;
        newCam.minDistance = m_lastMinDistance;
        newCam.xModificationAngle = m_lastXModificationAngle;
        newCam.limitXAngle = m_lastLimitXAngle;
        newCam.xAngleMin = m_lastXAngleMin;
        newCam.xAngleMax = m_lastXAngleMax;
        newCam.yAngleMin = m_lastYAngleMin;
        newCam.yAngleMax = m_lastYAngleMax;
        newCam.rotateCameraWithNormal = m_lastRotateCameraWithNormal;
        newCam.rotationIntensity = m_lastRotationIntensity;
        newCam.staticTransformPosition = m_lastStaticTransformPosition;
    }

}
