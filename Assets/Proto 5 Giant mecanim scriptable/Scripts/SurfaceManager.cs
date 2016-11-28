using UnityEngine;
using System.Collections;
using ProjectGiants.Surface; 


public class SurfaceManager : Singleton<SurfaceManager>
{


	public SurfaceConfig Config; 


	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}


	public Vector3 CalculateVelocitySpeedDir(Vector3 surfaceNormal, Vector3 tang)
	{
		float fromCtoG = ((Vector3.Angle(Vector3.up, surfaceNormal) - Config.Confort_angle)) / (Config.Glide_angle - Config.Confort_angle);
		fromCtoG = Mathf.Clamp(fromCtoG, 0f, 1f);
		return tang.normalized * (Config.maxGlideSpeed * Config.velocityGlideAcceleration.Evaluate(fromCtoG));


	}

	public float CalculateCurrentRotationSpeed(float velocity)
	{
		return ((Config.max_RotationSpeed - Config.min_RotationSpeed) * Config.rotationBySpeed.Evaluate (velocity)) + Config.min_RotationSpeed; 

	}
}
