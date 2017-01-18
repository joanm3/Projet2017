using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetParentFalseOnTriggerExit : MonoBehaviour
{

    public Transform target;

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
            target.parent = null;
    }
}
