using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class CameraTrigger : MonoBehaviour
{

    public ThirdPersonCameraMovement thirdPersonCamera;

    void Start()
    {
        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = Camera.main.GetComponent<ThirdPersonCameraMovement>();
            if (thirdPersonCamera == null)
            {
                thirdPersonCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<ThirdPersonCameraMovement>();
                if (thirdPersonCamera == null)
                {
                    Debug.LogError("Camera not found for Camera Trigger, deleting component");
                    Destroy(this);
                }
            }
        }

        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(ThirdPersonCameraMovement.fadeLayerString))
        {
            if (!thirdPersonCamera.fadeRenderers.Contains(other.GetComponent<Renderer>()))
            {
                thirdPersonCamera.fadeRenderers.Add(other.GetComponent<Renderer>());
            }
        }
    }

    void OnTriggerExit(Collider other)
    {

        if (other.gameObject.layer == LayerMask.NameToLayer(ThirdPersonCameraMovement.fadeLayerString))
        {
            thirdPersonCamera.FadeExitBehaviour(ref thirdPersonCamera, other.GetComponent<Renderer>()); 
        }

        //FADE BEHAVIOUR
        // thirdPersonCamera.FadeExitBehaviour(ref thirdPersonCamera, other); 
    }
}
