using UnityEngine;
using System.Collections;

public class UpdateCollider : MonoBehaviour {

	SkinnedMeshRenderer myMeshRenderer;
	MeshCollider myCollider;

	[Tooltip("Check the distance between the player and the bones. WIll do mesh collider actualisation only if one of the bones is near to the player")]
	public Transform[] bonesToCheck;
	[Tooltip("Distance to check. Attention ! a to large value will cause performance issues !")]
	public float distanceMax = 5.0f;

	public bool EDITOR_drawDistanceLines = true;

	[SerializeField]
	[HideInInspector]
	private Transform player;

	void UpdateColliderMesh() {
		Mesh colliderMesh = new Mesh();
		myMeshRenderer.BakeMesh(colliderMesh);
		myCollider.sharedMesh = null;
		myCollider.sharedMesh = colliderMesh;
	}

	void Start(){
		player = GameObject.Find("Player").transform;

		myCollider = GetComponent<MeshCollider>();
		myMeshRenderer = GetComponent<SkinnedMeshRenderer>();
	}

	void Update(){

		if(CheckIfInAnyRange())
		{
			myCollider.enabled = true;
			UpdateColliderMesh();
		}else{
			myCollider.enabled = false;
		}


	}

	bool CheckIfInAnyRange(){

		for (int i = 0; i < bonesToCheck.Length; i++)
		{
			if(Vector3.Distance(player.position, bonesToCheck[i].position) < distanceMax)
				return true;
		}
		return false;
	}

	public void OnDrawGizmosSelected ()
	{


		if (!Application.isPlaying)
			return; 

		if(EDITOR_drawDistanceLines){
			
			if(player != null){
				
				Gizmos.color = Color.red;
				for (int i = 0; i < bonesToCheck.Length; i++) {
					Gizmos.DrawRay(bonesToCheck[i].position, (player.position - bonesToCheck[i].position).normalized * distanceMax);
				}
			}else{
				print("null player ref, please name your player GameObject 'Player'");
				player = GameObject.Find("Player").transform;
				print("Error corrected, do as if i didn't say anything :)");

	//			if(player != null){
	//				print("Error corrected, do as if i didn't say anything :)");
	//			}else{
	//				print("Player not found. Please name your player GameObject 'Player' ");
	//			}

			}

		}
	}
}
