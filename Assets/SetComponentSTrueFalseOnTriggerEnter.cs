using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetComponentSTrueFalseOnTriggerEnter : MonoBehaviour
{

    public MonoBehaviour[] ComponentsToSetFalse;
    public MonoBehaviour[] ComponentsToSetTrue;



    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            foreach (MonoBehaviour component in ComponentsToSetFalse)
            {
                component.enabled = false;
            }

            foreach (MonoBehaviour component in ComponentsToSetTrue)
            {
                component.enabled = true;
            }
        }


    }
}
