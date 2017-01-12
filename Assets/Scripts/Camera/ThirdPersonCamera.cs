using ProjectGiants.GFunctions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


struct CameraPosition
{
    //position to align camera to, probable somewhere behind the character
    //or position to point camera at, probable somewhere along characters axis
    private Vector3 position;
    //transform used for any rotation
    private Transform xForm;
    public Vector3 Position { get { return position; } set { position = value; } }
    public Transform XForm { get { return xForm; } set { xForm = value; } }

    public void Init(string camName, Vector3 pos, Transform transform, Transform parent)
    {
        position = pos;
        xForm = transform;
        xForm.name = camName;
        xForm.parent = parent;
        xForm.localPosition = Vector3.zero;
        xForm.localPosition = position;
    }
}


//[RequireComponent(typeof(BarsEffect))]
public class ThirdPersonCamera : MonoBehaviour
{

    [SerializeField]
    private CamMode cameraMode = CamMode.Orbit;
    [SerializeField]
    private float distanceAway;
    [SerializeField]
    private float distanceUp;
    [SerializeField]
    private float smooth;
    [SerializeField]
    private Transform character;
    [SerializeField]
    private float camSmoothDampTime = 0.1f;
    [SerializeField]
    //for doing the lines when needed. 
    private float widescreen = 0.2f;
    [SerializeField]
    //time to draw the lines
    private float targetingTime = 0.5f;
    [SerializeField]
    private CharacterMotion characterMotion;
    [SerializeField]
    private Transform firstPersonCameraPosition;
    [SerializeField]
    [Range(0, 1)]
    private float firstPersonThreshold = 0.3f;
    [SerializeField]
    float fpLookSpeed = 1f;
    [SerializeField]
    [Tooltip("x=xmin, y=xmax, z=ymin, w=ymax")]
    private Vector4 fpsXYminAndMaxClampAngles;
    [SerializeField]
    private Transform staticCameraPosition;
    [SerializeField]
    private float movementThreshold = 1f;
    [SerializeField]
    private float lookDirDampTime = 0.2f;
    [SerializeField]
    private float lookDirFactorRotation = 1f;

    [SerializeField]
    private float distanceAwayMultiplier = 1.5f;
    [SerializeField]
    private float distanceUpMultiplier = 5f;
    [SerializeField]
    private float freeThreshold = 0.1f;
    [SerializeField]
    private Vector2 camMinDistFromChar = new Vector2(1f, -0.5f);
    [SerializeField]
    private float rightStickThreshold = 0.1f;
    [SerializeField]
    private const float freeRotationDegreePerSecond = -5f;
    [SerializeField]
    private Transform parentRig;

    //private global only
    private Vector3 lookDir;
    private Vector3 targetPosition;
    private Vector3 velocityCamSmooth = Vector3.zero;
    private BarsEffect barEffect;
    private CameraPosition firstPersonCamPos = new CameraPosition();

    float fpXRot = 0f;
    float fpYRot = 0f;
    private float fpStartingXRot = 0f;
    private Vector3 gizmoPosition;
    private Vector3 characterForward;
    private Vector3 characterUp;
    private float distanceStartWhenGoingToFPS;
    private float xAxisRot;
    private float lookWeight;
    private Vector3 curLookDir;
    private Vector3 velocityLookDir;
    private Vector3 savedRigToGoal;
    private float distanceAwayFree;
    private float distanceUpFree;
    private Vector2 rightStickPrevFrame = Vector2.zero;



    private const float TARGETING_THRESHOLD = 0.1f;


    #region Properites (Public)
    public enum CamMode
    {
        Orbit, Target, FirstPerson, Static, Free
    };

    public CamMode CameraMode { get { return cameraMode; } }

    #endregion


    #region Unity Methods
    void Start()
    {
        character = GameObject.FindGameObjectWithTag("Player").transform;
        lookDir = character.forward;
        curLookDir = character.forward;
        characterMotion = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterMotion>();
        barEffect = GetComponent<BarsEffect>();
        firstPersonCamPos = new CameraPosition();
        firstPersonCamPos.Init("First Person Camera", firstPersonCameraPosition.localPosition, firstPersonCameraPosition, character);
    }


