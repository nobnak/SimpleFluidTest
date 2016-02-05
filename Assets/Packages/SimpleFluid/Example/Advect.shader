﻿Shader "SimpleFluid/Advect" {
	Properties {
		_MainTex ("Image", 2D) = "black" {}
		_FluidTex ("Fluid", 2D) = "white" {}
		_Dt ("Delta Time", Float) = 0.1
	}
	SubShader {
		Cull Off ZWrite Off ZTest Always

		Pass {
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _FluidTex;
			float4 _FluidTex_TexelSize;

			float _Dt;

			v2f vert (appdata v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag (v2f i) : SV_Target {
				float2 duv = _FluidTex_TexelSize.xy;
				float4 u = tex2D(_FluidTex, i.uv);
				float4 c = tex2D(_MainTex, i.uv);
				float cAdv = tex2D(_MainTex, i.uv - _Dt * duv * u.xy).x;

				cAdv = clamp(lerp(cAdv, c.y, c.y * 0.1) - 0.01, 0.0, 2.0);
				return float4(cAdv, c.yzw);
			}
			ENDCG
		}
	}
}
