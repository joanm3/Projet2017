using UnityEngine;
using System.Collections;
using System;

public class Surface : MonoBehaviour
{

    public SurfaceProperties Properties;

    void Start()
    {
        if (Properties.Confort_angle >= Properties.Glide_angle)
        {
            Properties.Confort_angle = Properties.Glide_angle - 1f;
        }
    }

}

[Serializable]
public class SurfaceProperties
{
    [Space(10.0f)]
    public int Id = -1;
    public string Name;


    [Header("Angles")]

    [Tooltip("Jusqu'à quel angle sommes nous en Confort (Gizmos cyan)")]
    [Range(1.0f, 45.0f)]
    public float Confort_angle = 25.0f;
    [Tooltip("Jusqu'à quel angle sommes nous en Glide (au delà = Fall) (Gizmos violet puis rouge)")]
    [Range(45.0f, 90.0f)]
    public float Glide_angle = 25.0f;
    public float Gliding_force_time = 1f;


    [Header("Surface")]

    [Tooltip("La vitesse que donne la surface de Glide_angle maximum")]
    [Range(5.0f, 50.0f)]
    public float maxGlideSpeed = 15.0f;
    [Tooltip("Time est utilisée en tant qu'indicateur de surface. 0 = Confort_angle, 1 = Glide_angle. Value est utilisé pour définir quel proportion de maxGlideSpeed est utilisé (0 = 0%, 1 = 100%)")]
    public AnimationCurve velocityGlideAcceleration = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    [Tooltip("Vitesse de transition par seconde entre la velocité actuelle et celle fournie par la surface (1 = 1 unité de vitesse par seconde). lORSQUE LA VELOCITE MONTE")]
    [Range(1.0f, 40.0f)]
    public float velocityTransitionSpeed_acceleration = 2.0f;
    [Tooltip("Vitesse de transition par seconde entre la velocité actuelle et celle fournie par la surface (1 = 1 unité de vitesse par seconde). LORSQUE LA VELOCITE BAISSE")]
    [Range(1.0f, 40.0f)]
    public float velocityTransitionSpeed_decceleration = 0.5f;
}

