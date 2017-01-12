Shader "Hidden/BarsEffect" 
{
	Properties{
		_MainTex("Main (RGB)", 2D) = "white" {}
		_BarTex("Bars (RGB)", 2D) = "grayscaleRamp" {}
		_Coverage("Screen Coverage", float) = 1
	}
		SubShader
		{
			Pass
			{
				ZTest Always Cull Off Zwrite Off
				Fog { Mode off }

				CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"

				uniform sampler2D _MainTex;
				uniform sampler2D _BarTex;
				float _Coverage;

				fixed4 frag(v2f_img i) : COLOR
				{
					fixed4 original = tex2D(_MainTex, i.uv);


					float2 offset = float2(0, _Coverage);
					float2 flippedOffset = float2(0, _Coverage + 1);

					float2 flippedUVs = float2(i.uv.x, -1 * i.uv.y);

					float2 barUVs = float2(i.uv + offset);
					float2 flippedBarUVs = (flippedUVs + flippedOffset);

					return original * tex2D(_BarTex, barUVs) * tex2D(_BarTex, flippedBarUVs);
				}

				ENDCG
		}
	}

	Fallback off
}