    void LateUpdate()
    {


        float leftArrowX = Input.GetAxis("360_LD_Stick_X");
        float leftArrowY = Input.GetAxis("360_LD_Stick_Y");
        float rightX = Input.GetAxis("360_R_Stick_X");
        float rightY = Input.GetAxis("360_R_Stick_Y");
        float leftStickX = Input.GetAxis("360_L_Stick_X");
        float leftStickY = Input.GetAxis("360_L_Stick_Y");


        Vector3 _offset = new Vector3(0f, distanceUp, 0f);
        Vector3 characterOffset = character.position + _offset;
        gizmoPosition = characterOffset - this.transform.position;
        Vector3 lookAt = characterOffset;
        Vector3 targetPosition = Vector3.zero;

        if (Input.GetAxis("Target") > TARGETING_THRESHOLD)
        {
            if (barEffect != null) barEffect.coverage = Mathf.SmoothStep(barEffect.coverage, widescreen, targetingTime);
            cameraMode = CamMode.Target;
        }
        else
        {
            if (barEffect != null) barEffect.coverage = Mathf.SmoothStep(barEffect.coverage, 0f, targetingTime);

            //first person case
            if (leftArrowY > firstPersonThreshold && cameraMode != CamMode.Free)// && characterMotion.Speed < 0.2f)
            {
                if (cameraMode != CamMode.FirstPerson)
                {
                    fpStartingXRot = UpdateAngleInDeg(firstPersonCameraPosition.forward, Vector3.forward);
                    //fpXRot = fpStartingXRot;
                    fpXRot = 0f;
                    distanceStartWhenGoingToFPS = Vector3.Distance(this.transform.position, firstPersonCamPos.XForm.position);
                    fpYRot = 0f;
                    cameraMode = CamMode.FirstPerson;
                }
            }

            //free camera case


            if ((Mathf.Abs(rightY) > freeThreshold || Mathf.Abs(rightX) > freeThreshold) && cameraMode != CamMode.FirstPerson) // && System.Math.Round(characterMotion.Speed, 2) == 0)
            {
                cameraMode = CamMode.Free;
                savedRigToGoal = Vector3.zero;
            }



            if ((cameraMode == CamMode.FirstPerson && Input.GetButton("ExitFPV")) ||
                (cameraMode == CamMode.FirstPerson && Mathf.Abs(Input.GetAxis("360_L_Stick_X")) >= TARGETING_THRESHOLD) ||
                (cameraMode == CamMode.FirstPerson && Mathf.Abs(Input.GetAxis("360_L_Stick_Y")) >= TARGETING_THRESHOLD) ||
                (cameraMode == CamMode.Target && Input.GetAxis("Target") <= TARGETING_THRESHOLD))
            {
                if (cameraMode != CamMode.Orbit)
                {
                    cameraMode = CamMode.Orbit;
                    characterMotion.characterMovementType = CharacterMotion.CharacterMovementType.Relative;
                }
            }
        }

        //NOTE NOTE NOTE NOTE NOTE NOTE
        //use this one when we are gliding!!
        //targetPosition = followXform.position + Vector3.up * distanceUp - followXform.forward * distanceAway;

        switch (cameraMode)
        {
            case CamMode.Orbit:
                ResetCamera();
                if (characterMotion.Speed > movementThreshold)
                {
                    //all this does that the character tends to look to the side we are facing. 
                    lookDir = Vector3.Lerp(character.right * (rightX < 0 ? 1f : -1f) * lookDirFactorRotation, character.forward * (rightY < 0 ? -1f : 1f) * lookDirFactorRotation,
                        Mathf.Abs(Vector3.Dot(this.transform.forward, character.forward)));
                    curLookDir = Vector3.Normalize(characterOffset - this.transform.position);
                    curLookDir.y = 0f;
                    curLookDir = Vector3.SmoothDamp(curLookDir, lookDir, ref velocityLookDir, lookDirDampTime);

                }
                targetPosition = characterOffset + character.up * distanceUp - Vector3.Normalize(curLookDir) * distanceAway;
                Debug.DrawLine(character.position, targetPosition, Color.magenta);

                characterForward = characterMotion.Forward;
                characterUp = characterMotion.Up;
                characterMotion.characterMovementType = CharacterMotion.CharacterMovementType.Relative;

                break;

            case CamMode.Free:
                Vector3 rigToGoalDirection = Vector3.Normalize(characterOffset - this.transform.position);
                rigToGoalDirection.y = 0f;
                Vector3 rigToGoal = characterOffset - parentRig.position;
                rigToGoal.y = 0f;
                Debug.DrawRay(parentRig.transform.position, rigToGoal, Color.red);

                //moving camera in and out
                //if statement works for positive values; dont tewwn if stick not increasing in either direction; also dont tween if user is rotation
                //checked against right x threshold because very small values for rightY mess up the lerp function. 
                if (rightY < -1 * rightStickThreshold && rightY <= rightStickPrevFrame.y && Mathf.Abs(rightX) < rightStickThreshold)
                {
                    distanceUpFree = Mathf.Lerp(distanceUp, distanceUp * distanceUpMultiplier, Mathf.Abs(rightY));
                    distanceAwayFree = Mathf.Lerp(distanceAway, distanceAway * distanceAwayMultiplier, Mathf.Abs(rightY));
                    targetPosition = characterOffset + character.up * distanceUpFree - rigToGoalDirection * distanceAwayFree;
                }
                else if (rightY > rightStickThreshold && rightY >= rightStickPrevFrame.y && Mathf.Abs(rightX) < rightStickThreshold)
                {
                    //subract height of camera from height of player to find Y distance
                    distanceUpFree = Mathf.Lerp(Mathf.Abs(transform.position.y - characterOffset.y), camMinDistFromChar.y, rightY);
                    //use magnitude function to find x distance
                    distanceAwayFree = Mathf.Lerp(rigToGoal.magnitude, camMinDistFromChar.x, rightY);
                    targetPosition = characterOffset + character.up * distanceUpFree - rigToGoalDirection * distanceAwayFree;
                }

                parentRig.RotateAround(characterOffset, character.up, freeRotationDegreePerSecond * (Mathf.Abs(rightX) > rightStickThreshold ? rightX : 0f));

                if (rightX != 0 || rightY != 0)
                {
                    savedRigToGoal = rigToGoalDirection;
                }

                if (targetPosition == Vector3.zero)
                {
                    targetPosition = characterOffset + character.up * distanceUpFree - savedRigToGoal * distanceAwayFree;
                }


                this.transform.position = SmoothPosition(this.transform.position, targetPosition);
                transform.LookAt(lookAt);
                break;


            case CamMode.Target:
                //ResetCamera();
                characterMotion.characterMovementType = CharacterMotion.CharacterMovementType.Absolute;
                lookDir = character.forward;
                curLookDir = character.forward;
                //vull que sigui la posicio del personatge menys una distancia en el seu forward
                //targetPosition = followXform.position - (followXform.forward * distanceAway); 
                targetPosition = characterOffset + characterUp * distanceUp - characterForward * distanceAway;
                break;

            case CamMode.FirstPerson:
                //clamp this to a max.
                //ResetCamera();
                fpXRot += rightX * fpLookSpeed;
                fpYRot += rightY * fpLookSpeed;
                fpXRot = Mathf.Clamp(fpXRot, fpsXYminAndMaxClampAngles.x, fpsXYminAndMaxClampAngles.y);
                fpYRot = Mathf.Clamp(fpYRot, -fpsXYminAndMaxClampAngles.z, -fpsXYminAndMaxClampAngles.w);
                firstPersonCamPos.XForm.localRotation = Quaternion.Euler(fpYRot, fpXRot, 0f);
                Quaternion rotationShift = Quaternion.FromToRotation(this.transform.forward, firstPersonCamPos.XForm.forward);
                this.transform.rotation = rotationShift * this.transform.rotation;
                targetPosition = firstPersonCamPos.XForm.position;
                float _distance = Vector3.Distance(this.transform.position, firstPersonCamPos.XForm.position);
                float _goodDistance = (_distance > 0.1) ? GFunctions.NormalizedRangeValue(_distance, 0f, distanceStartWhenGoingToFPS) : 0f;
                lookAt = (Vector3.Lerp(this.transform.position + this.transform.forward, lookAt, _goodDistance));

                //later we can do that the character rotates with this, but not important now. 

                characterMotion.characterMovementType = CharacterMotion.CharacterMovementType.NoInput;
                break;


            case CamMode.Static:
                if (staticCameraPosition != null)
                {
                    //change the time to go to the point or add also a bool to go automatically to that point without lerp. 
                    targetPosition = staticCameraPosition.position;
                }
                else
                {
                    Debug.LogError("No staticCameraPosition assigned", this);
                    cameraMode = CamMode.Orbit;
                }
                break;



        }

        if (cameraMode != CamMode.Free)
        {
            RaycastHit wallHit = new RaycastHit();
            CompensateForWalls(characterOffset, ref targetPosition, out wallHit);
            this.transform.position = SmoothPosition(this.transform.position, targetPosition);
            transform.LookAt(lookAt);
        }

        rightStickPrevFrame = new Vector2(rightX, rightY);

    }


