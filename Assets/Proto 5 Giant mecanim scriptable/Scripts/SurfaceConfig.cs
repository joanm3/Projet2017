using UnityEngine;
using System.Collections;


namespace ProjectGiants.Surface
{
	public class SurfaceConfig
	{
		[Header("Angles")]
		[Space(20.0f)]

		[Tooltip("Jusqu'à quel angle sommes nous en Confort (Gizmos cyan)")]
		[Range(1.0f, 45.0f)]
		public float Confort_angle = 25.0f;
		[Tooltip("Jusqu'à quel angle sommes nous en Glide (au delà = Fall) (Gizmos violet puis rouge)")]
		[Range(45.0f, 90.0f)]
		public float Glide_angle = 25.0f;

		[Space(20.0f)]
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

		[Space(20.0f)]
		[Header("Input")]

		[Tooltip("La vitesse que donne le stick sur une surface plane")]
		[Range(5.0f, 40.0f)]
		public float maxInputSpeed = 15.0f;
		[Tooltip("Vitesse de rotation maximum (en angles par seconde)")]
		[Range(5.0f, 1440.0f)]
		public float max_RotationSpeed = 200.0f;
		[Tooltip("Vitesse de rotation minimum (en angles par seconde)")]
		[Range(5.0f, 360.0f)]
		public float min_RotationSpeed = 50.0f;
		[Tooltip("Vitesse de rotation en fonction de la vitesse de deplacement input. De gauche à droite la vitesse de deplacement input, de haut en bas la vitesse de rotation. Interpolation entre min_RotationSpeed et max_RotationSpeed en fonction de la curve")]
		public AnimationCurve rotationBySpeed = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
		[Tooltip("Interpolation entre 0 et maxInputSpeed")]
		public AnimationCurve InputAcceleration = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
		[Tooltip("Interpolation entre maxInputSpeed et 0")]
		public AnimationCurve InputDecceleration = AnimationCurve.Linear(0.0f, 1.0f, 0.5f, 0.0f);


	}
}