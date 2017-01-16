using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.EventSystems;


[ExecuteInEditMode]
public class SnapToGrid : MonoBehaviour
{
    public BoxCollider useChildBoxCollider;
    public bool removeBoxColliderInGame = false;

    public enum PivotPoint { objectPivot, objectCenter, TopRightFront, TopLeftFront, BottomRightFront, BottomLeftFront, TopRightBack, TopLeftBack, BottomRightBack, BottomLeftBack };

    public PivotPoint pivot = PivotPoint.objectPivot; 

    private void Awake()
    {
        ApplyBoxCollider();

#if !UNITY_EDITOR
        Destroy(this); 
#endif
    }

    private void OnEnable()
    {
        ApplyBoxCollider();
    }

    private void ApplyBoxCollider()
    {
        if (this.GetComponent<BoxCollider>() == null)
            gameObject.AddComponent<BoxCollider>();

        if (Application.isPlaying)
        {
            if (removeBoxColliderInGame)
            {
                Destroy(this.GetComponent<BoxCollider>());
            }
        }
    }

}