    void DrawGizmos()
    {
        Gizmos.DrawSphere(gizmoPosition, 0.5f);
    }
    #endregion

    #region Methods

    private Vector3 SmoothPosition(Vector3 fromPos, Vector3 toPos)
    {
        //improve the damptime to be able to change between cameras differently
        return Vector3.SmoothDamp(fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
    }

    private void CompensateForWalls(Vector3 fromObject, ref Vector3 toTarget, out RaycastHit wallHit)
    {
        //correct this to not see outside. or add your old code. 
        Debug.DrawLine(fromObject, toTarget, Color.cyan);

        if (Physics.Linecast(fromObject, toTarget, out wallHit))
        {
            Debug.DrawRay(wallHit.point, Vector3.left, Color.red);
            toTarget = new Vector3(wallHit.point.x, toTarget.y, wallHit.point.z);
        }
    }

    private float UpdateAngleInDeg(Vector3 direction, Vector3 worldVector)
    {
        float _angle =
            Mathf.Atan2(Vector3.Dot(Vector3.up, Vector3.Cross(worldVector, direction)),
            Vector3.Dot(worldVector, direction)) * Mathf.Rad2Deg;

        //Debug.Log(_angle);

        return _angle;
    }

    private void ResetCamera()
    {
        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime);
       // parentRig.position = transform.position;
    }

    #endregion
}
