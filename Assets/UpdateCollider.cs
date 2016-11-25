using UnityEngine;
using System.Collections;

public class UpdateCollider : MonoBehaviour {

	SkinnedMeshRenderer myMeshRenderer;
	MeshCollider myCollider;

	void UpdateColliderMesh() {
		Mesh colliderMesh = new Mesh();
		myMeshRenderer.BakeMesh(colliderMesh);
		myCollider.sharedMesh = null;
		myCollider.sharedMesh = colliderMesh;
	}

	void Start(){
		myCollider = GetComponent<MeshCollider>();
		myMeshRenderer = GetComponent<SkinnedMeshRenderer>();
	}

	void Update(){

		UpdateColliderMesh();

	}
}
