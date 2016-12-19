﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThirdPersonCameraMovement : MonoBehaviour
{

    public Transform playerTransform;
    public CharacterV3 player;
    public bool useJoystick = false;

    [Header("Camera values")]
    public float maxDistance = 40f;
    public float minDistance = 10f;
    public float cameraSpeed = 2f;
    public float sensitivityX = 4.0f;
    public float sensitivityY = 1.0f;
    public float lerpVelocity = 5f;
    [Range(1, 3)]
    public float fadeDistanceFactor = 3f;
    public LayerMask obstacleMask;
    public LayerMask fadeMask;
    public LayerMask terrainMask;
    public float currentX = 0.0f;
    public float currentY = 0.0f;

    [Header("Player Fadeout")]
    public float minDistToStartFadeCharacter = 5f;
    public float distanceToFullyDisappear = 1.8f;

    [Header("Angle clamp")]
    public float yAngleMin = -20.0f;
    public float yAngleMax = 50.0f;
    public bool limitXAngle = false;
    public float xAngleMin = -50.0f;
    public float xAngleMax = 50.0f;

    [Header("Modificiations")]
    public float xModificationAngle = 0f;
    public float yModificationAngle = 0f;
    public float waitingTimeToMoveCamera = 2f;



    [Header("Pivot local transform")]
    public Transform cameraPivotTransform;
    public Vector3 pivotPosition;
    public bool rotationByNormal = true;
    public float rotationIntensity = 1f;
    public float rotationLerp = 3f;
    public Vector3 pivotRotation;


    private Transform m_transform;
    private Camera m_cam;

    private bool m_fadeRaycastEntered = false;
    private bool m_obstacleRaycastEntered = false;
    private bool m_terrainRaycastEntered = false;
    private TerrainCollider m_terrainCollider = null;

    //private List<Renderer> m_colliderRenderers = new List<Renderer>();
    //private List<TerrainCollider> m_terrainColliders = new List<TerrainCollider>();

    private float m_noKeysTouchedTime = 0.0f;

    [Header("Read Values")]
    [SerializeField]
    private float m_lerpedHeight;
    [SerializeField]
    private float m_trueDistance;
    [SerializeField]
    private Renderer m_colliderRend = null;
    public List<Renderer> fadeRenderers = new List<Renderer>();
    public List<Renderer> obstacleRenderers = new List<Renderer>();
    [SerializeField]
    private Renderer[] playerRenderers;

    private Quaternion m_rotation;
    private Vector3 m_dir;
    private bool m_playerColorChanged = false;

#if UNITY_EDITOR

    Vector3 gizmoPoint;
    Vector3 gizmoRayDirection;
    float gizmoDistance;
    float gizmoRadius;
    Vector3 gizmoRayOrigin;
    Vector3 gizmoDownPoint;
    Vector3 gizmoDownRayDirection;
    float gizmoDownDistance;
    Vector3 gizmoDownRayOrigin;

#endif

    public const string obstacleLayerString = "Obstacle";
    public const string fadeLayerString = "Fade";
    public const string terrainLayerString = "Terrain";



    // Use this for initialization
    void Start()
    {
        m_transform = transform;
        m_cam = Camera.main;
        m_trueDistance = maxDistance;

        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterV3>();

        if (player != null)
        {
            playerRenderers = playerTransform.GetComponentsInChildren<Renderer>();
        }
        else
        {
            Debug.LogError("playerTransform not assigned nor found for ThirdPersonCameraMovement");
        }

    }



    void Update()
    {


        #region GET INPUT
        if (useJoystick)
        {
            float inputXStick = (Input.GetAxis("360_R_Stick_X") > 0.15f || Input.GetAxis("360_R_Stick_X") < -0.15f) ? Input.GetAxis("360_R_Stick_X") : 0f;
            float inputYStick = (Input.GetAxis("360_R_Stick_Y") > 0.15f || Input.GetAxis("360_R_Stick_Y") < -0.15f) ? Input.GetAxis("360_R_Stick_Y") : 0f;

            currentX += inputXStick * cameraSpeed;
            currentY += inputYStick * cameraSpeed;
        }
        else
        {
            currentX += Input.GetAxis("Mouse X");
            currentY += Input.GetAxis("Mouse Y");
        }
        #endregion

        #region BLOCK X AND Y CAMERA ANGLES 
        currentY = Mathf.Clamp(currentY, yAngleMin, yAngleMax);
        if (limitXAngle)
            currentX = Mathf.Clamp(currentX, xAngleMin, xAngleMax);
        #endregion

        //CAMERA BEHAVIOURS

        //BEHAVIOUR Distance from ground
        m_lerpedHeight = MappedLerp(currentY, yAngleMin - minDistance, yAngleMax, maxDistance, 0f);

        //BEHAVIOUR Rotation by normal
        if (rotationByNormal) CameraRotationByNormal(ref pivotRotation, rotationIntensity, rotationLerp);

        //BEHAVIOUR Apply distance when no obstacle and with distance from ground
        if (!m_terrainRaycastEntered && !m_obstacleRaycastEntered)
        {
            m_trueDistance = (Mathf.Abs(m_trueDistance - maxDistance) > 0.1f) ?
                Mathf.Lerp(m_trueDistance, maxDistance - m_lerpedHeight, Time.fixedDeltaTime * lerpVelocity) :
                maxDistance - m_lerpedHeight;
        }


        //BEHAVIOUR Obstacles raycasting
        #region RAYCAST PARAMETERS
        Vector3 _rayDirection = cameraPivotTransform.position - playerTransform.position;
        Vector3 _rayOrigin = playerTransform.position;
        RaycastHit _hit;
        float _distance = maxDistance + (Vector3.Distance(m_transform.position, cameraPivotTransform.position));
        float _plusObstacleRaycastDistance = 0.1f;
        float _raycastDistance = maxDistance - m_lerpedHeight + _plusObstacleRaycastDistance;

        //Draw Gizmo
        gizmoRayDirection = _rayDirection;
        gizmoDistance = 1f;
        gizmoRayOrigin = _rayOrigin;
        #endregion

        //BEHAVIOUR WITH OBSTACLES
        if (Physics.Raycast(_rayOrigin, _rayDirection, out _hit, _raycastDistance, SumLayers(obstacleMask, fadeMask, terrainMask)))
        {
            if (_hit.collider.gameObject.layer == LayerMask.NameToLayer(obstacleLayerString))
            {
                #region OBSTACLE ENTER
                m_fadeRaycastEntered = false;
                m_terrainRaycastEntered = false;
                //gizmoPoint = _hit.point;
                m_colliderRend = GetRendererFromCollision(_hit);
                if (m_colliderRend.enabled)
                {
                    if (!obstacleRenderers.Contains(m_colliderRend)) obstacleRenderers.Add(m_colliderRend);
                    for (int i = 0; i < obstacleRenderers.Count; i++)
                    {
                        if (obstacleRenderers[i].enabled && obstacleRenderers[i] != m_colliderRend)
                        {
                            float _lerpedAlpha = (obstacleRenderers[i].material.color.a < 0.9f) ?
                            Mathf.Lerp(obstacleRenderers[i].material.color.a, 1f, Time.deltaTime * lerpVelocity * 3f) : 1f;
                            obstacleRenderers[i].material.color = new Color(obstacleRenderers[i].material.color.r, obstacleRenderers[i].material.color.g, obstacleRenderers[i].material.color.b, _lerpedAlpha);
                            if (_lerpedAlpha >= 0.95)
                            {
                                obstacleRenderers.Remove(obstacleRenderers[i]);
                            }
                        }
                    }
                    float _lerpedAlphaB = (m_colliderRend.material.color.a >= 0.001f) ?
                    Mathf.Lerp(m_colliderRend.material.color.a, 0.0f, Time.deltaTime * lerpVelocity * 3f) : 0f;
                    m_colliderRend.material.color = new Color(m_colliderRend.material.color.r, m_colliderRend.material.color.g, m_colliderRend.material.color.b, _lerpedAlphaB);
                    //Debug.Log (_lerpedAlpha); 
                    m_trueDistance = Mathf.Lerp(m_trueDistance, _hit.distance, Time.deltaTime * lerpVelocity * 2f);
                    if (Mathf.Abs(m_trueDistance - _hit.distance) <= 0.1f)
                    {
                        m_trueDistance = _hit.distance;
                    }
                    m_obstacleRaycastEntered = true;
                }
                #endregion
            }
            else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer(fadeLayerString)))
            {
                #region FADE ENTER
                m_obstacleRaycastEntered = false;
                m_terrainRaycastEntered = false;
                float _radius = 15f;
                gizmoRadius = _radius;
                gizmoPoint = _hit.point;
                m_colliderRend = GetRendererFromCollision(_hit);

                //ADD LAYER MASK DONNO WHY IT IS NOT WORKING
                //LATER ON CHANGE SPHERE COLLIDER FOR CAPSULE COLLIDER TO AVOID MAKING DISAPEAR OBJECTS FAR
                RaycastHit[] _sphereHits = Physics.SphereCastAll(_hit.point, _radius, Vector3.one, Mathf.Infinity);

                for (int i = 0; i < _sphereHits.Length; i++)
                {

                    if (_sphereHits[i].collider.gameObject.layer == LayerMask.NameToLayer(fadeLayerString))
                    {
                        Renderer _rend = _sphereHits[i].collider.GetComponent<Renderer>();
                        if (!fadeRenderers.Contains(_rend) && _rend != null)
                        {
                            fadeRenderers.Add(_rend);
                        }
                    }
                }


                for (int i = 0; i < fadeRenderers.Count; i++)
                {
                    if (fadeRenderers[i].enabled)
                    {

                        if (fadeRenderers[i].material.HasProperty("_Color"))
                        {
                            float _lerpedAlpha = (fadeRenderers[i].material.color.a >= 0.001f) ? Mathf.Lerp(fadeRenderers[i].material.color.a, 0.5f, Time.deltaTime * lerpVelocity * 3f) : 0f;
                            fadeRenderers[i].material.color = new Color(fadeRenderers[i].material.color.r, fadeRenderers[i].material.color.g, fadeRenderers[i].material.color.b, _lerpedAlpha);
                        }

                        if (fadeRenderers[i].material.HasProperty("_Cloak"))
                        {
                            float _desiredDistance = maxDistance / fadeDistanceFactor;
                            fadeRenderers[i].material.SetFloat("_ChangePoint", _desiredDistance);
                            float _lerpedCloak = (fadeRenderers[i].material.GetFloat("_Cloak") > 0.01f) ?
                                Mathf.Lerp(fadeRenderers[i].material.GetFloat("_Cloak"), 0f, Time.deltaTime * lerpVelocity) : 0f;
                            //Debug.Log("cloak: " + _lerpedCloak);
                            fadeRenderers[i].material.SetFloat("_Cloak", _lerpedCloak);
                        }

                    }
                }

                m_fadeRaycastEntered = true;
                #endregion
            }
            else if ((_hit.collider.gameObject.layer == LayerMask.NameToLayer(terrainLayerString)))
            {
                #region TERRAIN ENTER
                m_fadeRaycastEntered = false;
                m_obstacleRaycastEntered = false;


                if (m_terrainCollider != null)
                {
                    //add code here when exiting a terrainCollider
                }

                m_terrainCollider = _hit.collider.GetComponent<TerrainCollider>();
                if (m_terrainCollider.enabled)
                {
                    m_trueDistance = Mathf.Lerp(m_trueDistance, _hit.distance, Time.deltaTime * lerpVelocity * 2f);
                    //Debug.Log (_hit.distance);
                    //gizmoPoint = _hit.point;
                    m_terrainRaycastEntered = true;
                }
                #endregion
            }
            else
            {
                #region OTHER COLLISIONS
                m_fadeRaycastEntered = false;
                m_terrainRaycastEntered = false;
                m_obstacleRaycastEntered = false;
                #endregion
            }
        }
        else
        {
            #region NO COLLISIONS
            m_fadeRaycastEntered = false;
            m_terrainRaycastEntered = false;
            m_obstacleRaycastEntered = false;
            #endregion
        }



        //BEHAVIOUR when exiting a obstacle object
        if (!m_obstacleRaycastEntered && m_colliderRend != null)
        {
            #region OBSTACLE EXIT
            for (int i = 0; i < obstacleRenderers.Count; i++)
            {
                if (obstacleRenderers[i].enabled)
                {
                    float _lerpedAlpha = (obstacleRenderers[i].material.color.a < 0.9f) ?
                    Mathf.Lerp(obstacleRenderers[i].material.color.a, 1f, Time.deltaTime * lerpVelocity * 3f) : 1f;
                    obstacleRenderers[i].material.color = new Color(obstacleRenderers[i].material.color.r, obstacleRenderers[i].material.color.g, obstacleRenderers[i].material.color.b, _lerpedAlpha);
                    if (_lerpedAlpha >= 0.95)
                    {
                        m_obstacleRaycastEntered = false;
                        m_colliderRend = null;
                        obstacleRenderers.Remove(obstacleRenderers[i]);
                    }
                }
            }
            #endregion
        }

        if (!m_fadeRaycastEntered && m_colliderRend != null)
        {
            #region FADE EXIT
            for (int i = 0; i < fadeRenderers.Count; i++)
            {

                Renderer _fadeRenderer = fadeRenderers[i];
                if (_fadeRenderer.material.HasProperty("_Color"))
                {
                    float _lerpedAlpha = Mathf.Lerp(_fadeRenderer.material.color.a, 1f, Time.fixedDeltaTime * lerpVelocity);
                    _fadeRenderer.material.color = new Color(_fadeRenderer.material.color.r, _fadeRenderer.material.color.g, _fadeRenderer.material.color.b, _lerpedAlpha);
                    if (_lerpedAlpha >= 0.9f)
                    {
                        _fadeRenderer.material.color = new Color(_fadeRenderer.material.color.r, _fadeRenderer.material.color.g, _fadeRenderer.material.color.b, 1f);
                        if (!_fadeRenderer.material.HasProperty("_Cloak"))
                        {
                            fadeRenderers.Remove(_fadeRenderer);
                        }
                    }
                }

                if (_fadeRenderer.material.HasProperty("_Cloak"))
                {
                    float _lerpedCloak = Mathf.Lerp(_fadeRenderer.material.GetFloat("_Cloak"), 1.8f, Time.fixedDeltaTime * lerpVelocity / 10);
                    _fadeRenderer.material.SetFloat("_Cloak", _lerpedCloak);
                    //Debug.Log("cloak: " + _lerpedCloak);
                    if (_lerpedCloak >= 1.5f)
                    {
                        _fadeRenderer.material.SetFloat("_Cloak", 1.8f);
                        fadeRenderers.Remove(_fadeRenderer);
                    }
                }
            }

            if (fadeRenderers.Count <= 0)
            {
                m_colliderRend = null;
            }
            #endregion
        }

        //BEHAVIOUR when exiting terrain
        if (m_terrainRaycastEntered && m_terrainCollider != null)
        {
            #region TERRAIN EXIT
            m_terrainCollider = null;
            m_terrainRaycastEntered = false;
            #endregion
        }


        //BEHAVIOUR Player too close to the camera
        float _distanceToPlayer = Vector3.Distance(playerTransform.position, m_cam.transform.position);
        if (_distanceToPlayer < minDistToStartFadeCharacter)
        {
            #region PLAYER FADE OUT
            float _mappedFadePlayer = MappedLerp(_distanceToPlayer, distanceToFullyDisappear, minDistToStartFadeCharacter, 0, 1f);
            Color _lerpedColor = new Color(_mappedFadePlayer, _mappedFadePlayer, _mappedFadePlayer, _mappedFadePlayer);
            foreach (Renderer renderer in playerRenderers)
            {
                if (renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color = _lerpedColor;
                }
            }
            m_playerColorChanged = true;
        }
        else if (m_playerColorChanged)
        {
            foreach (Renderer renderer in playerRenderers)
            {
                if (renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color = Color.white;
                }
            }
            m_playerColorChanged = false;
            #endregion
        }

    }


    void LateUpdate()
    {
        //APPLY MOVEMENT
        m_dir = new Vector3(0, 0, -m_trueDistance);

        m_rotation = Quaternion.Euler(currentY + yModificationAngle, currentX + xModificationAngle, 0f);
        //Debug.Log("rotation: " + m_rotation); 
        m_transform.position = playerTransform.position + m_rotation * m_dir;

        m_transform.LookAt(playerTransform.position);

        cameraPivotTransform.localPosition = pivotPosition;
        cameraPivotTransform.localRotation = Quaternion.Euler(pivotRotation);
    }

    private void OnDrawGizmos()
    {

        if (!Application.isPlaying)
            return;
        //Vector3 gizmoRayDirection = playerTransform.position - m_transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(gizmoRayOrigin, gizmoRayDirection * gizmoDistance);
        Gizmos.DrawWireSphere(gizmoPoint, gizmoRadius);


        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(gizmoDownRayOrigin, gizmoDownRayDirection * gizmoDownDistance);
        Gizmos.DrawWireSphere(gizmoDownPoint, 1f);

    }

    private void CameraRotationByNormal(ref Vector3 rotation, float xIntensity, float yIntensity, float zIntensity, float lerpFactor)
    {
        if (player == null)
            return;

        if (player.surfaceNormal != null)
        {
            //Vector3 desiredRotation = new Vector3(player.surfaceNormal.x * xIntensity * m_rotation.x, player.surfaceNormal.y * yIntensity * m_rotation.y, player.surfaceNormal.z * zIntensity * m_rotation.z);
            Vector3 desiredRotation = new Vector3((player.surfaceNormal.x - m_rotation.x) * xIntensity, (player.surfaceNormal.y - m_rotation.y) * yIntensity, (player.surfaceNormal.z - m_rotation.z) * zIntensity);

            //Debug.Log("Normal: " + player.surfaceNormal.ToString());
            //Debug.Log("Desired: " + desiredRotation.ToString());

            rotation = Vector3.Lerp(rotation, desiredRotation, Time.deltaTime * lerpFactor);
        }
    }

    private void CameraRotationByNormal(ref Vector3 rotation, float intensity, float lerpFactor)
    {
        if (player == null)
            return;

        if (player.surfaceNormal != null)
        {


            Vector3 norm = player.surfaceNormal;
            if (norm == Vector3.zero)
                return;

            Vector3 cameraPosRelativeToPlayer = (cameraPivotTransform.position - player.transform.position).normalized;
            Vector3 vectorOnFacePlane = Vector3.Cross(cameraPosRelativeToPlayer, Vector3.up);

            Vector3 relNorm = Vector3.ProjectOnPlane(norm, Vector3.Cross(Vector3.up, vectorOnFacePlane));

            float dot = Vector3.Dot(vectorOnFacePlane, relNorm);

            float desiredRotation = Vector3.Angle(relNorm, Vector3.up);

            if (dot > 0) desiredRotation = -desiredRotation;


            rotation.z = Mathf.Lerp(rotation.z, desiredRotation * intensity, Time.deltaTime * lerpFactor);



            //rotation = Vector3.Lerp(rotation, desiredRotation, Time.deltaTime * lerpFactor);
        }
    }

    public static Renderer GetRendererFromCollision(RaycastHit hit)
    {
        Renderer _colliderRend = (Renderer)hit.collider.GetComponent<MeshRenderer>();
        if (_colliderRend == null)
            _colliderRend = (Renderer)hit.collider.GetComponent<MeshRenderer>();
        if (_colliderRend == null)
            _colliderRend = (Renderer)hit.collider.GetComponent<SkinnedMeshRenderer>();
        return _colliderRend;
    }

    public static int SumLayers(LayerMask first, LayerMask second)
    {
        return first.value + second.value;
    }

    public static int SumLayers(LayerMask first, LayerMask second, LayerMask third)
    {
        return first.value + second.value + third.value;
    }

    public static float MappedLerp(float valueToTransform, float oldMin, float oldMax, float newMin, float newMax)
    {
        float oldRange = oldMax - oldMin;
        float newRange = newMax - newMin;
        return (((valueToTransform - oldMin) * newRange) / oldRange) + newMin;
    }

    public void FadeExitBehaviour(ref ThirdPersonCameraMovement thirdPersonCamera, Renderer renderer)
    {

        if (thirdPersonCamera.fadeRenderers.Contains(renderer))
        {
            Renderer _fadeRenderer = renderer;
            float _lerpVelocity = thirdPersonCamera.lerpVelocity;

            if (_fadeRenderer.material.HasProperty("_Color"))
            {
                float _lerpedAlpha = Mathf.Lerp(_fadeRenderer.material.color.a, 1f, Time.fixedDeltaTime * _lerpVelocity);
                _fadeRenderer.material.color = new Color(_fadeRenderer.material.color.r, _fadeRenderer.material.color.g, _fadeRenderer.material.color.b, _lerpedAlpha);
                if (_lerpedAlpha >= 0.9f)
                {
                    _fadeRenderer.material.color =
                        new Color(_fadeRenderer.material.color.r, _fadeRenderer.material.color.g, _fadeRenderer.material.color.b, 1f);
                    if (!_fadeRenderer.material.HasProperty("_Cloak"))
                    {
                        if (_fadeRenderer.material.HasProperty("_Cloak")) ;
                    }
                }
            }


            if (_fadeRenderer.material.HasProperty("_Cloak"))
            {
                float _lerpedCloak = Mathf.Lerp(_fadeRenderer.material.GetFloat("_Cloak"), 1f, Time.fixedDeltaTime * _lerpVelocity / 2);
                _fadeRenderer.material.SetFloat("_Cloak", _lerpedCloak);
                //Debug.Log("cloak: " + _lerpedCloak);
                if (_lerpedCloak >= 0.999f)
                {
                    _fadeRenderer.material.SetFloat("_Cloak", 1f);
                    thirdPersonCamera.fadeRenderers.Remove(_fadeRenderer);
                }
            }
            else
            {
                thirdPersonCamera.fadeRenderers.Remove(_fadeRenderer);
            }
        }
    }

    public void FadeExitBehaviour(Renderer renderer)
    {

        if (fadeRenderers.Contains(renderer))
        {
            Renderer _fadeRenderer = renderer;
            float _lerpVelocity = lerpVelocity;

            if (_fadeRenderer.material.HasProperty("_Color"))
            {
                float _lerpedAlpha = Mathf.Lerp(_fadeRenderer.material.color.a, 1f, Time.fixedDeltaTime * _lerpVelocity);
                _fadeRenderer.material.color = new Color(_fadeRenderer.material.color.r, _fadeRenderer.material.color.g, _fadeRenderer.material.color.b, _lerpedAlpha);
                if (_lerpedAlpha >= 0.9f)
                {
                    _fadeRenderer.material.color =
                        new Color(_fadeRenderer.material.color.r, _fadeRenderer.material.color.g, _fadeRenderer.material.color.b, 1f);
                    if (!_fadeRenderer.material.HasProperty("_Cloak"))
                    {
                        fadeRenderers.Remove(_fadeRenderer);
                        m_colliderRend = null;
                        m_fadeRaycastEntered = false;
                    }
                }
            }


            if (_fadeRenderer.material.HasProperty("_Cloak"))
            {
                float _lerpedCloak = Mathf.Lerp(_fadeRenderer.material.GetFloat("_Cloak"), 1f, Time.fixedDeltaTime * _lerpVelocity / 2);
                _fadeRenderer.material.SetFloat("_Cloak", _lerpedCloak);
                //Debug.Log("cloak: " + _lerpedCloak);
                if (_lerpedCloak >= 0.999f)
                {
                    _fadeRenderer.material.SetFloat("_Cloak", 1f);
                    fadeRenderers.Remove(_fadeRenderer);
                    m_colliderRend = null;
                    m_fadeRaycastEntered = false;
                }
            }
            else
            {
                m_colliderRend = null;
                m_fadeRaycastEntered = false;
                fadeRenderers.Remove(_fadeRenderer);
            }
        }
    }


}
