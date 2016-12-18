Shader "Custom/WorldSpaceNoiseMask"
{
	Properties
	{
		//material 
		[NoScaleOffset] _MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		//noise settings
		_NoiseSettings("Noise settings : scale xy, offset xy", Vector) = (1, 1, 0, 0)
		_NoiseSettings2("Noise settings gain, frequencyMul, baseWeight, na", Vector) = (0.5, 2.5, 0.5, 0)
		
		//circle properties
		_CircleForce("Circle Force", Range(0,1)) = 0.25
		_Expand("Inner Expand", Range(0,1)) = 0.5
		_Radius("Radius", FLOAT) = 0.75
		_WindowHeight("Window Height", FLOAT) = 0.0
		_ChangePoint("Change at this distance", Float) = 5
		_MaxDistance("Max hide value", Float) = 5
		_Cloak("Cloak", Range(0,1)) = 0.0

		//outline properties
		_LineWidth("Line Width", Range(0,1)) = 0.025
		_LineColor("Line Color (alpha = emission)", Color) = (1,1,1,0)
		_Color("Color(not applied)", Color) = (1,1,1,1)
	}

		SubShader
		{

			Tags
			{
				"RenderType" = "Opaque"
			}


			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma vertex vert 
			#pragma surface surf Standard fullforwardshadows

			#pragma target 4.0
			#include "Assets/Shaders/Include/snoise.cginc"

			uniform sampler2D _MainTex;
			uniform float _Glossiness;
			uniform float _Metallic;

			//noise 
			uniform float _Cloak;
			uniform float4 _NoiseSettings;
			uniform float4 _NoiseSettings2;

			//radial gradient
			uniform float4 _CircleColor;
			uniform float _CircleForce;
			uniform float _Expand;
			uniform float2 _Center;
			uniform float _Radius;
			uniform float _WindowHeight;

			//outline
			uniform float _ChangePoint; 
			uniform float4 _LineColor; 
			uniform float _LineWidth; 
			uniform float4 _Color; 
			uniform float _MaxDistance; 

			struct Input
			{
				float2 uv_MainTex : TEXCOORD0;
				float2 noiseUV : TEXCOORD1;
				float4 screenPos;
				float3 worldPos; 
			};



			void vert(inout appdata_full v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input,o);
				o.uv_MainTex = v.texcoord.xy;
				o.noiseUV = (v.texcoord.xy + _NoiseSettings.zw) * _NoiseSettings.xy;
			}

			float NoiseFunction(float baseNoiseValue, uint generation)
			{
				if (generation < 2)
				{
					return 1 - abs(baseNoiseValue);
				}
				else
				{
					return baseNoiseValue * baseNoiseValue;
				}

			}

			float PoseNoiseFunction(float baseNoiseValue)
			{
				return abs(baseNoiseValue);
			}


			void surf(Input IN, inout SurfaceOutputStandard o)
			{

				//***********************SCREEN-UV**********************//
				float2 screenUV = IN.screenPos.xy / IN.screenPos.w;


				//***************************NOISE*********************//
				float noise = 0;
				float weight = _NoiseSettings2.z;
				float frequency = 1;
				const float gain = _NoiseSettings2.x;
				const float frequencyMul = _NoiseSettings2.y;
				for (uint i = 0; i < 8; ++i)
				{
					noise += weight * NoiseFunction(snoise(screenUV * frequency), i);
					weight *= gain;
					frequency *= frequencyMul;
				}
				float postNoise = saturate(PoseNoiseFunction(noise));


				//***************************CIRCLE*********************//
				float2 centerFromSfml = float2(0.5, _WindowHeight + 0.5);
				float2 p = (screenUV.xy - centerFromSfml) / _Radius;
				float r = sqrt(dot(p, p));

				float circleLerp = 1 - _CircleForce; 
				float circle = lerp(circleLerp, noise, (r - _Expand) / (1 - _Expand));
				float maskClip = 1; 

				if (r < 1.0)
				{
					maskClip = 1 - (postNoise - circle); 
					//uncomment to show circle
					//float onlyCircle = lerp(circleLerp, 1, (r - _Expand) / (1 - _Expand));
					//o.Albedo = onlyCircle; 
					
					//uncomment to show maskClip
					//o.Albedo = maskClip; 
				}
				//else
				//{
				//	//o.Albedo = 1; 
				//}

				//uncomment to show noise
				//o.Albedo.rgb = postNoise; 



				//***************************DISTANCE*********************//


				float curDistance = distance(_WorldSpaceCameraPos.xyz, IN.worldPos);
				//if (curDistance < _MaxDistance)
				//	curDistance = _MaxDistance;
				//float changeFactor = maskClip - _Cloak;
				//float changeFactor = maskClip - (1 - (curDistance - _ChangePoint));
				float changeFactor = maskClip + (_Cloak * _ChangePoint * 1.1) - (1 - (curDistance - _ChangePoint));


				//hides pixels with value smaller than 0
				//clip((maskClip - _Cloak - (1 - changeFactor)));
				clip(changeFactor);


				//*********************ASSIGN****************************//
				float4 c = tex2D(_MainTex, IN.uv_MainTex);
				o.Albedo = c.rgb;

				if (changeFactor < _LineWidth)
				{
					o.Albedo = _LineColor.rgb; 
					o.Emission = _LineColor.a; 
				}

				//o.Emission = _LineColor.a;
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a;
			}
			ENDCG
		}

			FallBack Off
}