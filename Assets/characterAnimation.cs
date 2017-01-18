using ProjectGiants.GFunctions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class characterAnimation : MonoBehaviour
{

    [Header("Controle les states du character animator")]

    public Animator characterAnimator;
    public CharacterMotion cm;

    [Range(0f, 1f)]
    private float movingMode = 0.5f;

    //	[Range(-1f, 1f)]
    //	public float moveSpeed = 0f;

    [Range(-1f, 1f)]
    private float moveDir = 0f;

    [Tooltip("Temps de chute entre animation petite chute et animation grande chute")]
    [Range(1f, 10f)]
    public float timeOfFalling = 5f;

    [Range(-1f, 1f)]
    public float inclinaison = 0f;  //Character walking against or with the forces of the surface (-1 = walking against, 0 = walking on flat terrain, 1 = walking with)

    public bool isGrounded = true;  //TODO ce booleen = celui grounded de Joan

    private float t_Idle = 0f;
    private float t_FallTime = 0f;
    private float randomTimeToWaitToRaiseTheHead = 10f;
    private const float initialTimeTowait = 10f;

    void Start()
    {
        if (cm == null)
            cm = GameObject.FindObjectOfType<CharacterMotion>();


        randomTimeToWaitToRaiseTheHead = initialTimeTowait;
    }

    public void SetAnimatorDirection(Vector3 characterForward, Vector3 characterUp, Vector3 inputAlongSurface)
    {
        //Test si l'input fait aller à gauche ou à droite

        float dirNum = AngleDir(characterForward, inputAlongSurface, characterUp);

        moveDir = dirNum;

    }

    float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
    {
        Vector3 perp = Vector3.Cross(fwd, targetDir);
        float dir = Vector3.Dot(perp, up);

        return dir;
    }


    private float tempInclinaison = 0f;

    public void SetAnimatorInclinaison(float forwardAngleInclination, float minInclination, float maxInclination, float lerpValue)
    {

        float tempTnclinaison = GFunctions.MappedRangeValue(forwardAngleInclination, minInclination, maxInclination, 1, -1);
        inclinaison = Mathf.Lerp(inclinaison, tempTnclinaison, Time.deltaTime * lerpValue);
        inclinaison = Mathf.Clamp(inclinaison, -1, 1);

    }

    public void SetAnimatorInclinaison(Vector3 inputDirection, Vector3 surfaceDirection, float inputSpeed, float surfaceSpeed)
    {
        //Test si le character il marche, glisse ou monte (depend de la proportion vitesse input/surface vs direction input surface)
        //en haut on va a -1
        //en bas on va a 1
        //horizontal = 0



        tempInclinaison = 0f;

        float IdotS = Vector3.Dot(inputDirection, surfaceDirection);

        tempInclinaison = IdotS;

        inclinaison = tempInclinaison;
    }

    public Vector3 testSurfaceDir = Vector3.right;
    public float testSurfaceSpeed = 0f;

    void Update()
    {

        //Move direction (left or Right)
        //moveDir = -Input.GetAxis ("Horizontal");
        //   SetAnimatorDirection(transform.forward, transform.up, new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"))); //TODO appeller cette fonction depuis le script de Joan
        characterAnimator.SetFloat("Direction", moveDir);

        //MoveSpeed
        //		characterAnimator.SetFloat("MovingSpeed", Mathf.Abs(moveSpeed));
        // SetMovingMode(testinputSPeed, testMax); //TODO appeler cette fonction depuis le script de Joan (A chaque frame !!)
        characterAnimator.SetFloat("MoveMode", movingMode);

        //Inclinaison
        //TODO % of speedmax
        //SetAnimatorInclinaison(new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")), testSurfaceDir, testinputSPeed, testSurfaceSpeed);
        characterAnimator.SetFloat("Inclinaison", inclinaison);

        //Air or not air
        characterAnimator.SetBool("Grounded", isGrounded);


        //Reception
        characterAnimator.SetFloat("ReceptionIntensity", t_FallTime / timeOfFalling);
        if (!isGrounded)
        {
            t_FallTime = Mathf.Clamp(t_FallTime + Time.deltaTime, 0f, timeOfFalling);
        }
        else if (!characterAnimator.GetCurrentAnimatorStateInfo(0).IsName("Fall") && !characterAnimator.GetCurrentAnimatorStateInfo(0).IsName("Reception_BlendTree"))
        {
            t_FallTime = 0f;
        }


    }

    public float testinputSPeed = 0f;
    public float testMax = 15f;

    private float t_Idle2 = 0f;
    private bool playIdle2 = false;



    //Il faut appeller cette fonction toutes les frames
    public void SetMovingMode(float totalSpeed, float speedInputMax)
    {

        float _tempSpeed = totalSpeed / speedInputMax;  //Range 0 to 1
        _tempSpeed /= 2f;   //Range 0 to 0.5
        _tempSpeed = Mathf.Clamp((_tempSpeed * 1.75f) + 0.5f, 0.5f, 1f);    //Range 0.5 to 1. Multiply is used to make the animation run at full speed even if the player isn't

        if (totalSpeed <= 0.1f)
        {

            if (t_Idle >= randomTimeToWaitToRaiseTheHead)
            {
                //				_tempSpeed = 0f;	//jouer l'animation de regarder en l'air	//TROP SOUDAIN
                //				characterAnimator.Play(characterAnimator.GetCurrentAnimatorStateInfo(0).GetHashCode(), -1, 0f);	//restart animation	//NOT WORKING
                t_Idle2 = 0.5f;
                playIdle2 = true;

                t_Idle = 0f;    //reset timer
                randomTimeToWaitToRaiseTheHead = Random.Range(initialTimeTowait - 2f, initialTimeTowait + 10f); //Timer random
            }
            else
            {
                if (playIdle2)
                {
                    t_Idle2 -= Time.deltaTime / 4f;
                    _tempSpeed = Mathf.Lerp(0f, 1f, t_Idle2);
                    if (t_Idle2 <= 0f)
                    {
                        playIdle2 = false;
                    }
                }
                else
                {
                    t_Idle += Time.deltaTime;
                    _tempSpeed = Mathf.Lerp(0f, 0.5f, t_Idle / 4f);
                }


            }

        }
        else
        {
            t_Idle2 = 0f;
            playIdle2 = false;
        }

        movingMode = _tempSpeed;
    }


}
